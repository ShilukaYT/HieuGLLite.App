using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using DiscordRPC;
using Downloader;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Taskbar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using SharpCompress.Compressors.ZStandard.Unsafe;
using Windows.ApplicationModel.Store;
using Windows.Media.Playback;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static HieuGLLite.Apps.AppModel;

namespace HieuGLLite.Apps
{
	public partial class Main : Form
	{
		[DllImport("dwmapi.dll")]
		public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		[DllImport("shell32.dll", SetLastError = true)]
		static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

		public const int WM_COPYDATA = 0x004A;

		[StructLayout(LayoutKind.Sequential)]
		public struct COPYDATASTRUCT
		{
			public IntPtr dwData;
			public int cbData;
			public IntPtr lpData;
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool ChangeWindowMessageFilter(uint msg, uint dwFlag);

		public const uint MSGFLT_ADD = 1;
		private NotifyIcon trayIcon;
		private ContextMenuStrip trayMenu;

		private DiscordRpcClient discordClient;

		public bool isDevMode = false;

		public string AppName;
		public string version = "26.4.1 (Public Beta)";
		public int versioncode = 260401;

		public string FE_version;

		public bool isDownloading;

		public bool isRunning;

		private bool isSessionVerified = false;
		public readonly string hostURL = "https://shilukayt.github.io/HieuGLLiteFE/";
		public readonly string jsonURL = "https://raw.githubusercontent.com/ShilukaYT/HieuGLLiteFE/refs/heads/main/assets/json/";
		private readonly string authURL = "https://shilukayt.github.io/DiscordAuth/";

		private string officialGuildId = "1074226640894316655";

		private List<string> currentUserRoles = new List<string>();

		private string recentTitle = string.Empty;

		private string currentLanguage = string.Empty;

		private MultiThreadedDownloader _downloader;

		private List<GameApp> globalAppList = new List<GameApp>();
		private Dictionary<string, MultiThreadedDownloader> activeDownloads = new Dictionary<string, MultiThreadedDownloader>();
		private Dictionary<string, CancellationTokenSource> cancellationTokens = new Dictionary<string, CancellationTokenSource>();

		private static string RootFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps");
		private static string DownloadFolder => Path.Combine(RootFolder, "Downloads");
		private static string StateFolder => Path.Combine(RootFolder, "DownloadStates");
		private static string ConfigFile => Path.Combine(RootFolder, "installed_apps.json");
		private static string SettingsFile => Path.Combine(RootFolder, "launcher_settings.json");
		private static string TempFolder => Path.Combine(RootFolder, "Temp");
		private static string LogsFolder => Path.Combine(RootFolder, "Logs");

		private Dictionary<string, AppProgressState> appRunningStates = new Dictionary<string, AppProgressState>();

		[ComImport]
		[Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface ITaskbarList3
		{
			[PreserveSig] void HrInit();
			[PreserveSig] void AddTab(IntPtr hwnd);
			[PreserveSig] void DeleteTab(IntPtr hwnd);
			[PreserveSig] void ActivateTab(IntPtr hwnd);
			[PreserveSig] void SetActiveAlt(IntPtr hwnd);
			[PreserveSig] void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);
			[PreserveSig] void SetProgressValue(IntPtr hwnd, ulong ullCompleted, ulong ullTotal);
			[PreserveSig] void SetProgressState(IntPtr hwnd, TaskbarProgressBarStatus tbpFlags);
		}

		public enum TaskbarProgressBarStatus
		{
			NoProgress = 0, Indeterminate = 0x1, Normal = 0x2, Error = 0x4, Paused = 0x8
		}

		[ComImport]
		[Guid("56fdf344-fd6d-11d0-958a-006097c9a090")]
		[ClassInterface(ClassInterfaceType.None)]
		internal class TaskbarInstance { }

		private ITaskbarList3 _taskbarList;

		[DllImport("psapi.dll")]
		public static extern int EmptyWorkingSet(IntPtr hwProc);

		[DllImport("wininet.dll")]
		private extern static bool InternetGetConnectedState(out int description, int reservedValue);

		public Main()
		{
			SetCurrentProcessExplicitAppUserModelID("HieuGLLite.Apps");
			WriteLog("INFO", $"Initializing the application, AppID: HieuGLLite.Apps");
			InitializeComponent();
			FontManager.LoadCustomFont();
			ChangeWindowMessageFilter(WM_COPYDATA, MSGFLT_ADD);
			LoadSettings();

			Main.WriteLog("INFO", "Registering event");
			webView21.CoreWebView2InitializationCompleted += WebView21_CoreWebView2InitializationCompleted;
			webView21.WebMessageReceived += WebView_WebMessageReceived;
			webView21.NavigationCompleted += webview21_NavigationCompleted;
			SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

			this.Resize += Main_Resize;

			SetupWakeUpListener();
		}
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_COPYDATA)
			{
				Main.WriteLog("INFO", "Getting uri");
				COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT));
				string receivedUrl = Marshal.PtrToStringUni(cds.lpData);

				if (!string.IsNullOrEmpty(receivedUrl) && receivedUrl.Contains("code="))
				{
					int startIndex = receivedUrl.IndexOf("code=") + 5;
					string code = receivedUrl.Substring(startIndex);

					if (code.Contains("&"))
					{
						code = code.Substring(0, code.IndexOf("&"));
					}

					Main.WriteLog("INFO", "Getting token code: " + code);
					Main.WriteLog("INFO", "Showing app");
					this.Show();
					this.WindowState = FormWindowState.Normal;
					this.Activate();

					ExchangeCodeForTokenAsync(code);
				}
			}

			base.WndProc(ref m);
		}

		private EventWaitHandle wakeUpHandle;
		private Thread wakeUpThread;

		private void SetupWakeUpListener()
		{
			Main.WriteLog("INFO", "Setting up wakeup listener");
			string AppGuid = "69ba7d1a-d023-476b-86e2-7652abebb84d";
			wakeUpHandle = new EventWaitHandle(false, EventResetMode.AutoReset, AppGuid + "_WakeUp");

			wakeUpThread = new Thread(() =>
			{
				while (true)
				{
					wakeUpHandle.WaitOne();

					this.Invoke((MethodInvoker)delegate
					{
						ShowApp();
					});
				}
			});
			wakeUpThread.IsBackground = true;
			wakeUpThread.Start();
		}

		public static void WriteLog(string level, string message, Exception ex = null)
		{
			try
			{
				if (!Directory.Exists(LogsFolder)) Directory.CreateDirectory(LogsFolder);
				string logFile = Path.Combine(LogsFolder, $"Log_{DateTime.Now:yyyy-MM-dd}.txt");

				using (StreamWriter writer = new StreamWriter(logFile, true, Encoding.UTF8))
				{
					writer.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
					if (ex != null)
					{
						writer.WriteLine($"[CHI TIẾT LỖI]: {ex.Message}");
						writer.WriteLine($"[VỊ TRÍ DÒNG CODE]: {ex.StackTrace}");
						writer.WriteLine(new string('-', 50));
					}
				}
			}
			catch { }
		}

		private async void Main_Load(object sender, EventArgs e)
		{
			await DetectAndSetDefaultLanguageAsync();
			ApplyLanguage(currentLanguage);
			Main.WriteLog("INFO", "Apply app name to: " + AppName);
			AppName = isDevMode ? $"{Lang.AppName} (Developer mode)" : $"{Lang.AppName} (Public Beta)";
			// Thay vì fix cứng Hieu GL Lite, ta dùng đa ngôn ngữ từ Langs
			SetupSystemTray();
			InitializeRPC();


			bool isDarkTheme = currentTheme == "dark" || (currentTheme == "system" && IsWindowsDarkMode());

			Main.WriteLog("INFO", "Changing title to : " + AppName);
			this.Text = AppName;

			Main.WriteLog("INFO", "Changing Webview2 theme to " + currentTheme);
			ApplyTheme(isDarkTheme);
			webView21.DefaultBackgroundColor = isDarkTheme ? Color.FromArgb(18, 18, 18) : Color.White;
			webView21.ZoomFactor = 0.8;

			Main.WriteLog("INFO", "Setting up the environment");
			var env = await CoreWebView2Environment.CreateAsync(null, RootFolder);

			await webView21.EnsureCoreWebView2Async(env);

			webView21.CoreWebView2.SetVirtualHostNameToFolderMapping(
			"hieugllite.app",
			System.IO.Path.Combine(Application.StartupPath, "Assets"),
			CoreWebView2HostResourceAccessKind.Allow
			);

			int desc;
			Main.WriteLog("INFO", "Checking internet state");
			if (InternetGetConnectedState(out desc, 0))
			{
				Main.WriteLog("INFO", "Connected, loading web...");
				webView21.Source = new Uri(isDevMode ? "http://localhost:5173" : hostURL);
			}
			else
			{
				Main.WriteLog("INFO", "Lost connection, loading offline html file...");
				string offlineFilePath = Path.Combine(Application.StartupPath, "Assets", "offline.html");
				if (System.IO.File.Exists(offlineFilePath))
				{
					webView21.Source = new Uri("file:///" + offlineFilePath.Replace("\\", "/"));
				}
				else
				{
					webView21.CoreWebView2.NavigateToString("<body style='background:#121212;color:white;text-align:center;padding-top:20%;font-family:sans-serif'><h1>Mất kết nối mạng!</h1><p>Vui lòng kiểm tra lại Internet của bạn.</p></body>");
				}
			}
		}

		private async Task DetectAndSetDefaultLanguageAsync()
		{
			if (System.IO.File.Exists(SettingsFile)) return;

			WriteLog("INFO", "First launch detected. Detecting user location to set default language...");

			try
			{
				string countryCode = "US";
				countryCode = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName;

				if (!string.IsNullOrEmpty(countryCode) && countryCode.ToUpper() == "VN")
				{
					currentLanguage = "vi-VN";
					WriteLog("INFO", "Location: Vietnam. Default language set to Vietnamese.");
				}
				else
				{
					currentLanguage = "en-US";
					WriteLog("INFO", $"Location: {countryCode}. Default language set to English.");
				}

				SaveSettings();
			}
			catch (Exception ex)
			{
				currentLanguage = "en-US";
				SaveSettings();
				WriteLog("ERROR", $"Auto-detect language failed: {ex.Message}. Fallback to English.");
			}
		}

		private void ApplyLanguage(string langCode)
		{
			try
			{
				var culture = new System.Globalization.CultureInfo(langCode);

				// 1. Đổi ngôn ngữ cho luồng hiện hành (Main UI Thread)
				System.Threading.Thread.CurrentThread.CurrentCulture = culture;
				System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

				// 2. ÉP TẤT CẢ CÁC LUỒNG NGẦM (Task.Run) PHẢI DÙNG CHUNG NGÔN NGỮ NÀY
				System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;
				System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;

				// Xóa dòng Lang.Culture = culture; đi vì không cần thiết nữa!

				WriteLog("INFO", $"Language changed to: {langCode}");
			}
			catch (Exception ex)
			{
				WriteLog("ERROR", $"Failed to apply language: {ex.Message}");
			}
		}

		private void SetupSystemTray()
		{
			Main.WriteLog("INFO", "Setting up system tray");
			trayMenu = new ContextMenuStrip();
			trayMenu.Items.Add($"{Lang.TrayOpen} {AppName}", null, TrayOpen_Click);
			trayMenu.Items.Add(Lang.TrayExit, null, TrayExit_Click);

			trayIcon = new NotifyIcon();
			trayIcon.Text = AppName;

			trayIcon.Icon = this.Icon;

			trayIcon.ContextMenuStrip = trayMenu;
			trayIcon.DoubleClick += TrayIcon_DoubleClick;
			trayIcon.Visible = true;
		}

		private void UpdateWebViewZoom()
		{
			if (webView21 == null || webView21.CoreWebView2 == null) return;

			int width = this.ClientSize.Width;
			int height = this.ClientSize.Height;
			double newZoom = 1.0;

			if (width <= 1280 || height <= 720)
			{
				newZoom = 0.8;
			}
			else if (width <= 1600 || height <= 900)
			{
				newZoom = 0.9;
			}
			else if (width <= 2560 || height <= 1440)
			{
				newZoom = 1.0;
			}
			else
			{
				newZoom = 1.3;
			}

			if (webView21.ZoomFactor != newZoom)
			{
				Main.WriteLog("INFO", "Updating zoom to " + newZoom * 100 + "%");
				webView21.ZoomFactor = newZoom;
			}
		}

		private void Main_Resize(object sender, EventArgs e)
		{
			UpdateWebViewZoom();
		}
		private bool? _lastAppliedSystemThemeIsDark = null;

		private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			if (e.Category == UserPreferenceCategory.General)
			{
				if (currentTheme == "system")
				{
					bool isDark = IsWindowsDarkMode();

					if (_lastAppliedSystemThemeIsDark == isDark) return;

					_lastAppliedSystemThemeIsDark = isDark;

					this.Invoke((MethodInvoker)delegate
					{
						ApplyTheme(isDark);
						if (webView21 != null)
						{
							webView21.DefaultBackgroundColor = isDark ? Color.FromArgb(18, 18, 18) : Color.White;
						}
					});
				}
			}
		}

		private void WebView21_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
		{
			if (e.IsSuccess)
			{
				webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = isDevMode;
				webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = isDevMode;
				webView21.CoreWebView2.Settings.AreDevToolsEnabled = isDevMode;
				webView21.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = isDevMode;
				webView21.CoreWebView2.Settings.IsZoomControlEnabled = isDevMode;
			}
			else
			{
				CustomMessageBox.Show(Lang.MsgErrInitWebview + e.InitializationException, Lang.MsgErrTitle, false);
			}
		}

		private string currentOtp = "";
		private DateTime otpExpiryTime;

		private void webview21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
		{
			if (e.IsSuccess)
			{
				Task.Delay(500).ContinueWith(_ =>
				{
					this.Invoke((MethodInvoker)delegate
					{
					});
				});
				var syncData = new { type = "INIT_COMPLETED" };
				webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(syncData));
			}
			else
			{
				if (e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
				{
					string offlineFilePath = Path.Combine(Application.StartupPath, "Assets", "offline.html");
					if (System.IO.File.Exists(offlineFilePath))
						webView21.CoreWebView2.Navigate("file://" + offlineFilePath);
					else
						webView21.CoreWebView2.NavigateToString("<body style='background:#121212;color:white;text-align:center;padding-top:20%'><h1>Mất kết nối mạng!</h1></body>");
				}
			}
		}

		private void webView21_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
		{
			string videoPath = Path.Combine(Application.StartupPath, "Assets", IsWindowsDarkMode() ? "loading_dark.mp4" : "loading_light.mp4");
			string html = $@"
    <style>
        body {{ margin: 0; overflow: hidden; background: #{(IsWindowsDarkMode() ? "121212" : "FFFFFF")}; }}
        video {{ width: 100vw; height: 100vh; object-fit: cover; }}
    </style>
    <video autoplay muted loop playsinline>
        <source src='file:///{videoPath.Replace("\\", "/")}' type='video/mp4'>
    </video>";
			webView21.CoreWebView2.NavigateToString(html);
		}

		private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
		{
			string appID;
			var message = JObject.Parse(e.WebMessageAsJson); string type = message["type"]?.ToString();
			switch (type)
			{
				case "SELECT_FOLDER":
					this.Invoke((MethodInvoker)delegate
					{
						Main.WriteLog("INFO", "Requested to open the folder selection dialog box.");
						using (FolderBrowserDialog fbd = new FolderBrowserDialog())
						{
							if (fbd.ShowDialog(this) == DialogResult.OK)
							{
								string selectedPath = fbd.SelectedPath;

								var response = new
								{
									type = "FOLDER_SELECTED",
									path = selectedPath
								};
								webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
							}
						}
					});
					break;

				case "RELOAD_APP":
					Main.WriteLog("INFO", "Reloading app");
					this.Invoke((MethodInvoker)delegate
					{
						webView21.Source = new Uri(isDevMode ? "http://localhost:5173/" : hostURL);
					});
					break;
				case "GET_CLIENT_VERSION":
					this.Invoke((MethodInvoker)delegate
					{
						string jsonResponse = $@"{{
                            ""type"": ""CLIENT_VERSION"",
                            ""version"": ""{this.version}"",
                            ""versioncode"": ""{this.versioncode}""
                        }}";
						webView21.CoreWebView2.PostWebMessageAsJson(jsonResponse);

					});
					break;
				case "CHANGE_TITLE":
					string newTitle;

					string title = message["title"]?.ToString();
					if (title != null)
					{
						recentTitle = this.Text;
						newTitle = title + " - " + AppName;
					}
					else
					{
						newTitle = recentTitle;
					}
					;

					this.Invoke((MethodInvoker)delegate { this.Text = newTitle; });
					Debug.WriteLine("Đã thay đổi title thành: " + newTitle);
					Main.WriteLog("INFO", "Changed to: " + newTitle);
					break;
				case "OPEN_URL":

					string url = message["url"]?.ToString();
					string feature = message["feature"]?.ToString();
					if (!string.IsNullOrEmpty(url))
					{
						try
						{
							Debug.WriteLine("Đã yêu cầu mở liên kết: " + url);
							Main.WriteLog("INFO", "Requested url: " + url);
							System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
							{
								FileName = url,
								UseShellExecute = true
							});

						}
						catch (Exception ex)
						{
							Main.WriteLog("ERROR", "Unable to open the link: " + url);
							CustomMessageBox.Show(Lang.MsgErrOpenLink + ex.Message, Lang.MsgErrTitle, false);
						}
					}
					break;

				case "LOGIN_DISCORD":
					Task.Run(() =>
					{
						HandleDiscordLogin();
					});
					break;

				case "CHECK_AUTO_LOGIN":
					Task.Run(() => { AutoLoginDiscord(); });
					break;

				case "LOGOUT_DISCORD":
					if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat")))
					{
						System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"));
					}
					currentUserRoles = new List<string>();
					GetData();
					break;

				case "GET_APPS":
					{
						bool isForce = message["force"]?.Value<bool>() ?? false;

						GetData(isForce);
						break;
					}

				case "PLAY":
					string appId = message["appId"]?.ToString();

					Task.Run(() =>
					{
						try
						{
							var app = globalAppList.FirstOrDefault(a => a.id == appId);

							if (app != null && app.isInstalled && !string.IsNullOrEmpty(app.programPath))
							{
								string exePath = System.IO.Path.Combine(app.programPath, app.exeName);

								if (System.IO.File.Exists(exePath))
								{
									isRunning = true;

									this.Invoke((MethodInvoker)delegate
									{
										webView21.CoreWebView2.PostWebMessageAsJson($@"{{
                                    ""type"": ""APP_STATE_CHANGED"", 
                                    ""appId"": ""{appId}"", 
                                    ""isRunning"": true 
                                }}");

										this.Hide();
									});

									System.Diagnostics.Process appProcess = new System.Diagnostics.Process();
									appProcess.StartInfo.FileName = exePath;
									appProcess.StartInfo.WorkingDirectory = app.programPath;
									appProcess.StartInfo.UseShellExecute = true;
									appProcess.EnableRaisingEvents = true;

									appProcess.Exited += (sender, e) =>
									{
										app.runningProcessIds.Remove(appProcess.Id);

										isRunning = globalAppList.Any(a => a.runningProcessIds.Count > 0);

										if (app.runningProcessIds.Count == 0)
										{
											this.Invoke((MethodInvoker)delegate
											{
												webView21.CoreWebView2.PostWebMessageAsJson($@"{{
                                            ""type"": ""APP_STATE_CHANGED"", 
                                            ""appId"": ""{appId}"", 
                                            ""isRunning"": false 
                                        }}");

												if (!isRunning)
												{
													this.Show();
													this.WindowState = FormWindowState.Normal;
													this.Activate();
													UpdatePresence("", "Phiên bản: " + FE_version, "logo", "");
												}
											});
										}
										appProcess.Dispose();
									};

									if (appProcess.Start())
									{
										app.runningProcessIds.Add(appProcess.Id);
										string large = !string.IsNullOrEmpty(app.rpc_large_key) ? app.rpc_large_key : "logo";
										string small = !string.IsNullOrEmpty(app.rpc_small_key) ? app.rpc_small_key : "";

										UpdatePresence(
											app.name,
											"Phiên bản: " + FE_version,
											large,
											small
										);
									}
								}
								else
								{
									CustomMessageBox.Show(Lang.MsgErrExecuteFile + exePath, Lang.MsgErrTitle, false);
								}
							}
						}
						catch (Exception ex)
						{
							CustomMessageBox.Show(Lang.MsgErrExecuteFile + ex.Message, Lang.MsgErrTitle, false);
						}
					});
					break;
				case "KILL_APP":

					Task.Run(() =>
					{
						var app = globalAppList.FirstOrDefault(a => a.id == message["appId"]?.ToString());

						if (app != null && app.runningProcessIds.Count > 0)
						{
							var result = CustomMessageBox.Show(Lang.MsgWarnKillApp, Lang.MsgConfirmTitle, true);

							if (result == DialogResult.Yes)
							{
								var pidsToKill = app.runningProcessIds.ToList();

								foreach (int pid in pidsToKill)
								{
									try
									{
										var p = System.Diagnostics.Process.GetProcessById(pid);
										p.Kill();
									}
									catch
									{
									}
								}
							}
						}
					});
					break;
				case "PUSH_FE_VERSION":

					string feVersion = message["version"]?.ToString();
					this.Invoke((MethodInvoker)delegate
					{
						this.FE_version = feVersion;

						UpdatePresence("", $" Phiên bản: {FE_version}", "base", "");
					});
					break;

				case "INSTALL":
					HandleInstallRequest(message);
					break;

				case "PAUSE_DOWNLOAD":
					string pId = message["appId"].ToString();
					if (activeDownloads.TryGetValue(pId, out var pDownloader))
					{
						WriteLog("INFO", $"Pausing download");
						pDownloader.Pause();
						var package = pDownloader.GetCurrentPackage();
						if (package != null)
						{
							package.Urls = new string[0];
							string stateFile = Path.Combine(StateFolder, $"{pId}_{pDownloader.CurrentStatusType}.json");
							System.IO.File.WriteAllText(stateFile, JsonConvert.SerializeObject(package));
						}
						SendStatusToVue(pId, "DOWNLOAD_STATUS", new { status = "PAUSED" });

					}
					break;

				case "CANCEL_DOWNLOAD":
					var confirmResult = CustomMessageBox.Show(Lang.MsgConfirmCancelInstall, Lang.MsgConfirmCancelTitle, true);
					if (confirmResult == DialogResult.Yes && message["appId"] != null)
					{
						string appIdToCancel = message["appId"].ToString();

						if (activeDownloads.TryGetValue(appIdToCancel, out var downloader))
						{
							downloader.Resume();
							downloader.Cancel();
						}

						if (cancellationTokens.TryGetValue(appIdToCancel, out var cts))
						{
							cts.Cancel();

							foreach (var f in Directory.GetFiles(StateFolder, $"{appIdToCancel}_*.json"))
								System.IO.File.Delete(f);
						}

						SendStatusToVue(appIdToCancel, "DOWNLOAD_STATUS", new { status = "DOWNLOAD_CANCELLED" });
					}
					break;

				case "RESUME_DOWNLOAD":
					string rId = message["appId"].ToString();
					Main.WriteLog("INFO", $"Resuming download");

					if (activeDownloads.TryGetValue(rId, out var rDownloader))
					{
						rDownloader.Resume();
					}
					break;


				case "SYNC_DOWNLOAD_STATUS":
					var syncData = appRunningStates.Select(kvp => new
					{
						appId = kvp.Key,
						status = kvp.Value.Status,
						percent = kvp.Value.Percent,
						isDownloading = kvp.Value.Status == "DOWNLOADING"
					}).ToList();

					webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
					{
						type = "DOWNLOAD_SYNC_DATA",
						downloads = syncData
					}));
					break;

				case "CLOSE_WINDOW":
					Main.WriteLog("INFO", $"Closing app!");

					Application.Exit();
					break;
				case "OPEN_MULTI":
					ExecuteAppUtility(message["appId"]?.ToString(), null, "HD-MultiInstanceManager.exe", "");

					break;

				case "CLEANUP":
					ExecuteAppUtility(message["appId"]?.ToString(), "CLEANINGUP", "HD-DiskCompaction.exe", "");
					break;

				case "BACKUP":
					ExecuteAppUtility(message["appId"]?.ToString(), "BACKUP_RESTORE", "HD-DataManager.exe", "-backup");
					break;

				case "RESTORE":
					ExecuteAppUtility(message["appId"]?.ToString(), "BACKUP_RESTORE", "HD-DataManager.exe", "-restore");
					break;

				case "UNINSTALL":
					var result = CustomMessageBox.Show(Lang.MsgConfirmUninstall, Lang.MsgConfirmTitle, true);
					if (result == DialogResult.Yes)
					{
						UninstallApp(message["appId"]?.ToString());
					}
					else return;
					break;
				case "CLEAR_CACHE":
					if (!isDownloading)
					{
						var dialogresult = CustomMessageBox.Show(Lang.MsgConfirmCleanup, Lang.MsgWarnTitle, true);
						if (dialogresult == DialogResult.Yes)
						{
							CleanLauncherInternalFilesAsync();
						}
						else return; break;
					}
					else
					{
						WriteLog("INFO", $"Unable to clean up because there are apps downloading.");
						CustomMessageBox.Show(Lang.MsgWarnCleanupDownloading, Lang.MsgWarnCannotCleanupTitle, false);
					}
					break;
				case "UNINSTALL_APP":
					string protocolName = "hieugllite";
					var uninstallConfirm = CustomMessageBox.Show(
						Lang.MsgConfirmUninstallBase,
						Lang.MsgConfirmUninstallBaseTitle,
						true
					);

					if (uninstallConfirm == DialogResult.Yes)
					{

						webView21.Stop();
						try
						{
							using (var hkcuClasses = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Classes", true))
							{
								if (hkcuClasses?.OpenSubKey(protocolName) != null)
								{
									hkcuClasses.DeleteSubKeyTree(protocolName);
								}
							}

							using (var hkcr = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("", true))
							{
								if (hkcr?.OpenSubKey(protocolName) != null)
								{
									hkcr.DeleteSubKeyTree(protocolName);
								}
							}
						}
						catch (UnauthorizedAccessException)
						{
							CustomMessageBox.Show(Lang.MsgErrRequireAdmin, Lang.MsgErrMissingPrivilegeTitle, false);
						}
						catch (Exception ex)
						{
							CustomMessageBox.Show(Lang.MsgErrRegistry + ex.Message, Lang.MsgErrTitle, false);
						}

						if (webView21 != null)
						{
							webView21.Dispose();
						}

						try
						{
							if (System.IO.Directory.Exists(RootFolder))
							{
								System.IO.Directory.Delete(RootFolder, true);
							}
						}
						catch (Exception ex)
						{
							Main.WriteLog("ERROR", $"Unable to completely clean the folder: {ex.Message}");
						}

						try
						{
							Process process = new Process();
							process.StartInfo.FileName = Path.Combine(Application.StartupPath, "unins000.exe");
							process.StartInfo.UseShellExecute = true;
							process.StartInfo.Arguments = "/SILENT /SUPPRESSMSGBOXES";
							process.Start();
						}
						catch { }


						Application.Exit();
					}
					break;

				case "GET_SETTINGS":
					this.Invoke((MethodInvoker)delegate
					{
						bool isWin11 = Environment.OSVersion.Version.Build >= 22000;

						webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
						{
							type = "SYNC_SETTINGS",
							theme = currentTheme,
							minimizeToTray = minimizeToTrayOnClose,
							isWindows11 = isWin11,
							language = currentLanguage
						}));
					});
					break;

				case "CHANGE_LANGUAGE":
					string newLang = message["lang"]?.ToString() ?? "vi-VN";

					// Kiểm tra xem người dùng có thực sự chọn một ngôn ngữ MỚI hay không
					// (Tránh trường hợp họ đang dùng tiếng Việt mà lại bấm chọn tiếng Việt tiếp)
					if (currentLanguage != newLang)
					{
						currentLanguage = newLang;
						SaveSettings();

						this.Invoke((MethodInvoker)delegate
						{
							ApplyLanguage(currentLanguage);

							// TÙY CHỌN 2 (Xịn xò hơn): Hỏi người dùng có muốn khởi động lại luôn không
							var result = CustomMessageBox.Show(Lang.MsgRestartAfterChangeLanguage, Lang.MsgRestartAfterChangeLanguageTitle, true);
							if (result == DialogResult.Yes)
							{
								if (!isDownloading)
								{
									Application.Restart();
									Environment.Exit(0);
								}
								else
								{
									CustomMessageBox.Show(Lang.MsgWarnExitInstalling, Lang.MsgWarnTitle, false);
									return;
								}
							}
						});
					}
					break;

				case "THEME_CHANGED":
					currentTheme = message["mode"]?.ToString() ?? "system";
					SaveSettings();
					this.Invoke((MethodInvoker)delegate
					{
						bool isDarkTheme = currentTheme == "dark" || (currentTheme == "system" && IsWindowsDarkMode());
						ApplyTheme(isDarkTheme);
					});
					break;

				case "SET_CLOSE_BEHAVIOR":
					minimizeToTrayOnClose = message["minimizeToTray"]?.Value<bool>() ?? true;
					SaveSettings();
					break;

				case "DOWNLOAD_UPDATE":
					string updateUrl = message["url"]?.ToString();
					string expectedHash = message["hash"]?.ToString();
					if (string.IsNullOrEmpty(updateUrl)) return;

					Task.Run(async () =>
					{
						try
						{
							Directory.CreateDirectory(TempFolder);
							string exePath = Path.Combine(TempFolder, "HieuGLLite_Update.exe");

							if (System.IO.File.Exists(exePath)) System.IO.File.Delete(exePath);
							isDownloading = true;

							using (MultiThreadedDownloader downloader = new MultiThreadedDownloader())
							{
								downloader.OnProgressChanged += (percent, speed, downloaded) =>
								{
									this.Invoke((MethodInvoker)delegate
									{
										double displayPercent = percent >= 99 ? 99 : Math.Round(percent);
										webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "UPDATE_PROGRESS", percent = displayPercent }));
									});
								};

								await downloader.StartDownloadAsync(updateUrl, exePath);

								bool isOk = true;

								if (!string.IsNullOrEmpty(expectedHash))
								{
									isOk = VerifySHA256(exePath, expectedHash);
								}

								if (isOk)
								{
									this.Invoke((MethodInvoker)delegate
									{
										webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "UPDATE_READY", path = exePath }));
									});
								}
								else
								{
									if (System.IO.File.Exists(exePath)) System.IO.File.Delete(exePath);

									this.Invoke((MethodInvoker)delegate
									{
										webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "UPDATE_FAILED", error = "Xác thực tệp thất bại (Lỗi SHA256)! Vui lòng tải lại để đảm bảo an toàn." }));
									});
								}
							}
						}
						catch (Exception ex)
						{
							this.Invoke((MethodInvoker)delegate
							{
								webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "UPDATE_FAILED", error = ex.Message }));
							});
						}
					});
					break;

				case "INSTALL_UPDATE":
					string installerPath = message["path"]?.ToString();
					if (System.IO.File.Exists(installerPath))
					{
						Process.Start(new ProcessStartInfo
						{
							FileName = installerPath,
							UseShellExecute = true,
							Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS"
						});

						CleanupBeforeExit();
						Application.Exit();
					}
					else
					{
						CustomMessageBox.Show(Lang.MsgErrUpdateNotFound, Lang.MsgErrTitle, false);
					}
					break;

				case "SEND_OTP":
					string emailTo = message["email"]?.ToString();
					if (string.IsNullOrEmpty(emailTo)) return;

					Task.Run(() =>
					{
						try
						{
							currentOtp = new Random().Next(100000, 999999).ToString();
							otpExpiryTime = DateTime.Now.AddMinutes(2);

							var smtpClient = new System.Net.Mail.SmtpClient("smtp.gmail.com")
							{
								Port = 587,
								Credentials = new NetworkCredential("hieugllite@gmail.com", "unxs txfu dmua womq"),
								EnableSsl = true,
							};

							var mailMessage = new System.Net.Mail.MailMessage
							{
								From = new System.Net.Mail.MailAddress("hieugllite@gmail.com", "Hiếu GL Lite's App"),
								Subject = "[Hiếu GL Lite's App - No Reply] Mã xác minh danh tính (OTP)",
								Body = $@"
									<div style='font-family: Arial, sans-serif; padding: 20px; color: #333;'>
										<h2 style='color: #EA4335;'>Xác minh bảo mật</h2>
										<p>Bạn đã yêu cầu cài đặt một ứng dụng cần xác minh danh tính.</p>
										<p>Mã OTP của bạn là: <b style='font-size: 24px; color: #1a73e8; letter-spacing: 2px;'>{currentOtp}</b></p>
										<p><i>Mã này sẽ hết hạn sau một thời gian ngắn. Vui lòng không chia sẻ mã này cho bất kỳ ai!</i></p>
									</div>",
								IsBodyHtml = true,
							};

							mailMessage.To.Add(emailTo);

							smtpClient.Send(mailMessage);

							this.Invoke((MethodInvoker)delegate
							{
								webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "OTP_SENT_SUCCESS" }));
							});
						}
						catch (Exception ex)
						{
							this.Invoke((MethodInvoker)delegate
							{
								webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "OTP_SENT_FAILED", error = ex.Message }));
							});
						}
					});
					break;
				case "VERIFY_OTP":
					string userInputOtp = message["code"]?.ToString();

					this.Invoke((MethodInvoker)delegate
					{
						if (DateTime.Now > otpExpiryTime)
						{
							currentOtp = "";
							webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "OTP_VERIFY_FAILED" }));
							return;
						}

						if (!string.IsNullOrEmpty(currentOtp) && userInputOtp == currentOtp)
						{
							currentOtp = "";
							isSessionVerified = true;
							webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "OTP_VERIFY_SUCCESS" }));
						}
						else
						{
							webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new { type = "OTP_VERIFY_FAILED" }));
						}
					});
					break;

				case "default":
					CustomMessageBox.Show("Yêu cầu không hợp lệ: " + type, Lang.MsgErrTitle, false);
					break;
			}
		}

		void UpdateInternalState(string appId, string status, double percent = 0)
		{
			if (!appRunningStates.ContainsKey(appId))
				appRunningStates[appId] = new AppProgressState { AppId = appId };

			appRunningStates[appId].Status = status;
			appRunningStates[appId].Percent = percent;
		}

		private async Task CleanLauncherInternalFilesAsync()
		{
			try
			{
				WriteLog("INFO", $"Cleaning app cache, downloads,...");
				if (webView21.CoreWebView2 != null)
				{
					WriteLog("INFO", $"Cleaning browser cache...");

					await webView21.CoreWebView2.Profile.ClearBrowsingDataAsync(
						CoreWebView2BrowsingDataKinds.DiskCache |
						CoreWebView2BrowsingDataKinds.LocalStorage
					);
				}

				if (Directory.Exists(TempFolder))
				{
					WriteLog("INFO", $"Cleaning temp folder...");
					DirectoryInfo di = new DirectoryInfo(TempFolder);
					foreach (FileInfo file in di.GetFiles())
					{
						try { file.Delete(); } catch { }
					}
				}

				if (Directory.Exists(DownloadFolder))
				{
					WriteLog("INFO", $"Cleaning download folder...");
					DirectoryInfo di = new DirectoryInfo(DownloadFolder);
					foreach (FileInfo file in di.GetFiles())
					{
						try { file.Delete(); } catch { }
					}
				}

				if (Directory.Exists(LogsFolder))
				{
					WriteLog("INFO", $"Cleaning logs folder...");
					DirectoryInfo di = new DirectoryInfo(LogsFolder);
					foreach (FileInfo file in di.GetFiles())
					{
						try { file.Delete(); } catch { }
					}
				}

				webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
				{
					type = "CLEANUP_COMPLETED",
					message = "Đã xóa bộ nhớ đệm hệ thống thành công!"
				}));
			}
			catch (Exception ex)
			{
				WriteLog("INFO", $"Clean up error " + ex.Message);
			}
		}

		private void ApplyTheme(bool isDark)
		{
			Main.WriteLog("INFO", "Apply theme: " + (isDark ? "Dark" : "Light"));
			this.BackColor = isDark ? Color.FromArgb(18, 18, 18) : Color.White;

			int useDarkMode = isDark ? 1 : 0;
			DwmSetWindowAttribute(this.Handle, 20, ref useDarkMode, sizeof(int));

			int titleColor = isDark ? 0x001E1E1E : 0x00FFFFFF;
			DwmSetWindowAttribute(this.Handle, 35, ref titleColor, sizeof(int));

			int textColor = isDark ? 0x00FFFFFF : 0x00000000;
			DwmSetWindowAttribute(this.Handle, 36, ref textColor, sizeof(int));
		}

		public bool IsWindowsDarkMode()
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
				{
					if (key != null)
					{
						object registryValue = key.GetValue("AppsUseLightTheme");
						if (registryValue != null)
						{
							return (int)registryValue == 0;
						}
					}
				}
			}
			catch { }
			return false;
		}

		private void UninstallApp(string appId)
		{
			var app = globalAppList.FirstOrDefault(a => a.id == appId);
			if (app == null || !app.isInstalled) return;

			string FolderPath = Path.Combine(Application.StartupPath, "BSTCleaner_native");
			string fullPath = Path.Combine(FolderPath, "BSTCleaner.exe");
			if (!System.IO.File.Exists(fullPath))
			{
				CustomMessageBox.Show(Lang.MsgErrToolNotFound + "BSTCleaner.exe", Lang.MsgErrTitle, false);
				return;
			}

			Task.Run(() =>
			{
				try
				{
					WriteLog("INFO", $"Uninstalling {app.id}");
					SendStatusToVue(appId, "DOWNLOAD_STATUS", new { status = "UNINSTALLING" });
					SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);

					using (Process p = new Process())
					{
						p.StartInfo.FileName = fullPath;
						p.StartInfo.WorkingDirectory = FolderPath;
						p.StartInfo.Arguments = $"-noui -oem {app.oem.Replace("BlueStacks_", "")}";
						p.StartInfo.UseShellExecute = true;
						p.EnableRaisingEvents = true;

						var tcs = new TaskCompletionSource<bool>();

						p.Exited += (s, ev) =>
						{
							tcs.SetResult(true);
						};

						p.Start();

						tcs.Task.Wait();

						app.isInstalled = false;
						app.programPath = "";
						app.dataPath = "";

						WriteLog("INFO", $"Updating app location");
						SaveAppListToDisk();

						SendStatusToVue(appId, "APP_UNINSTALLED", new { });
						ShowToastWithIconAsync(app.id, Lang.ToastUninstallTitle, string.Format(Lang.ToastUninstallMessage, app.name), app.icon);
						SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
					}
				}
				catch (Exception ex)
				{
					WriteLog("ERROR", $"File execution error: " + ex.Message);
					CustomMessageBox.Show(Lang.MsgErrExecuteFile + ex.Message, Lang.MsgErrTitle, false);
					SendStatusToVue(appId, "DOWNLOAD_FAILED", new { error = ex.Message });
				}
			});

		}

		private bool CleanConflictingApp(GameApp app)
		{
			string oemToClean = app.oem.Replace("BlueStacks_", "");

			string FolderPath = Path.Combine(Application.StartupPath, "BSTCleaner_native");
			string fullPath = Path.Combine(FolderPath, "BSTCleaner.exe");

			Debug.WriteLine("Đang yêu cầu gỡ cài đặt: " + oemToClean);

			if (!System.IO.File.Exists(fullPath))
			{
				this.Invoke((MethodInvoker)delegate
				{
					CustomMessageBox.Show(Lang.MsgErrToolNotFound + "BSTCleaner.exe", Lang.MsgErrTitle, false);
				});
				return false;
			}

			try
			{
				SendStatusToVue(app.id, "DOWNLOAD_STATUS", new { status = "UNINSTALLING" });
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);

				using (Process p = new Process())
				{
					p.StartInfo.FileName = fullPath;
					p.StartInfo.WorkingDirectory = FolderPath;
					p.StartInfo.Arguments = $"-noui -oem {oemToClean}";
					p.StartInfo.UseShellExecute = true;
					p.EnableRaisingEvents = true;

					var tcs = new TaskCompletionSource<bool>();
					p.Exited += (s, ev) => tcs.SetResult(true);

					p.Start();
					tcs.Task.Wait();

					app.isConflict = false;
					app.conflictAppID = "";
					app.conflictAppName = "";

					SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
					return true;
				}
			}
			catch (Exception ex)
			{
				this.Invoke((MethodInvoker)delegate
				{
					CustomMessageBox.Show(Lang.MsgErrExecuteFile + ex.Message, Lang.MsgErrTitle, false);
				});
				return false;
			}
		}

		private void ExecuteAppUtility(string appId, string status, string exeName, string args = "")
		{
			var app = globalAppList.FirstOrDefault(a => a.id == appId);
			if (app == null || !app.isInstalled) return;

			string fullPath = Path.Combine(app.programPath, exeName);
			if (!System.IO.File.Exists(fullPath))
			{
				CustomMessageBox.Show(Lang.MsgErrToolNotFound + exeName, Lang.MsgErrTitle, false);
				return;
			}

			Task.Run(() =>
			{
				try
				{
					if (status != null)
					{
						SendStatusToVue(appId, "DOWNLOAD_STATUS", new { status = status });
						SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);
					}
					Main.WriteLog("INFO", $"Executing file {fullPath} {args}");

					using (Process p = new Process())
					{
						p.StartInfo.FileName = fullPath;
						p.StartInfo.WorkingDirectory = app.programPath;
						p.StartInfo.Arguments = args;
						p.StartInfo.UseShellExecute = true;
						p.EnableRaisingEvents = true;

						var tcs = new TaskCompletionSource<bool>();

						p.Exited += (s, ev) =>
						{
							tcs.SetResult(true);
						};

						p.Start();

						tcs.Task.Wait();

						SendStatusToVue(appId, "DOWNLOAD_COMPLETED", new { });
						SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
					}
				}
				catch (Exception ex)
				{
					WriteLog("INFO", $"File execution error: " + ex.Message);
					CustomMessageBox.Show(Lang.MsgErrExecuteFile + ex.Message, Lang.MsgErrTitle, false);
					SendStatusToVue(appId, "DOWNLOAD_FAILED", new { error = ex.Message });
				}
			});
		}

		private HttpListener discordListener;
		private readonly HttpClient httpClient = new HttpClient();

		private void HandleDiscordLogin()
		{
			Main.WriteLog("INFO", "Setting up the login portal using Discord.");
			string clientId = "1475485221028626483";

			string redirectUri = authURL;

			string discordAuthUrl = $"https://discord.com/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify+guilds.members.read+email";
			Main.WriteLog("INFO", "Requested URL: " + discordAuthUrl);

			try
			{
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = discordAuthUrl,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				Main.WriteLog("ERROR", "Unable to open the link");
				CustomMessageBox.Show(Lang.MsgErrOpenBrowser + ex.Message, Lang.MsgErrTitle, false);
			}
		}

		private async void ExchangeCodeForTokenAsync(string code)
		{
			string clientId = "1475485221028626483";
			string clientSecret = "5ATtI-m3quy2xg_GHoXoFqdGASACRwf0";
			string redirectUri = "https://shilukayt.github.io/DiscordAuth/";

			Main.WriteLog("INFO", "Getting token");
			using (HttpClient client = new HttpClient())
			{
				var values = new Dictionary<string, string>
				{
					{ "client_id", clientId },
					{ "client_secret", clientSecret },
					{ "grant_type", "authorization_code" },
					{ "code", code },
					{ "redirect_uri", redirectUri }
				};

				var content = new FormUrlEncodedContent(values);
				var response = await client.PostAsync("https://discord.com/api/oauth2/token", content);
				string responseString = await response.Content.ReadAsStringAsync();

				if (response.IsSuccessStatusCode)
				{
					Main.WriteLog("INFO", "Token retrieved successfully, now receiving the token.");
					var tokenJson = JObject.Parse(responseString);
					string accessToken = tokenJson["access_token"]?.ToString();

					if (!string.IsNullOrEmpty(accessToken))
					{
						Main.WriteLog("INFO", "Getting font");
						SaveSecureToken(accessToken);
						currentUserRoles = await FetchUserRolesFromDiscord(accessToken);
						GetData();

						var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
						userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
						var userResponse = await client.SendAsync(userRequest);

						if (userResponse.IsSuccessStatusCode)
						{
							string userString = await userResponse.Content.ReadAsStringAsync();
							var userJson = JObject.Parse(userString);

							string globalName = (string)userJson["global_name"];
							string username = (string)userJson["username"];

							var profile = new
							{
								id = userJson["id"]?.ToString(),
								name = !string.IsNullOrEmpty(globalName) ? globalName : username,
								username = username,
								email = userJson["email"]?.ToString(),
								avatar = userJson["avatar"] != null ? $"https://cdn.discordapp.com/avatars/{userJson["id"]}/{userJson["avatar"]}.png" : null
							};
							this.Invoke((MethodInvoker)delegate
							{
								var responseData = new { type = "USER_LOGGED_IN", data = profile };
								webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(responseData));
							});
						}
					}
				}
				else
				{
					CustomMessageBox.Show(Lang.MsgErrTokenExchange + responseString, Lang.MsgApiErrorTitle, false);
				}
			}
		}

		private async Task<bool> AutoLoginDiscord()
		{
			try
			{
				string savedToken = LoadSecureToken();

				if (!string.IsNullOrEmpty(savedToken))
				{
					var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
					userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);
					var userResponse = await httpClient.SendAsync(userRequest);
					currentUserRoles = await FetchUserRolesFromDiscord(savedToken);

					if (userResponse.IsSuccessStatusCode)
					{
						string userString = await userResponse.Content.ReadAsStringAsync();
						var userJson = JObject.Parse(userString);
						string globalName = (string)userJson["global_name"];
						string username = (string)userJson["username"];

						var profile = new
						{
							id = userJson["id"]?.ToString(),
							name = !string.IsNullOrEmpty(globalName) ? globalName : username,
							username = username,
							email = userJson["email"]?.ToString(),
							avatar = userJson["avatar"] != null ? $"https://cdn.discordapp.com/avatars/{userJson["id"]}/{userJson["avatar"]}.png" : null
						};
						this.Invoke((MethodInvoker)delegate
						{
							var responseData = new { type = "USER_LOGGED_IN", data = profile };
							webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(responseData));
						});
						return true;
					}
					else
					{
						System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"));
					}
				}
			}
			catch { }
			return false;
		}

		private void SaveSecureToken(string token)
		{
			try
			{
				byte[] plainBytes = Encoding.UTF8.GetBytes(token);

				byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

				Main.WriteLog("INFO", "Saving the token, the token is stored at: " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"));
				System.IO.File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"), encryptedBytes);

				System.IO.File.SetAttributes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"), System.IO.FileAttributes.Hidden);
			}
			catch (Exception ex)
			{
				Main.WriteLog("ERROR", "Token saving error: " + ex.Message);
				CustomMessageBox.Show(Lang.MsgErrSaveToken + ex.Message, Lang.MsgErrTitle, false);
			}
		}

		private string LoadSecureToken()
		{
			Main.WriteLog("INFO", "Loading token...");
			if (!System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"))) return null;

			try
			{
				byte[] encryptedBytes = System.IO.File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"));

				byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

				return Encoding.UTF8.GetString(plainBytes);
			}
			catch
			{
				Main.WriteLog("ERROR", "Token failed, unable to log in.");
				System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"));
				return null;
			}
		}

		private async Task<List<string>> FetchUserRolesFromDiscord(string accessToken)
		{
			try
			{
				Main.WriteLog("INFO", "Getting role");
				var request = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/users/@me/guilds/{officialGuildId}/member");
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

				var response = await httpClient.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					Main.WriteLog("INFO", "Role found, currently being categorized");
					string content = await response.Content.ReadAsStringAsync();
					var memberJson = JObject.Parse(content);

					return memberJson["roles"]?.ToObject<List<string>>() ?? new List<string>();
				}
				else if (response.StatusCode == HttpStatusCode.NotFound)
				{
					Main.WriteLog("INFO", "Not found because I haven't joined the server.");
					Task.Run(() =>
					{
						CustomMessageBox.Show(Lang.MsgNotJoinedServer, Lang.MsgNoticeTitle, false);
					});
				}
			}
			catch (Exception ex)
			{
				Main.WriteLog("ERROR", "Unable to select a role, will be classified with default parameters.");
				WriteLog("ERROR", "Can't get role: " + ex.Message);
			}

			return new List<string>();
		}

		private async Task ShowToastWithIconAsync(string appId, string title, string message, string iconUrl)
		{
			try
			{
				if (!Directory.Exists(TempFolder)) Directory.CreateDirectory(TempFolder);

				string localIconPath = Path.Combine(TempFolder, $"{appId}_icon.png");

				if (Uri.TryCreate(iconUrl, UriKind.Absolute, out Uri webUri))
				{
					using (var response = await httpClient.GetAsync(webUri))
					{
						if (response.IsSuccessStatusCode)
						{
							byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
							System.IO.File.WriteAllBytes(localIconPath, imageBytes);
						}
					}
				}

				var toast = new ToastContentBuilder()
					.AddText(title)
					.AddText(message);

				if (System.IO.File.Exists(localIconPath))
				{
					toast.AddAppLogoOverride(new Uri("file:///" + localIconPath), ToastGenericAppLogoCrop.Default);
				}

				toast.Show();
				WriteLog("INFO", "Toast display requested");
			}
			catch (Exception ex)
			{
				new ToastContentBuilder().AddText(title).AddText(message).Show();
				WriteLog("ERROR", "Toast error: " + ex.Message);
			}
		}

		private void SetTaskbarState(TaskbarProgressBarStatus state)
		{
			this.Invoke((MethodInvoker)delegate
			{
				if (_taskbarList == null)
				{
					_taskbarList = (ITaskbarList3)new TaskbarInstance();
					_taskbarList.HrInit();
				}
				_taskbarList.SetProgressState(this.Handle, state);
				WriteLog("INFO", "Taskbar status has been changed to: " + state);

			});
		}

		private void SetTaskbarProgressValue(int value, int total = 100)
		{
			this.Invoke((MethodInvoker)delegate
			{
				if (_taskbarList == null)
				{
					_taskbarList = (ITaskbarList3)new TaskbarInstance();
					_taskbarList.HrInit();
				}

				_taskbarList.SetProgressState(this.Handle, TaskbarProgressBarStatus.Normal);

				_taskbarList.SetProgressValue(this.Handle, (ulong)value, (ulong)total);
				WriteLog("INFO", "Taskbar status has been changed. Adjusted percentage: " + value);

			});
		}

		private bool isExiting = false;

		private bool CanExitApp()
		{
			if (isDownloading)
			{
				CustomMessageBox.Show(Lang.MsgWarnExitInstalling, Lang.MsgWarnTitle, false);
				WriteLog("INFO", "Unable to exit the app, the app is installing.");

				return false;
			}

			if (isRunning)
			{
				CustomMessageBox.Show(Lang.MsgWarnExitRunning, Lang.MsgWarnTitle, false);
				WriteLog("INFO", "Unable to exit the application, the application is running.");
				return false;
			}

			return true;
		}

		private void CleanupBeforeExit()
		{
			if (trayIcon != null)
			{
				trayIcon.Visible = false;
				trayIcon.Dispose();
			}
			discordClient?.Dispose();
			SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
			WriteLog("INFO", "Cleaning up.");
		}


		private void ShowApp()
		{
			Main.WriteLog("INFO", "Showing app");
			this.Show();
			this.WindowState = FormWindowState.Normal;
			this.Activate();

			if (webView21 != null && !webView21.IsDisposed && webView21.CoreWebView2 != null)
			{
				try
				{
					webView21.CoreWebView2.Resume();
					WriteLog("INFO", "Webview2 has been woken up.");
				}
				catch { }
			}
		}

		private void TrimTotalMemory()
		{
			try
			{
				WriteLog("INFO", "Cleaning RAM...");
				EmptyWorkingSet(System.Diagnostics.Process.GetCurrentProcess().Handle);
			}
			catch { }
		}


		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				if (minimizeToTrayOnClose)
				{
					e.Cancel = true;
					SuspendApp();
					WriteLog("INFO", "Hiding app.");
				}
				else
				{
					if (!CanExitApp())
					{
						Main.WriteLog("INFO", "Can't exit app =))");
						e.Cancel = true;
						return;
					}

					isExiting = true;

					CleanupBeforeExit();
					Main.WriteLog("INFO", "Closing app!");
				}
			}
		}

		private async void SuspendApp()
		{
			try
			{
				Main.WriteLog("INFO", "Suspend app");
				if (isExiting || this.IsDisposed) return;

				this.Hide();

				if (webView21 != null && !webView21.IsDisposed && webView21.CoreWebView2 != null)
				{
					await webView21.CoreWebView2.TrySuspendAsync();

					GC.Collect();
					GC.WaitForPendingFinalizers();
					TrimTotalMemory();
				}
			}
			catch (ObjectDisposedException)
			{
			}
			catch (InvalidCastException)
			{
			}
			catch (Exception ex)
			{
				Main.WriteLog("ERROR", "Error when suspending Webview2: " + ex.Message);
				System.Diagnostics.Debug.WriteLine("Lỗi nhẹ khi Suspend WebView2: " + ex.Message);
			}
		}

		private void TrayExit_Click(object sender, EventArgs e)
		{
			if (!CanExitApp()) return;

			isExiting = true;

			CleanupBeforeExit();
			Main.WriteLog("INFO", "Closing app");
			Application.Exit();
		}

		private void TrayIcon_DoubleClick(object sender, EventArgs e) => ShowApp();
		private void TrayOpen_Click(object sender, EventArgs e) => ShowApp();

		private async Task<List<GameApp>> GetSyncedAppListAsync(string jsonUrl)
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("User-Agent", "HieuGLLite-Launcher");
					string jsonString = await client.GetStringAsync(jsonUrl);

					List<GameApp> appList = JsonConvert.DeserializeObject<List<GameApp>>(jsonString);

					if (appList != null)
					{
						foreach (var app in appList)
						{
							CheckAppInstallation(app);
						}
					}

					return appList;
				}
			}
			catch (Exception ex)
			{
				this.Invoke((MethodInvoker)delegate
				{
					WriteLog("ERROR", "Lỗi mất kết nối khi đang lấy dữ liệu JSON", ex);

					string offlineFilePath = Path.Combine(Application.StartupPath, "Assets", "offline.html");
					if (System.IO.File.Exists(offlineFilePath) && webView21 != null && webView21.CoreWebView2 != null)
					{
						webView21.CoreWebView2.Navigate("file:///" + offlineFilePath.Replace("\\", "/"));
					}
					else
					{
						CustomMessageBox.Show("Không thể kết nối đến máy chủ, vui lòng kiểm tra lại Internet!", "Lỗi kết nối", false);
					}
				});

				return new List<GameApp>();
			}
		}

		private void CheckAppInstallation(GameApp app)
		{
			Main.WriteLog("INFO", $"Checking app installation for {app.id}!");
			if (string.IsNullOrEmpty(app.oem)) return;
			string regPath = $@"SOFTWARE\{app.oem}";

			app.isInstalled = false;
			app.isConflict = false;
			app.conflictAppName = "";

			try
			{
				using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
				using (RegistryKey key = baseKey.OpenSubKey(regPath))
				{
					if (key != null)
					{
						string installDir = key.GetValue("InstallDir")?.ToString();
						string registryAppID = key.GetValue("appID")?.ToString();
						string dataDir = key.GetValue("DataDir")?.ToString();

						string rawVersion = key.GetValue("Version")?.ToString();
						string cleanVersion = null;
						if (!string.IsNullOrEmpty(rawVersion))
						{
							string[] parts = rawVersion.Split('.');
							cleanVersion = parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : rawVersion;
						}

						string fullExePath = Path.Combine(installDir ?? "", app.exeName ?? "");

						if (!string.IsNullOrEmpty(installDir) && System.IO.File.Exists(fullExePath))
						{
							if (registryAppID == app.id)
							{
								app.isInstalled = true;
								app.programPath = installDir;
								app.dataPath = dataDir;
								app.installedVersion = cleanVersion;
							}
							else
							{
								app.isConflict = true;
								app.conflictAppID = registryAppID;
								app.programPath = installDir;

								var existingApp = globalAppList.FirstOrDefault(a => a.id == registryAppID);
								app.conflictAppName = existingApp != null ? existingApp.name : registryAppID;
							}
						}
					}
				}
			}
			catch { app.isInstalled = false; }
		}

		private void InitializeRPC()
		{
			Main.WriteLog("INFO", "Setting up Discord RPC");
			discordClient = new DiscordRpcClient("1475485221028626483");

			discordClient.OnReady += (sender, e) =>
			{
				Debug.WriteLine($"Discord RPC sẵn sàng cho user: {e.User.Username}");
				Main.WriteLog("INFO", $"Discord RPC is ready for user: {e.User.Username}");
			};

			discordClient.Initialize();

			UpdatePresence("", "Phiên bản: " + FE_version, "base", "");
		}

		public void UpdatePresence(string state, string details, string largeKey, string smallKey)
		{
			Main.WriteLog("INFO", $"Changing presence: State={state}, Details={details}, LargeKey={largeKey}, SmallKey={smallKey}");
			if (discordClient == null || discordClient.IsDisposed) return;

			discordClient.SetPresence(new RichPresence()
			{
				State = state,
				Details = details,
				Timestamps = Timestamps.Now,
				Assets = new Assets()
				{
					LargeImageKey = largeKey,
					LargeImageText = "",
					SmallImageKey = smallKey,
					SmallImageText = ""
				}
			});
		}

		private DateTime lastRoleFetchTime = DateTime.MinValue;

		public void GetData(bool forceRefresh = false)
		{
			Main.WriteLog("INFO", "Getting apps list");
			Task.Run(async () =>
			{
				if (forceRefresh || currentUserRoles == null || (DateTime.Now - lastRoleFetchTime).TotalMinutes > 2)
				{
					bool loginSuccess = await AutoLoginDiscord();
					if (loginSuccess)
					{
						lastRoleFetchTime = DateTime.Now;
					}
				}

				// Kiểm tra xem hệ thống đang dùng tiếng Anh hay Việt
				string langSuffix = currentLanguage.StartsWith("en") ? "_en" : "_vi";

				// Nối đuôi ngôn ngữ vào tên file (ví dụ: appsList_en.json)
				//string jsonUrl = jsonURL + $"appsList{langSuffix}.json";
				string jsonUrl = jsonURL + $"appsList.json";

				var allApps = await GetSyncedAppListAsync(jsonUrl);
				if (allApps == null) return;

				WriteLog("INFO", $"Total apps retrieved: {allApps.Count}");

				globalAppList = allApps;

				foreach (var app in globalAppList)
				{
					CheckAppInstallation(app);
				}

				List<string> userRoles = currentUserRoles ?? new List<string>();
				var filteredApps = globalAppList.Where(app =>
					string.IsNullOrEmpty(app.requiredRoleID) ||
					userRoles.Contains(app.requiredRoleID)
				).ToList();

				Main.WriteLog("INFO", "Posting app list to Web!");
				this.Invoke((MethodInvoker)delegate
				{
					webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
					{
						type = "APPS_DATA",
						data = filteredApps
					}));
				});
			});
		}

		public bool VerifySHA256(string filePath, string expectedHash)
		{
			Main.WriteLog("INFO", "Verifying SHA256 for the file: " + filePath);
			using (var sha256 = System.Security.Cryptography.SHA256.Create())
			{
				using (var stream = System.IO.File.OpenRead(filePath))
				{
					var hashBytes = sha256.ComputeHash(stream);
					var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
					return hashString == expectedHash.ToLower();
				}
			}
		}

		private double BytesToGB(long bytes)
		{
			return (double)bytes / (1024 * 1024 * 1024);
		}

		private double GetFreeSpaceGB(string path)
		{
			try
			{
				string targetPath = string.IsNullOrEmpty(path) ? Application.StartupPath : path;
				string driveName = Path.GetPathRoot(Path.GetFullPath(targetPath));

				DriveInfo drive = new DriveInfo(driveName);
				if (drive.IsReady)
				{
					return (double)drive.AvailableFreeSpace / (1024 * 1024 * 1024);
				}
			}
			catch { }
			return 0;
		}

		private void HandleInstallRequest(dynamic message)
		{

			string appId = message["appId"]?.ToString();
			string selectedPath = message["installPath"]?.ToString();
			if (isDownloading)
			{
				Main.WriteLog("WARNING", "Unable to install " + appId + " because another application is installing.");
				this.Invoke((MethodInvoker)delegate
				{
					CustomMessageBox.Show(Lang.MsgWarnAnotherInstalling, Lang.MsgNoticeTitle, false);
				});
				return;
			}

			var app = globalAppList.FirstOrDefault(a => a.id == appId);
			if (app == null) return;

			if (app.isVerifyRequired && !isSessionVerified)
			{
				Main.WriteLog("WARNING", "Unable to install because identity verification failed.");
				this.Invoke((MethodInvoker)delegate
				{
					CustomMessageBox.Show(Lang.MsgErrUnauthorizedAcess, Lang.MsgErrSecurityTitle, false);
				});

				Main.WriteLog("WARNING", "The installer has been cancelled.");
				SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
				return;
			}

			string checkPath = !string.IsNullOrEmpty(selectedPath) ? selectedPath :
							  (!string.IsNullOrEmpty(app.programPath) ? app.programPath : @"C:\");
			Main.WriteLog("INFO", "Checking free space for the " + checkPath + " folder.");

			double requiredGB = (double)app.requiredSize / (1024 * 1024 * 1024);
			double freeGB = GetFreeSpaceGB(checkPath);

			if (freeGB < requiredGB)
			{
				Main.WriteLog("WARNING", $"Insufficient space for installation, requires {requiredGB:F2}GB, currently has {freeGB:F2}GB.");

				SetTaskbarState(TaskbarProgressBarStatus.Error);
				this.Invoke((MethodInvoker)delegate
				{
					string msg = string.Format(Lang.MsgWarnInsufficientSpace, Path.GetPathRoot(checkPath), requiredGB, freeGB);
					CustomMessageBox.Show(msg, Lang.MsgWarnTitle, false);
				});

				Main.WriteLog("WARNING", "The installer has been cancelled.");
				SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
				SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
				return;
			}
			Task.Run(async () =>
			{
				if (app.isInstalled)
				{
					Main.WriteLog("INFO", "Submit a version change requested..");

					DialogResult result = DialogResult.None;

					this.Invoke((MethodInvoker)delegate
					{
						string msg = string.Format(Lang.MsgWarnChangeVersion, app.name);
						result = CustomMessageBox.Show(
							msg,
							Lang.MsgWarnChangeVersionTitle,
							true
						);
					});

					if (result == DialogResult.Yes)
					{
						Main.WriteLog("INFO", $"Uninstalling {app.id}.");

						bool cleanSuccess = await Task.Run(() => CleanConflictingApp(app));

						if (!cleanSuccess)
						{
							Main.WriteLog("WARNING", "The installer has been cancelled.");

							SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
							return;
						}
					}
					else
					{
						Main.WriteLog("WARNING", "The installer has been cancelled.");

						SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
						return;
					}
				}
				else if (app.isConflict)
				{
					Main.WriteLog("INFO", "Request to remove conflicting applications.");

					DialogResult result = DialogResult.None;
					string conflictName = !string.IsNullOrEmpty(app.conflictAppName) ? app.conflictAppName : "một phiên bản giả lập khác (cùng lõi hệ thống)";

					this.Invoke((MethodInvoker)delegate
					{
						string msg = string.Format(Lang.MsgWarnConflictFound, conflictName, app.name);
						result = CustomMessageBox.Show(
							msg,
							Lang.MsgWarnConflictTitle,
							true
						);
					});

					if (result == DialogResult.Yes)
					{
						Main.WriteLog("INFO", $"Uninstalling {app.id}.");
						bool cleanSuccess = await Task.Run(() => CleanConflictingApp(app));
						if (!cleanSuccess)
						{
							Main.WriteLog("WARNING", "The installer has been cancelled.");
							SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
							return;
						}
					}
					else
					{
						Main.WriteLog("WARNING", "The installer has been cancelled.");
						SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
						return;
					}
				}

				try
				{
					Main.WriteLog("INFO", $"Starting install {app.id}.");
					isDownloading = true;
					await StartComboDownloadAsync(appId, message);
				}
				finally
				{
					isDownloading = false;
				}
			});
		}

		private async Task StartComboDownloadAsync(string appid, dynamic data)
		{
			Main.WriteLog("INFO", $"Preparing the enviroment.");
			Directory.CreateDirectory(DownloadFolder);
			Directory.CreateDirectory(StateFolder);

			var cts = new CancellationTokenSource();
			cancellationTokens[appid] = cts;
			try
			{
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);
				string exePath = Path.Combine(DownloadFolder, data["exeName"].ToString());
				string androidPath = Path.Combine(DownloadFolder, data["androidName"].ToString());
				Main.WriteLog("INFO", $"Downloading {appid}.");
				if (!await DownloadStepAsync(appid, data["exeUrl"].ToString(), exePath, "DOWNLOADING_EXE", cts.Token))
				{
					isDownloading = false;
					SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
					return;
				}
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);
				if (!await DownloadStepAsync(appid, data["androidUrl"].ToString(), androidPath, "DOWNLOADING_ANDROID", cts.Token))
				{
					isDownloading = false;
					SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
					return;
				}
				if (!System.IO.File.Exists(exePath) || !System.IO.File.Exists(androidPath))
				{
					Main.WriteLog("ERROR", $"Files not found to install {appid}.");
					CustomMessageBox.Show(Lang.MsgErrMissingInstallFiles, Lang.MsgErrInstallFailedTitle, false);

					return;
				}

				SendStatusToVue(appid, "DOWNLOAD_STATUS", new { status = "VERIFYING" });
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);
				await Task.Delay(300);

				bool isExeOk = VerifySHA256(exePath, data["exeHash"]?.ToString());
				bool isAndroidOk = VerifySHA256(androidPath, data["androidHash"]?.ToString());

				if (isExeOk && isAndroidOk)
				{
					Main.WriteLog("INFO", $"SHA256 verification complete, application installation in progress.");
					await RunSilentInstallerAsync(appid, exePath, data["androidCode"].ToString(), data["installPath"].ToString());
				}
				else
				{
					Main.WriteLog("ERROR", $"Failure check, exe file status: {(isExeOk ? "PASS" : "FAIL")}, android file status: {(isAndroidOk ? "PASS" : "FAIL")}");
					SendStatusToVue(appid, "DOWNLOAD_FAILED", new { error = "Lỗi xác thực tệp (SHA256 Mismatch)!" });
					SetTaskbarState(TaskbarProgressBarStatus.Error);

					string exeStatus = isExeOk ? Lang.FileValid : Lang.FileCorrupted;
					string androidStatus = isAndroidOk ? Lang.FileValid : Lang.FileCorrupted;
					string detailMessage = string.Format(Lang.MsgErrHashMismatch, exeStatus, androidStatus);

					CustomMessageBox.Show(
						detailMessage,
						Lang.MsgHashMismatchTitle,
						false
					);
					Main.WriteLog("INFO", $"Cleaning files.");
					if (System.IO.File.Exists(exePath)) System.IO.File.Delete(exePath);
					if (System.IO.File.Exists(androidPath)) System.IO.File.Delete(androidPath);
					SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
				}
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine($"Download for {appid} was canceled.");
				Main.WriteLog("WARNING", "The installer has been cancelled.");
			}
			finally
			{
				isDownloading = false;

				cancellationTokens.Remove(appid);
				activeDownloads.Remove(appid);

				SendStatusToVue(appid, "DOWNLOAD_CANCELLED", new { });
				SetTaskbarState(TaskbarProgressBarStatus.NoProgress);

				WriteLog("INFO", $"Successfully released all processes for {appid}");
			}
		}
		private async Task<bool> DownloadStepAsync(string appid, string url, string savePath, string statusType, CancellationToken token)
		{
			using (MultiThreadedDownloader downloader = new MultiThreadedDownloader())
			{
				activeDownloads[appid] = downloader;
				downloader.CurrentStatusType = statusType;

				string statePath = Path.Combine(StateFolder, $"{appid}_{statusType}.json");

				downloader.OnProgressChanged += (percent, speed, downloaded) =>
				{
					Main.WriteLog("INFO", $"Download progress {percent}, speed {speed}, downloaded {downloaded}.");
					SendStatusToVue(appid, "DOWNLOAD_PROGRESS", new { percent, speed, downloaded, status = statusType });


					this.Invoke((MethodInvoker)delegate
					{
						SetTaskbarProgressValue((int)percent);
					});
				};

				if (System.IO.File.Exists(statePath))
				{
					Main.WriteLog("INFO", $"Resuming download.");
					try
					{
						var json = System.IO.File.ReadAllText(statePath);
						var package = JsonConvert.DeserializeObject<DownloadPackage>(json);
						package.Urls = new string[] { url };
						await downloader.ResumeFromPackageAsync(package);
					}
					catch
					{
						await downloader.StartDownloadAsync(url, savePath);
					}
				}
				else
				{
					await downloader.StartDownloadAsync(url, savePath);
				}

				if (token.IsCancellationRequested)
				{
					Main.WriteLog("INFO", $"Cancelling download.");
					return false;
				}

				Main.WriteLog("INFO", $"Removing state file.");
				if (System.IO.File.Exists(statePath)) System.IO.File.Delete(statePath);
				await Task.Delay(200);
			}
			return true;
		}

		private async Task RunSilentInstallerAsync(string appid, string exePath, string androidCode, string installPath)
		{
			var app = globalAppList.FirstOrDefault(a => a.id == appid);
			SendStatusToVue(appid, "DOWNLOAD_STATUS", new { status = "INSTALLING" });

			try
			{
				string args = $"-s -DefaultImageName={androidCode} -ImageToLaunch={androidCode} -PDDir=\"{installPath}\"";
				WriteLog("INFO", $"Executing file {exePath} with arguments: {args}");
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);
				using (Process p = new Process())
				{
					p.StartInfo.FileName = exePath;
					p.StartInfo.Arguments = args;
					p.StartInfo.UseShellExecute = true;
					p.EnableRaisingEvents = true;

					var tcs = new TaskCompletionSource<bool>();
					p.Exited += (s, e) =>
					{
						tcs.SetResult(p.ExitCode <= 1);
					};

					p.Start();
					bool processSuccess = await tcs.Task;

					await Task.Delay(2000);
					CheckAppInstallation(app);

					int retryCount = 0;
					while (!app.isInstalled && retryCount < 3)
					{
						await Task.Delay(2000);
						CheckAppInstallation(app);
						retryCount++;
					}

					if (app.isInstalled)
					{
						WriteLog("INFO", $"Installation successful, cleaning up in progress.");
						if (System.IO.File.Exists(exePath)) System.IO.File.Delete(exePath);

						string stateFile = Path.Combine(StateFolder, $"{appid}_state.json");
						if (System.IO.File.Exists(stateFile)) System.IO.File.Delete(stateFile);

						WriteLog("INFO", $"Updating the application's path.");
						app.dataPath = installPath;
						SaveAppListToDisk();

						isDownloading = false;

						SendStatusToVue(appid, "DOWNLOAD_COMPLETED", new { savedPath = app.programPath });
						ShowToastWithIconAsync(appid, Lang.ToastInstallTitle, string.Format(Lang.ToastInstallMessage, app.name), app.icon);
					}
					else
					{
						SendStatusToVue(appid, "DOWNLOAD_FAILED", new { error = "Cài đặt thất bại, bị xung đột hoặc lỗi hệ thống (Bộ cài đã hoàn tác)." });
						CustomMessageBox.Show(Lang.MsgErrInstallRolledBack, Lang.MsgErrInstallFailedTitle, false);
						SetTaskbarState(TaskbarProgressBarStatus.Error);
						WriteLog("ERROR", $"Installation failed, installer undone changes");
					}
				}
			}
			catch (Exception ex)
			{
				SendStatusToVue(appid, "DOWNLOAD_FAILED", new { error = ex.Message });
			}
			finally
			{
				activeDownloads.Remove(appid);
				SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
			}
		}

		private void SendStatusToVue(string appid, string type, object extraData)
		{
			this.Invoke((MethodInvoker)delegate
			{
				JObject response = JObject.FromObject(new { type = type, appId = appid });
				JObject extra = JObject.FromObject(extraData);

				response.Merge(extra, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });

				webView21.CoreWebView2.PostWebMessageAsJson(response.ToString());
			});
		}

		private void UpdateLocalAppStatus(string appid, bool isInstalled, string path)
		{
			var app = globalAppList.FirstOrDefault(a => a.id == appid);
			if (app != null)
			{
				app.isInstalled = isInstalled;
				app.dataPath = path;
				CheckAppInstallation(app);
			}
		}

		private void SaveAppListToDisk()
		{
			try
			{
				Directory.CreateDirectory(RootFolder);
				WriteLog("INFO", $"Saving app path");
				var saveList = globalAppList
					.Where(a => a.isInstalled)
					.Select(a => new LocalAppStatus
					{
						id = a.id,
						isInstalled = a.isInstalled,
						programPath = a.programPath,
						dataPath = a.dataPath,
						installedVersion = a.installedVersion
					}).ToList();
				string json = JsonConvert.SerializeObject(saveList, Formatting.Indented);
				WriteLog("INFO", $"{json}");
				System.IO.File.WriteAllText(ConfigFile, json);
			}
			catch (Exception ex)
			{
				WriteLog("ERROR", $"Save failed: {ex.Message}");
				Debug.WriteLine("Lưu thất bại: " + ex.Message);
			}
		}

		private bool minimizeToTrayOnClose = true;
		private string currentTheme = "system";

		private void LoadSettings()
		{
			Main.WriteLog("INFO", "Getting settings file...");
			if (System.IO.File.Exists(SettingsFile))
			{
				Main.WriteLog("INFO", "Settings file found, reading file...");
				try
				{
					string json = System.IO.File.ReadAllText(SettingsFile);

					var settings = JsonConvert.DeserializeObject<AppSettingsModel>(json);

					if (settings != null)
					{
						Main.WriteLog("INFO", "Reading successful, settings being applied.");
						minimizeToTrayOnClose = settings.minimizeToTray;
						currentTheme = settings.theme;
						currentLanguage = settings.language;
					}
				}
				catch (Exception ex)
				{
					Main.WriteLog("ERROR", "Reading failed, Error: " + ex.Message);
					System.Diagnostics.Debug.WriteLine("Lỗi đọc file Cài đặt JSON: " + ex.Message);
				}
			}
		}

		private void SaveSettings()
		{
			try
			{
				Main.WriteLog("INFO", "Creating folder: " + RootFolder);
				Directory.CreateDirectory(RootFolder);

				var settings = new AppSettingsModel
				{
					minimizeToTray = minimizeToTrayOnClose,
					theme = currentTheme,
					language = currentLanguage
				};
				Main.WriteLog("INFO", "Saving settings...");

				System.IO.File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
			}
			catch (Exception ex)
			{
				Main.WriteLog("ERROR", "Saving settings failed, error: " + ex.Message);
				System.Diagnostics.Debug.WriteLine("Lỗi lưu file Cài đặt JSON: " + ex.Message);
			}
		}

		private void webView21_Click(object sender, EventArgs e)
		{

		}
	}
}