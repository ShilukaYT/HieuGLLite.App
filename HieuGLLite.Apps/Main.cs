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
using Microsoft.WindowsAPICodePack.Shell;   // Đôi khi enum ProgressState nằm ở đây
using Microsoft.WindowsAPICodePack.Taskbar; // Để dùng TaskbarManager
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.Media.Playback;
using static System.Net.WebRequestMethods;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static HieuGLLite.Apps.AppModel;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;




namespace HieuGLLite.Apps
{
	public partial class Main : Form
	{
		// Biến thư viện cho ứng dụng có thể dùng chung
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
			public IntPtr lpData; // Đổi sang IntPtr cho an toàn tuyệt đối
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, ref COPYDATASTRUCT lParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool ChangeWindowMessageFilter(uint msg, uint dwFlag);

		public const uint MSGFLT_ADD = 1;
		private NotifyIcon trayIcon;
		private ContextMenuStrip trayMenu;

		private DiscordRpcClient discordClient;

		//Các biến toàn cục khác
		public bool isDevMode = true;

		public string AppName;
		public string version = "26.3.11";
		public int versioncode = 260311;

		public string FE_version;


		public bool isDownloading; // Biến trạng thái tải về (Dùng để Vue có thể bật loading khi cần)

		public bool isRunning; // Biến trạng thái app đang chạy hay không (Dùng để Vue đổi nút Play thành màu xanh khi đang mở app)

		public readonly string hostURL = "https://shilukayt.github.io/HieuGLLiteFE/";
		//public readonly string hostURL = "http://localhost:5173";

		public readonly string jsonURL = "https://raw.githubusercontent.com/ShilukaYT/HieuGLLiteFE/refs/heads/main/assets/json/";
		private readonly string authURL = "https://shilukayt.github.io/DiscordAuth/";

		private string officialGuildId = "1074226640894316655";

		private List<string> currentUserRoles = new List<string>();

		private MultiThreadedDownloader _downloader; // Biến quản lý quá trình tải về, có thể dùng chung cho nhiều app nếu cần

		private List<GameApp> globalAppList = new List<GameApp>();
		// Quản lý các cỗ máy đang chạy
		private Dictionary<string, MultiThreadedDownloader> activeDownloads = new Dictionary<string, MultiThreadedDownloader>();
		// Lưu trữ CancellationTokenSource cho từng app đang tải
		private Dictionary<string, CancellationTokenSource> cancellationTokens = new Dictionary<string, CancellationTokenSource>();

		// --- QUY HOẠCH ĐƯỜNG DẪN HỆ THỐNG ---
		private static string RootFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps");
		private static string DownloadFolder => Path.Combine(RootFolder, "Downloads");
		private static string StateFolder => Path.Combine(RootFolder, "DownloadStates");
		private static string ConfigFile => Path.Combine(RootFolder, "installed_apps.json");
		private static string SettingsFile => Path.Combine(RootFolder, "launcher_settings.json");
		private static string TempFolder => Path.Combine(RootFolder, "Temp");

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
			// ... các hàm khác có thể lược bỏ nếu không dùng
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

		public Main()
		{
			SetCurrentProcessExplicitAppUserModelID("HieuGLLite.Launcher.v1");
			InitializeComponent();
			ChangeWindowMessageFilter(WM_COPYDATA, MSGFLT_ADD);
			LoadSettings();
			this.Resize += Main_Resize;

			// Đổi màu nền dựa theo file JSON
			bool isDark = currentTheme == "dark" || (currentTheme == "system" && IsWindowsDarkMode());
			this.BackColor = isDark ? Color.FromArgb(18, 18, 18) : Color.White;
		}
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_COPYDATA)
			{
				// Giải mã con trỏ thành Struct
				COPYDATASTRUCT cds = (COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof(COPYDATASTRUCT));
				// Đọc chuỗi từ vùng nhớ
				string receivedUrl = Marshal.PtrToStringUni(cds.lpData);

				// NẾU APP 1 NHẬN ĐƯỢC, NÓ PHẢI HIỆN BẢNG NÀY LÊN:
				//MessageBox.Show("App 1 ĐÃ CHỘP ĐƯỢC LINK: " + receivedUrl);

				if (!string.IsNullOrEmpty(receivedUrl) && receivedUrl.Contains("code="))
				{
					// Cắt chuỗi thông minh, bất chấp việc có dấu "/" hay không
					int startIndex = receivedUrl.IndexOf("code=") + 5;
					string code = receivedUrl.Substring(startIndex);

					// Xóa các tham số rác phía sau (nếu có)
					if (code.Contains("&"))
					{
						code = code.Substring(0, code.IndexOf("&"));
					}

					// Đưa Launcher lên mặt đất
					this.Show();
					this.WindowState = FormWindowState.Normal;
					this.Activate();

					// 🚀 BẮN CODE ĐI ĐỔI TOKEN NGAY VÀ LUÔN!
					ExchangeCodeForTokenAsync(code);
				}
			}

			base.WndProc(ref m);
		}
		private async void Main_Load(object sender, EventArgs e)
		{
			AppName = isDevMode ? "Hieu GL Lite (Developer mode)" : "Hieu GL Lite (beta)";
			SetupSystemTray();
			InitializeRPC();

			// 1. TÍNH TOÁN THEME TỪ BIẾN JSON ĐÃ LOAD Ở HÀM CONSTRUCTOR
			bool isDarkTheme = currentTheme == "dark" || (currentTheme == "system" && IsWindowsDarkMode());

			// 2. ĐỒNG BỘ THEME CHO WINFORMS (Thanh Title, Nút X/Min/Max)
			ApplyTheme(isDarkTheme);

			this.Text = AppName;

			// 3. ĐỒNG BỘ THEME CHO WEBVIEW2 TRƯỚC KHI KHỞI TẠO (Chống chớp trắng/đen màn hình)
			webView21.DefaultBackgroundColor = isDarkTheme ? Color.FromArgb(18, 18, 18) : Color.White;
			webView21.ZoomFactor = 0.8;

			// Đăng ký các sự kiện
			webView21.CoreWebView2InitializationCompleted += WebView21_CoreWebView2InitializationCompleted;
			webView21.WebMessageReceived += WebView_WebMessageReceived;
			webView21.NavigationCompleted += webview21_NavigationCompleted;

			// 4. Khởi tạo môi trường dữ liệu người dùng
			var env = await CoreWebView2Environment.CreateAsync(null, RootFolder);

			await webView21.EnsureCoreWebView2Async(env);

			webView21.CoreWebView2.SetVirtualHostNameToFolderMapping(
			"hieugllite.app",
			System.IO.Path.Combine(Application.StartupPath, "Assets"),
			CoreWebView2HostResourceAccessKind.Allow
			);

			webView21.Source = new Uri(isDevMode ? "http://localhost:5173" : hostURL);
		}
		private void SetupSystemTray()
		{
			trayMenu = new ContextMenuStrip();
			trayMenu.Items.Add("Mở " + AppName, null, TrayOpen_Click);
			trayMenu.Items.Add("Thoát hoàn toàn", null, TrayExit_Click);

			trayIcon = new NotifyIcon();
			trayIcon.Text = AppName;

			// MẸO TEST: Dùng icon mặc định của Windows để chắc chắn 100% nó hiển thị
			trayIcon.Icon = this.Icon;

			// Nếu bạn có file .ico riêng thì dùng dòng này (bỏ comment):
			// trayIcon.Icon = new Icon("duong_dan_toi_file_icon.ico");

			trayIcon.ContextMenuStrip = trayMenu;
			trayIcon.DoubleClick += TrayIcon_DoubleClick;

			// ÉP HIỆN NGAY LẬP TỨC
			trayIcon.Visible = true;
		}

		// 1. Hàm tính toán và áp dụng Zoom dựa trên chiều rộng (Width)
		private void UpdateWebViewZoom()
		{
			// Kiểm tra xem WebView2 đã khởi tạo xong chưa để tránh lỗi
			if (webView21 == null || webView21.CoreWebView2 == null) return;

			int width = this.ClientSize.Width;
			double newZoom = 1.0;

			// Logic phân cấp theo yêu cầu của bạn
			if (width <= 1280)
			{
				newZoom = 0.8;
			}
			else if (width <= 1800)
			{
				newZoom = 0.9;
			}
			else if (width <= 2560)
			{
				newZoom = 1.0;
			}
			else {
				newZoom = 1.3;
			}

			// Chỉ cập nhật nếu giá trị Zoom có sự thay đổi để tránh giật lag
			if (webView21.ZoomFactor != newZoom)
			{
				webView21.ZoomFactor = newZoom;
			}
		}

		// 2. Sự kiện xảy ra mỗi khi cửa sổ thay đổi kích thước
		private void Main_Resize(object sender, EventArgs e)
		{
			UpdateWebViewZoom();
		}

		private void WebView21_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
		{
			if (e.IsSuccess)
			{
				// Cấu hình WebView2 sau khi khởi tạo thành công

				webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = isDevMode ? true : false;
				webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = isDevMode ? true : false;
				webView21.CoreWebView2.Settings.AreDevToolsEnabled = isDevMode ? true : false;
				webView21.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = isDevMode ? true : false;
				webView21.CoreWebView2.Settings.IsZoomControlEnabled = isDevMode;

			}
			else
			{
				MessageBox.Show("WebView2 initialization failed: " + e.InitializationException);
			}
		}

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
				// 6. Gửi lệnh "Tắt Loading" xuống Vue (Khi HTML đã load xong)
				var syncData = new { type = "INIT_COMPLETED" };
				webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(syncData));
			}
			else
			{
				// Xử lý khi mất mạng/Lỗi kết nối
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
						using (FolderBrowserDialog fbd = new FolderBrowserDialog())
						{
							if (fbd.ShowDialog(this) == DialogResult.OK)
							{
								// Gửi nguyên bản, ví dụ: "D:\" hoặc "D:\Games"
								string selectedPath = fbd.SelectedPath;

								var response = new
								{
									type = "FOLDER_SELECTED",
									path = selectedPath
								};
								// JsonConvert sẽ tự biến "D:\" thành "D:\\" trong chuỗi JSON (đúng chuẩn)
								webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(response));
							}
						}
					});
					break;

				case "RELOAD_APP":
					this.Invoke((MethodInvoker)delegate
					{
						webView21.Source = new Uri("http://localhost:5173/");
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
						newTitle = title + " - " + AppName;
					}
					else

					{
						newTitle = AppName;
					}
						;


					this.Invoke((MethodInvoker)delegate { this.Text = newTitle; });
					break;
				case "OPEN_URL":

					string url = message["url"]?.ToString();
					string feature = message["feature"]?.ToString(); // Có thể dùng để phân biệt loại liên kết (hỗ trợ sau)
					if (!string.IsNullOrEmpty(url))
					{
						try
						{
							System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
							{
								FileName = url,
								UseShellExecute = true
							});
						}
						catch (Exception ex)
						{
							MessageBox.Show("Không thể mở liên kết: " + ex.Message);
						}
					}
					break;

				case "LOGIN_DISCORD":
					// Bỏ cái Task.Run vào đây để app không bị đơ lúc chờ mở trình duyệt
					Task.Run(() =>
					{
						HandleDiscordLogin();
					});
					GetData();
					break;

				case "CHECK_AUTO_LOGIN":
					// Vue vừa bật lên sẽ gọi cái này
					Task.Run(() => { AutoLoginDiscord(); });
					break;

				case "LOGOUT_DISCORD":
					// Vue gọi khi bấm nút Đăng xuất
					if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat")))
					{
						System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat")); // Xóa trí nhớ
					}
					currentUserRoles = new List<string>();
					GetData();
					break;

				case "GET_APPS":
					{
						// Thay vì dùng TryGetValue dài dòng, ta dùng toán tử an toàn (?.Value<bool>()) của Newtonsoft.Json
						// Nó sẽ tự tìm key "force", nếu có thì lấy giá trị true/false, nếu không có thì mặc định lấy false (?? false)
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
									// 1. CẬP NHẬT BIẾN GLOBAL LÀ ĐANG CÓ APP CHẠY
									isRunning = true;

									// 2. BÁO VUE: ĐỔI NÚT APP NÀY THÀNH "ĐANG MỞ..." VÀ ẨN LAUNCHER
									this.Invoke((MethodInvoker)delegate
									{
										webView21.CoreWebView2.PostWebMessageAsJson($@"{{
                                    ""type"": ""APP_STATE_CHANGED"", 
                                    ""appId"": ""{appId}"", 
                                    ""isRunning"": true 
                                }}");

										this.Hide();
									});

									// 3. CẤU HÌNH GỌI APP
									System.Diagnostics.Process appProcess = new System.Diagnostics.Process();
									appProcess.StartInfo.FileName = exePath;
									appProcess.StartInfo.WorkingDirectory = app.programPath;
									appProcess.StartInfo.UseShellExecute = true;
									appProcess.EnableRaisingEvents = true;

									// 4. ĐỊNH NGHĨA VIỆC SẼ LÀM KHI APP NÀY TẮT
									appProcess.Exited += (sender, e) =>
									{
										// Xóa ID của cửa sổ vừa tắt khỏi danh sách của App này
										app.runningProcessIds.Remove(appProcess.Id);

										// --- ĐIỂM DANH TOÀN CỤC ---
										// Kiểm tra xem trong toàn bộ Launcher, còn bất kỳ app nào đang có ID chạy không?
										// Nếu không còn bất kỳ app nào chạy -> isRunning global mới được phép = false
										isRunning = globalAppList.Any(a => a.runningProcessIds.Count > 0);

										// --- XỬ LÝ GIAO DIỆN APP CỤ THỂ ---
										// Nếu riêng cái App này (VD: BlueStacks) không còn cửa sổ nào chạy nữa
										if (app.runningProcessIds.Count == 0)
										{
											this.Invoke((MethodInvoker)delegate
											{
												// Báo Vue trả lại nút xanh cho riêng App này
												webView21.CoreWebView2.PostWebMessageAsJson($@"{{
                                            ""type"": ""APP_STATE_CHANGED"", 
                                            ""appId"": ""{appId}"", 
                                            ""isRunning"": false 
                                        }}");

												// Nếu TOÀN BỘ các game đều đã tắt (isRunning global = false), mới hiện lại Launcher
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

									// 5. BÙM! MỞ APP THÔI
									if (appProcess.Start())
									{
										// LƯU LẠI PID VÀO DANH SÁCH CỦA APP NÀY
										app.runningProcessIds.Add(appProcess.Id);
										string large = !string.IsNullOrEmpty(app.rpc_large_key) ? app.rpc_large_key : "logo";
										string small = !string.IsNullOrEmpty(app.rpc_small_key) ? app.rpc_small_key : "";

										//MessageBox.Show($"RPC large value: "+ large +"\nRPC small value: "+ small);

										// Cập nhật trạng thái Discord
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
									MessageBox.Show($"Không tìm thấy file thực thi tại: {exePath}");
								}
							}
						}
						catch (Exception ex)
						{
							MessageBox.Show($"Không thể khởi động ứng dụng: {ex.Message}");
						}
					});
					break;
				case "KILL_APP":

					Task.Run(() =>
					{
						var app = globalAppList.FirstOrDefault(a => a.id == message["appId"]?.ToString());

						// Nếu app tồn tại và cuốn sổ đang có ID
						if (app != null && app.runningProcessIds.Count > 0)
						{
							var result = MessageBox.Show("Bạn có chắc muốn tắt ứng dụng này?\nTiến trình chơi của bạn có thể sẽ không được lưu lại!", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
							if (result == DialogResult.Yes)
							{
								// Copy danh sách ra một mảng tạm để tránh lỗi khi vòng lặp đang chạy mà sự kiện Exited nhảy vào xóa ID
								var pidsToKill = app.runningProcessIds.ToList();

								foreach (int pid in pidsToKill)
								{
									try
									{
										var p = System.Diagnostics.Process.GetProcessById(pid);
										p.Kill(); // "Bóp cổ" từng clone một!
									}
									catch
									{
										// Bỏ qua nếu clone đó đã tự tắt trước đó
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

						// CẬP NHẬT DISCORD NGAY LẬP TỨC VỚI PHIÊN BẢN FE VỪA NHẬN
						UpdatePresence("", $" Phiên bản: {FE_version}", "base", "");
					});
					break;

				case "INSTALL":
					// Thay vì viết hàng trăm dòng code ở đây, ta chỉ cần gọi hàm điều phối
					// Hàm này sẽ tự lo việc tải EXE -> tải Android -> Verify -> Cài đặt
					HandleInstallRequest(message);
					break;

				case "PAUSE_DOWNLOAD":
					string pId = message["appId"].ToString();
					if (activeDownloads.TryGetValue(pId, out var pDownloader))
					{
						pDownloader.Pause();
						var package = pDownloader.GetCurrentPackage();
						if (package != null)
						{
							package.Urls = new string[0]; // Bảo mật: Xóa URL
							string stateFile = Path.Combine(StateFolder, $"{pId}_{pDownloader.CurrentStatusType}.json");
							System.IO.File.WriteAllText(stateFile, JsonConvert.SerializeObject(package));
						}
						SendStatusToVue(pId, "DOWNLOAD_STATUS", new { status = "PAUSED" });

					}
					break;

				case "CANCEL_DOWNLOAD":
					var confirmResult = MessageBox.Show("Bạn có chắc muốn hủy tải ứng dụng này?\nMọi tiến trình và dữ liệu tạm thời sẽ bị xóa sạch!", "Xác nhận hủy tải", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
					if (confirmResult == DialogResult.Yes && message["appId"] != null)
					{
						string appIdToCancel = message["appId"].ToString();

						// 1. "Đánh thức" downloader nếu nó đang bị Pause
						if (activeDownloads.TryGetValue(appIdToCancel, out var downloader))
						{
							// Nếu đang dừng, phải cho nó chạy lại thì lệnh Cancel mới có tác dụng
							downloader.Resume();
							downloader.Cancel(); // Gửi lệnh hủy cho MultiThreadedDownloader
						}

						// 2. Kích hoạt công tắc hủy tổng lực để ngắt chuỗi Task
						if (cancellationTokens.TryGetValue(appIdToCancel, out var cts))
						{
							cts.Cancel();

							// Dọn dẹp tệp tin trạng thái để không bị Resume "lén" lần sau
							foreach (var f in Directory.GetFiles(StateFolder, $"{appIdToCancel}_*.json"))
								System.IO.File.Delete(f);
						}

						//trả về status đã hủy cho Vue để nó ẩn progressbar và hiện lại nút Install
						SendStatusToVue(appIdToCancel, "DOWNLOAD_STATUS", new { status = "DOWNLOAD_CANCELLED" });
					}
					break;

				case "RESUME_DOWNLOAD":
					string rId = message["appId"].ToString();
					if (activeDownloads.TryGetValue(rId, out var rDownloader))
					{
						rDownloader.Resume(); // Chạy tiếp từ byte đang dừng
											  // C# sẽ tự gửi lại Progress, Vue sẽ tự nhảy status ra khỏi 'PAUSED'
					}
					break;


				case "SYNC_DOWNLOAD_STATUS":
					var syncData = appRunningStates.Select(kvp => new
					{
						appId = kvp.Key,
						status = kvp.Value.Status,   // Trả về "VERIFYING", "INSTALLING"...
						percent = kvp.Value.Percent,
						isDownloading = kvp.Value.Status == "DOWNLOADING"
					}).ToList();

					webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
					{
						type = "DOWNLOAD_SYNC_DATA",
						downloads = syncData
					}));
					break;
				//else if (type == "DRAG_WINDOW")
				//{
				//	this.Invoke((MethodInvoker)delegate
				//	{
				//		ReleaseCapture();
				//		SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
				//	});
				//}
				//else if (type == "MINIMIZE_WINDOW")
				//{
				//	this.Invoke((MethodInvoker)delegate { this.WindowState = FormWindowState.Minimized; });
				//}
				case "CLOSE_WINDOW":
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
					var result = MessageBox.Show("Bạn có muốn gỡ cài đặt ứng dụng này không?\nTất cả ứng dụng và trò chơi được cài đặt trên ứng dụng này sẽ bị gỡ cài đặt!", "Gỡ cài đặt", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
					if (result == DialogResult.Yes)
					{
						UninstallApp(message["appId"]?.ToString());
					}
					else return;
					break;
				case "CLEAR_CACHE":
					CleanLauncherInternalFilesAsync();

					break;
				case "UNINSTALL_APP":
					string protocolName = "hieugllite";
					var uninstallConfirm = MessageBox.Show(
						"Bạn có muốn gỡ cài đặt ứng dụng này không?\nMột số giả lập sẽ không thể khởi động nếu không có ứng dụng này?",
						"Xác nhận gỡ cài đặt",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning
					);

					if (uninstallConfirm == DialogResult.Yes)
					{

						webView21.Stop();
						// 1. GỠ GIAO THỨC REGISTRY (Giữ nguyên của bạn)
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
							MessageBox.Show("Cần quyền Quản trị viên (Run as Administrator) để gỡ bỏ hoàn toàn giao thức hệ thống.", "Thiếu quyền", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}
						catch (Exception ex)
						{
							MessageBox.Show($"Lỗi gỡ Registry: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
						}

						// 2. GIẢI PHÓNG WEBVIEW2 ĐỂ MỞ KHÓA FOLDER
						if (webView21 != null)
						{
							webView21.Dispose(); // Ép WebView2 nhả các file đang ngậm trong RootFolder ra
						}

						// 3. XÓA DỮ LIỆU ROOT FOLDER
						try
						{
							if (System.IO.Directory.Exists(RootFolder))
							{
								// BẮT BUỘC PHẢI CÓ CHỮ 'true' ĐỂ XÓA XUYÊN TẤT CẢ FILE CON
								System.IO.Directory.Delete(RootFolder, true);
							}
						}
						catch (Exception ex)
						{
							// Không nên dùng MessageBox ở đây, nếu xóa không được 1 vài file rác thì cứ lờ đi rồi thoát app là đẹp nhất
							Console.WriteLine($"Không thể xóa sạch thư mục: {ex.Message}");
						}

						// 4. GỌI UNINSTALLER NGOÀI (NẾU BẠN MUỐN XÓA LUÔN FILE .EXE CỦA LAUNCHER NÀY)
						// Lưu ý: 1 phần mềm đang chạy KHÔNG THỂ TỰ XÓA FILE .EXE CỦA CHÍNH NÓ.
						// Bắt buộc phải mượn tay 1 tool bên ngoài (như Uninstall.exe của Inno Setup)
						/*
						try
						{
							Process process = new Process();
							process.StartInfo.FileName = Path.Combine(Application.StartupPath, "unins000.exe"); // Tên tool gỡ cài đặt
							process.StartInfo.UseShellExecute = true;
							process.Start();
						}
						catch { }
						*/

						// 5. RÚT ĐIỆN APP
						Application.Exit();
					}
					break;

				case "GET_SETTINGS":
					// Vue yêu cầu lấy Cài đặt lúc vừa khởi động
					this.Invoke((MethodInvoker)delegate {
						webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
						{
							type = "SYNC_SETTINGS",
							theme = currentTheme,
							minimizeToTray = minimizeToTrayOnClose
						}));
					});
					break;

				case "THEME_CHANGED":
					currentTheme = message["mode"]?.ToString() ?? "system";
					SaveSettings(); // LƯU VÀO JSON
					this.Invoke((MethodInvoker)delegate {
						bool isDarkTheme = currentTheme == "dark" || (currentTheme == "system" && IsWindowsDarkMode());
						ApplyTheme(isDarkTheme);
					});
					break;

				case "SET_CLOSE_BEHAVIOR":
					minimizeToTrayOnClose = message["minimizeToTray"]?.Value<bool>() ?? true;
					SaveSettings(); // LƯU VÀO JSON
					break;

				//case "INSTALL_MULTI":
				//	Task.Run(async () =>
				//	{
				//		string appId = message["appId"]?.ToString();
				//		var app = globalAppList.FirstOrDefault(a => a.id == appId);

				//		if (app == null || !app.isInstalled || string.IsNullOrEmpty(app.dataPath))
				//		{
				//			this.Invoke((MethodInvoker)delegate { MessageBox.Show("Không tìm thấy đường dẫn Data!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); });
				//			return;
				//		}

				//		string androidCode = message["androidCode"]?.ToString();
				//		string androidUrl = message["androidUrl"]?.ToString();

				//		var cts = new CancellationTokenSource();
				//		cancellationTokens[appId] = cts;

				//		try
				//		{
				//			isDownloading = true;
				//			SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);

				//			// =========================================================
				//			// BƯỚC 1: ÉP BLUESTACKS TẢI BẢN GỐC TRƯỚC VÀ CHỜ ĐỢI
				//			// =========================================================
				//			// Báo Vue hiện chữ "Đang chờ tải xuống..." để khách hàng biết
				//			SendStatusToVue(appId, "DOWNLOAD_STATUS", new { status = "WAITING" });

				//			string multiManagerPath = Path.Combine(app.programPath, "HD-MultiInstanceManager.exe");
				//			if (System.IO.File.Exists(multiManagerPath))
				//			{
				//				using (Process p = new Process())
				//				{
				//					p.StartInfo.FileName = multiManagerPath;
				//					p.StartInfo.Arguments = $"--cmd installImage --imageName {androidCode}";
				//					p.StartInfo.UseShellExecute = false;
				//					p.StartInfo.CreateNoWindow = true;
				//					p.Start();
				//					// KHÔNG dùng await Task.Delay hay p.Exited ở đây nữa vì process mồi sẽ thoát ngay lập tức
				//				}

				//				// --- BẮT ĐẦU CHIẾN THUẬT: THEO DÕI KHÓA TỆP (FILE LOCK POLLING) ---
				//				string engineFolderCheck = Path.Combine(app.dataPath, "Engine", androidCode);
				//				string vhdPath = Path.Combine(engineFolderCheck, "Root.vhd");
				//				string vhdxPath = Path.Combine(engineFolderCheck, "Root.vhdx");

				//				bool isBlueStacksDone = false;

				//				// Vòng lặp kiểm tra liên tục mỗi 2 giây
				//				while (!isBlueStacksDone)
				//				{
				//					cts.Token.ThrowIfCancellationRequested(); // Vẫn cho phép khách bấm Hủy nếu đợi lâu quá

				//					bool fileExists = System.IO.File.Exists(vhdPath) || System.IO.File.Exists(vhdxPath);

				//					if (fileExists)
				//					{
				//						string targetPath = System.IO.File.Exists(vhdPath) ? vhdPath : vhdxPath;
				//						try
				//						{
				//							// Cố gắng mở file với quyền Ghi. 
				//							// NẾU GIẢ LẬP ĐANG TẢI: File đang bị khóa -> Văng lỗi IOException ngay lập tức
				//							using (FileStream fs = System.IO.File.Open(targetPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
				//							{
				//								// NẾU LỌT VÀO ĐÂY: Mở thành công -> Giả lập đã nhả file ra -> ĐÃ TẢI XONG!
				//								isBlueStacksDone = true;
				//							}
				//						}
				//						catch (IOException)
				//						{
				//							// Bị văng lỗi tức là giả lập vẫn đang tải, im lặng đợi tiếp...
				//						}
				//					}

				//					// Nếu chưa xong thì nghỉ 2 giây rồi gõ cửa check tiếp
				//					if (!isBlueStacksDone)
				//					{
				//						await Task.Delay(2000, cts.Token);
				//					}
				//				}
				//			}

				//			// =========================================================
				//			// BƯỚC 2: TẢI BẢN MOD TỪ JSON CỦA BẠN (Định dạng ZIP)
				//			// =========================================================

				//			// Lấy tên file từ JSON (thông qua Vue gửi xuống)
				//			string androidName = message["androidName"]?.ToString() ?? $"{androidCode}_Mod.zip";

				//			// Hứng luôn pass giải nén (nếu có)
				//			string zipPassword = message["zipPassword"]?.ToString();

				//			string tempZipPath = Path.Combine(DownloadFolder, androidName);

				//			bool success = await DownloadStepAsync(appId, androidUrl, tempZipPath, "DOWNLOADING_ANDROID", cts.Token);

				//			// THÊM IF SUCCESS VÀO ĐÂY ĐỂ ĐẢM BẢO TẢI XONG MỚI CHẠY TIẾP
				//			if (success)
				//			{
				//				SendStatusToVue(appId, "DOWNLOAD_STATUS", new { status = "INSTALLING" });

				//				// KHAI BÁO ENGINE FOLDER Ở ĐÂY
				//				string engineFolder = Path.Combine(app.dataPath, "Engine", androidCode);

				//				// =========================================================
				//				// BƯỚC 3: GIẢI NÉN VÀ GHI ĐÈ VÀO THƯ MỤC ENGINE (SHARPCOMPRESS)
				//				// =========================================================

				//				var readerOptions = new SharpCompress.Readers.ReaderOptions
				//				{
				//					Password = string.IsNullOrEmpty(zipPassword) ? null : zipPassword
				//				};

				//				using (var archive = ZipArchive.OpenArchive(tempZipPath, readerOptions))
				//				{
				//					foreach (var entry in archive.Entries)
				//					{
				//						if (!entry.IsDirectory)
				//						{
				//							try
				//							{
				//								entry.WriteToDirectory(engineFolder, new ExtractionOptions()
				//								{
				//									ExtractFullPath = true,
				//									Overwrite = true
				//								});
				//							}
				//							catch (Exception ex)
				//							{
				//								Debug.WriteLine($"Lỗi giải nén file {entry.Key}: {ex.Message}");
				//								throw new Exception("Lỗi giải nén: Sai mật khẩu hoặc file nén bị hỏng!");
				//							}
				//						}
				//					}
				//				}

				//				// =========================================================
				//				// BƯỚC 4: XÁC ĐỊNH PHIÊN BẢN VÀ PATCH XML (BYPASS BẢO MẬT)
				//				// =========================================================
				//				if (IsVersionGreaterOrEqual(app.installedVersion, "5.22.140"))
				//				{
				//					PatchBstkFile(engineFolder, androidCode);
				//				}

				//				// =========================================================
				//				// BƯỚC 5: DỌN DẸP VÀ ĐỒNG BỘ GIAO DIỆN
				//				// =========================================================
				//				string confFile = Path.Combine(app.dataPath, "bluestacks.conf");
				//				app.instances = ParseBlueStacksInstances(confFile);

				//				if (System.IO.File.Exists(tempZipPath)) System.IO.File.Delete(tempZipPath);

				//				SendStatusToVue(appId, "DOWNLOAD_COMPLETED", new { });
				//				this.Invoke((MethodInvoker)delegate {
				//					MessageBox.Show(
				//						$"Đã cài và Bypass thành công bản Mod {androidCode} vào lõi giả lập!\n\n" +
				//						"Vui lòng tạo bản sao mới trong Trình quản lý đa phiên bản để trải nghiệm.",
				//						"Tuyệt vời", MessageBoxButtons.OK, MessageBoxIcon.Information);
				//				});
				//			}
				//			else
				//			{
				//				SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
				//			}
				//		}
				//		catch (Exception ex)
				//		{
				//			SendStatusToVue(appId, "DOWNLOAD_FAILED", new { error = ex.Message });
				//		}
				//		finally
				//		{
				//			isDownloading = false;
				//			cancellationTokens.Remove(appId);
				//			activeDownloads.Remove(appId);
				//			SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
				//		}
				//	});
				//	break;
				case "default":
					MessageBox.Show("Yêu cầu không hợp lệ: " + type);
					break;
			}
		}

		// Trong hàm tải hoặc cài đặt, mỗi khi đổi trạng thái:
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
				// 1. Xóa dữ liệu duyệt web của WebView2 (Cache, LocalStorage...)
				if (webView21.CoreWebView2 != null)
				{
					await webView21.CoreWebView2.Profile.ClearBrowsingDataAsync(
						CoreWebView2BrowsingDataKinds.DiskCache |
						CoreWebView2BrowsingDataKinds.LocalStorage
					);
				}

				// 2. Dọn dẹp thư mục Temp (Xóa các tệp icon Toast đã tải về)
				if (Directory.Exists(TempFolder))
				{
					DirectoryInfo di = new DirectoryInfo(TempFolder);
					foreach (FileInfo file in di.GetFiles())
					{
						try { file.Delete(); } catch { /* Bỏ qua nếu file đang bận */ }
					}
				}

				// 2. Dọn dẹp thư mục Download (Xóa các tệp chưa hoàn thiện)
				if (Directory.Exists(DownloadFolder))
				{
					DirectoryInfo di = new DirectoryInfo(DownloadFolder);
					foreach (FileInfo file in di.GetFiles())
					{
						try { file.Delete(); } catch { /* Bỏ qua nếu file đang bận */ }
					}
				}

				// 3. Gửi thông báo hoàn tất xuống Vue
				webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(new
				{
					type = "CLEANUP_COMPLETED",
					message = "Đã xóa bộ nhớ đệm hệ thống thành công!"
				}));
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Lỗi dọn dẹp: " + ex.Message);
			}
		}

		private void ApplyTheme(bool isDark)
		{
			// 1. Đổi màu nền của Form WinForms (để tránh bị lóe trắng khi load)
			this.BackColor = isDark ? Color.FromArgb(18, 18, 18) : Color.White;

			// 2. Cấu hình Windows API
			int useDarkMode = isDark ? 1 : 0;
			// Ép thanh Title sang Dark mode (để nút X/Min/Max đổi màu theo)
			DwmSetWindowAttribute(this.Handle, 20, ref useDarkMode, sizeof(int));

			// 3. Đổi màu nền thanh Title (Caption Color)
			// Dark: #121212 (BGR: 0x001E1E1E) | Light: #FFFFFF (BGR: 0x00FFFFFF)
			int titleColor = isDark ? 0x001E1E1E : 0x00FFFFFF;
			DwmSetWindowAttribute(this.Handle, 35, ref titleColor, sizeof(int));

			// 4. Đổi màu chữ trên thanh Title (Text Color)
			int textColor = isDark ? 0x00FFFFFF : 0x00000000;
			DwmSetWindowAttribute(this.Handle, 36, ref textColor, sizeof(int));
		}
		public bool IsWindowsDarkMode()
		{
			try
			{
				// Đường dẫn đến khóa Registry quản lý Theme của ứng dụng
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
				{
					if (key != null)
					{
						object registryValue = key.GetValue("AppsUseLightTheme");
						if (registryValue != null)
						{
							// 0 = Dark Mode, 1 = Light Mode
							return (int)registryValue == 0;
						}
					}
				}
			}
			catch { /* Xử lý lỗi nếu không có quyền truy cập Registry */ }
			return false; // Mặc định là Light nếu không đọc được
		}

		private void UninstallApp(string appId)
		{
			var app = globalAppList.FirstOrDefault(a => a.id == appId);
			if (app == null || !app.isInstalled) return;

			string FolderPath = Path.Combine(Application.StartupPath, "BSTCleaner_native");
			string fullPath = Path.Combine(FolderPath, "BSTCleaner.exe");
			if (!System.IO.File.Exists(fullPath))
			{
				MessageBox.Show($"Không tìm thấy công cụ: BSTCleaner.exe");
				return;
			}

			Task.Run(() =>
			{
				try
				{

					// 1. Báo Vue: Đang xử lý (Hiện progressbar, ẩn các nút Pause/Cancel)
					SendStatusToVue(appId, "DOWNLOAD_STATUS", new { status = "UNINSTALLING" });
					SetTaskbarState(TaskbarProgressBarStatus.Indeterminate); // Hiệu ứng chờ trên Taskbar


					using (Process p = new Process())
					{
						p.StartInfo.FileName = fullPath;
						p.StartInfo.WorkingDirectory = FolderPath;
						p.StartInfo.Arguments = $"-noui -oem {app.oem.Replace("BlueStacks_", "")}";
						p.StartInfo.UseShellExecute = true; // Cho phép mở giao diện GUI của tool
						p.EnableRaisingEvents = true;

						// TẠO MỘT BIẾN CHỜ (TaskCompletionSource)
						var tcs = new TaskCompletionSource<bool>();

						// Sự kiện này chỉ chạy khi CỬA SỔ TOOL ĐÓ ĐÓNG LẠI
						p.Exited += (s, ev) =>
						{
							tcs.SetResult(true);
						};

						p.Start();

						// Đợi cho đến khi Tool đóng hẳn mới chạy tiếp xuống dưới
						tcs.Task.Wait();

						app.isInstalled = false; // Đánh dấu là đã gỡ
						app.programPath = "";    // Xóa đường dẫn cũ
						app.dataPath = "";       // Xóa đường dẫn dữ liệu

						// 3. LƯU XUỐNG Ổ CỨNG
						// Để khi tắt Launcher mở lại, nó không báo là App vẫn còn cài đặt
						SaveAppListToDisk();

						// 2. Tool đã đóng -> Báo Vue trả lại nút "CHƯA CÀI ĐẶT"
						SendStatusToVue(appId, "APP_UNINSTALLED", new { });
						ShowToastWithIconAsync(app.id, "Gỡ cài đặt hoàn tất", $"{app.name} đã được gỡ cài đặt", app.icon);
						SetTaskbarState(TaskbarProgressBarStatus.NoProgress); // Tắt hiệu ứng trên Taskbar
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Lỗi thực thi: {ex.Message}");
					SendStatusToVue(appId, "DOWNLOAD_FAILED", new { error = ex.Message });
				}
			});

		}

		private bool CleanConflictingApp(GameApp app)
		{
			// Lấy chuỗi OEM đã cắt gọn (VD: nxt, nxt_cn, msi5)
			string oemToClean = app.oem.Replace("BlueStacks_", "");

			string FolderPath = Path.Combine(Application.StartupPath, "BSTCleaner_native");
			string fullPath = Path.Combine(FolderPath, "BSTCleaner.exe");

			if (!System.IO.File.Exists(fullPath))
			{
				this.Invoke((MethodInvoker)delegate
				{
					MessageBox.Show($"Không tìm thấy công cụ dọn dẹp: BSTCleaner.exe", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
				});
				return false;
			}

			try
			{
				// Báo Vue đang gỡ cài đặt để hiện Progress chữ đỏ
				SendStatusToVue(app.id, "DOWNLOAD_STATUS", new { status = "UNINSTALLING" });
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);

				using (Process p = new Process())
				{
					p.StartInfo.FileName = fullPath;
					p.StartInfo.WorkingDirectory = FolderPath;
					p.StartInfo.Arguments = $"-noui -oem {oemToClean}"; // Truyền đúng OEM để diệt
					p.StartInfo.UseShellExecute = true;
					p.EnableRaisingEvents = true;

					var tcs = new TaskCompletionSource<bool>();
					p.Exited += (s, ev) => tcs.SetResult(true);

					p.Start();
					tcs.Task.Wait(); // Chờ Tool dọn dẹp xong

					// Reset trạng thái xung đột để lát nữa cài đặt mượt mà
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
					MessageBox.Show($"Lỗi khi chạy công cụ dọn dẹp: {ex.Message}");
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
				MessageBox.Show($"Không tìm thấy công cụ: {exeName}");
				return;
			}

			Task.Run(() =>
			{
				try
				{
					if (status != null)
					{
						// 1. Báo Vue: Đang xử lý (Hiện progressbar, ẩn các nút Pause/Cancel)
						SendStatusToVue(appId, "DOWNLOAD_STATUS", new { status = status });
						SetTaskbarState(TaskbarProgressBarStatus.Indeterminate); // Hiệu ứng chờ trên Taskbar
					}

					using (Process p = new Process())
					{
						p.StartInfo.FileName = fullPath;
						p.StartInfo.WorkingDirectory = app.programPath;
						p.StartInfo.Arguments = args;
						p.StartInfo.UseShellExecute = true; // Cho phép mở giao diện GUI của tool
						p.EnableRaisingEvents = true;

						// TẠO MỘT BIẾN CHỜ (TaskCompletionSource)
						var tcs = new TaskCompletionSource<bool>();

						// Sự kiện này chỉ chạy khi CỬA SỔ TOOL ĐÓ ĐÓNG LẠI
						p.Exited += (s, ev) =>
						{
							tcs.SetResult(true);
						};

						p.Start();

						// Đợi cho đến khi Tool đóng hẳn mới chạy tiếp xuống dưới
						tcs.Task.Wait();

						// 2. Tool đã đóng -> Báo Vue trả lại nút "MỞ ỨNG DỤNG"
						SendStatusToVue(appId, "DOWNLOAD_COMPLETED", new { });
						SetTaskbarState(TaskbarProgressBarStatus.NoProgress); // Reset trạng thái Taskbar
					}
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Lỗi thực thi: {ex.Message}");
					SendStatusToVue(appId, "DOWNLOAD_FAILED", new { error = ex.Message });
				}
			});
		}

		// Đưa HttpListener ra làm biến của Class để dễ quản lý
		private HttpListener discordListener;
		private readonly HttpClient httpClient = new HttpClient();


		private void HandleDiscordLogin()
		{
			// 1. Điền Client ID từ trang Discord Developer Portal của bạn vào đây
			string clientId = "1475485221028626483"; // <--- THAY BẰNG SỐ CỦA BẠN

			// ĐỔI REDIRECT URI THÀNH TRANG WEB TRUNG GIAN CỦA BẠN (Phải khớp 100% với trên Discord)
			string redirectUri = authURL;

			string discordAuthUrl = $"https://discord.com/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify+guilds.members.read";

			try
			{
				// Chỉ việc mở trình duyệt lên, phần còn lại Web và WndProc sẽ lo!
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
				{
					FileName = discordAuthUrl,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				MessageBox.Show("Không thể mở trình duyệt: " + ex.Message);
			}
		}

		private async void ExchangeCodeForTokenAsync(string code)
		{
			string clientId = "1475485221028626483";
			string clientSecret = "5ATtI-m3quy2xg_GHoXoFqdGASACRwf0";
			string redirectUri = "https://shilukayt.github.io/DiscordAuth/";

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
					// 1. Lấy Access Token từ chuỗi JSON
					var tokenJson = JObject.Parse(responseString);
					string accessToken = tokenJson["access_token"]?.ToString();

					if (!string.IsNullOrEmpty(accessToken))
					{
						// 2. LƯU TOKEN LẠI ĐỂ AUTO LOGIN (Rất quan trọng)
						SaveSecureToken(accessToken);
						currentUserRoles = await FetchUserRolesFromDiscord(accessToken);
						GetData(); // Cập nhật lại danh sách app với Role mới lấy được

						// 3. Lấy thông tin User (Tên, Avatar, Email) ngay lập tức
						var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
						userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
						//MessageBox.Show("Đã lấy được Access Token: " + accessToken); // Debug token
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
								// Lấy Tên hiển thị, nếu rỗng thì lùi về lấy username
								name = !string.IsNullOrEmpty(globalName) ? globalName : username,
								username = username, // BẮT BUỘC PHẢI CÓ
								email = userJson["email"]?.ToString(),
								avatar = userJson["avatar"] != null ? $"https://cdn.discordapp.com/avatars/{userJson["id"]}/{userJson["avatar"]}.png" : null
							};
							// 4. Bắn dữ liệu lên Vue để hiển thị Avatar
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
					MessageBox.Show("Đổi token thất bại: " + responseString, "Lỗi API", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		// Bổ sung thêm biến ID vào hàm AutoLogin cũ của bạn để Vue không bị lỗi trắng Avatar
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
							username = username, // BẮT BUỘC PHẢI CÓ DÒNG NÀY
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
				// Chuyển token thành mảng byte
				byte[] plainBytes = Encoding.UTF8.GetBytes(token);

				// Mã hóa bằng DPAPI (Gắn chặt với User Windows hiện tại)
				byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

				// Lưu ra file nhị phân
				System.IO.File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"), encryptedBytes);

				// (Tùy chọn) Ẩn file đi để người dùng không ngứa tay xóa mất
				System.IO.File.SetAttributes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"), System.IO.FileAttributes.Hidden);
			}
			catch (Exception ex) { MessageBox.Show("Lỗi lưu token: " + ex.Message); }
		}

		private string LoadSecureToken()
		{
			if (!System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"))) return null;

			try
			{
				// Đọc file nhị phân đã mã hóa
				byte[] encryptedBytes = System.IO.File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"));

				// Giải mã (Windows sẽ tự động dùng key của tài khoản hiện tại)
				byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

				// Trả về token nguyên bản
				return Encoding.UTF8.GetString(plainBytes);
			}
			catch
			{
				// Giải mã thất bại (File bị lỗi hoặc bị mang sang máy khác)
				// Xóa luôn file rác này đi
				System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "accounts_encryption_key.dat"));
				return null;
			}
		}

		private async Task<List<string>> FetchUserRolesFromDiscord(string accessToken)
		{
			try
			{
				var request = new HttpRequestMessage(HttpMethod.Get, $"https://discord.com/api/users/@me/guilds/{officialGuildId}/member");
				//MessageBox.Show(accessToken); // Debug token
				request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

				var response = await httpClient.SendAsync(request);
				if (response.IsSuccessStatusCode)
				{
					string content = await response.Content.ReadAsStringAsync();
					var memberJson = JObject.Parse(content);

					// Trích xuất mảng roles (danh sách các ID)
					return memberJson["roles"]?.ToObject<List<string>>() ?? new List<string>();
				}
				else if (response.StatusCode == HttpStatusCode.NotFound)
				{
					Task.Run(() =>
					{
						MessageBox.Show("Bạn chưa tham gia máy chủ của Hiếu GL Lite\nTính năng của ứng dụng có thể sẽ bị hạn chế!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
					});
				}
			}
			catch (Exception ex) { Debug.WriteLine("Lỗi lấy Role: " + ex.Message); }

			return new List<string>();
		}

		// Main.cs
		private async Task ShowToastWithIconAsync(string appId, string title, string message, string iconUrl)
		{
			try
			{
				// 1. Đảm bảo thư mục Temp tồn tại
				if (!Directory.Exists(TempFolder)) Directory.CreateDirectory(TempFolder);

				// 2. Xác định đường dẫn file local (Dùng appId làm tên file cho chắc)
				string localIconPath = Path.Combine(TempFolder, $"{appId}_icon.png");

				// 3. Tải ảnh từ URL về thư mục Temp
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

				// 4. Hiển thị Toast bằng file đã tải về
				var toast = new ToastContentBuilder()
					.AddText(title)
					.AddText(message);

				if (System.IO.File.Exists(localIconPath))
				{
					// Bắt buộc dùng file:/// cho đường dẫn cục bộ
					toast.AddAppLogoOverride(new Uri("file:///" + localIconPath), ToastGenericAppLogoCrop.Default);
				}

				toast.Show();
			}
			catch (Exception ex)
			{
				// Fallback: Hiện thông báo không ảnh nếu có lỗi
				new ToastContentBuilder().AddText(title).AddText(message).Show();
				Debug.WriteLine("Lỗi Toast: " + ex.Message);
			}
		}
		// Main.cs
		private void SetTaskbarState(TaskbarProgressBarStatus state)
		{
			this.Invoke((MethodInvoker)delegate
			{
				if (_taskbarList == null)
				{
					_taskbarList = (ITaskbarList3)new TaskbarInstance();
					_taskbarList.HrInit();
				}
				// Thiết lập trạng thái (Normal, Indeterminate, Error, v.v.)
				_taskbarList.SetProgressState(this.Handle, state);
			});
		}

		// Main.cs
		private void SetTaskbarProgressValue(int value, int total = 100)
		{
			this.Invoke((MethodInvoker)delegate
			{
				if (_taskbarList == null)
				{
					_taskbarList = (ITaskbarList3)new TaskbarInstance();
					_taskbarList.HrInit();
				}

				// 1. Chuyển sang màu Xanh lá (Normal) để hiện %
				_taskbarList.SetProgressState(this.Handle, TaskbarProgressBarStatus.Normal);

				// 2. Cập nhật giá trị hiện tại
				_taskbarList.SetProgressValue(this.Handle, (ulong)value, (ulong)total);
			});
		}
		// Biến cờ đánh dấu app đang trong quá trình bị tiêu diệt (Chống lỗi WebView2)
		private bool isExiting = false;
		// 1. Hàm kiểm tra điều kiện an toàn trước khi thoát
		private bool CanExitApp()
		{
			if (isDownloading)
			{
				MessageBox.Show("Ứng dụng hiện đang cài đặt, bạn không thể thoát ngay lúc này.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}

			if (isRunning)
			{
				MessageBox.Show("Một ứng dụng đang chạy, bạn không thể thoát hoàn toàn lúc này.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false;
			}

			return true;
		}

		// 2. Hàm dọn dẹp các tiến trình nền
		private void CleanupBeforeExit()
		{
			if (trayIcon != null)
			{
				trayIcon.Visible = false;
				trayIcon.Dispose();
			}
			discordClient?.Dispose();
		}

		// Hàm đánh thức ứng dụng
		private void ShowApp()
		{
			this.Show();
			this.WindowState = FormWindowState.Normal;
			this.Activate();

			// THÊM !webView21.IsDisposed VÀO ĐÂY
			if (webView21 != null && !webView21.IsDisposed && webView21.CoreWebView2 != null)
			{
				try
				{
					webView21.CoreWebView2.Resume();
					System.Diagnostics.Debug.WriteLine("[Hệ thống] Đã đánh thức WebView2.");
				}
				catch { }
			}
		}


		// Hàm vét cạn RAM của TOÀN BỘ HỆ THỐNG (C# và WebView2)
		private void TrimTotalMemory()
		{
			try
			{
				// 1. ÉP RAM CỦA CHÍNH LAUNCHER C# XUỐNG ĐÁY (Vũ khí bí mật ở đây)
				EmptyWorkingSet(System.Diagnostics.Process.GetCurrentProcess().Handle);

				// 2. ÉP RAM CỦA TẤT CẢ TIẾN TRÌNH WEBVIEW2 XUỐNG ĐÁY
				Process[] wv2Processes = Process.GetProcessesByName("msedgewebview2");
				foreach (Process p in wv2Processes)
				{
					try
					{
						EmptyWorkingSet(p.Handle);
					}
					catch
					{
						// Bỏ qua các tiến trình không có quyền truy cập
					}
				}
			}
			catch { }
		}

		// ==========================================
		// CÁC SỰ KIỆN CHÍNH (ĐÃ ĐƯỢC LÀM SẠCH)
		// ==========================================

		// 1. HÀM XỬ LÝ KHI BẤM NÚT X TRÊN CÙNG
		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				// NẾU CÀI ĐẶT TRONG JSON LÀ "THU NHỎ XUỐNG KHAY" (Mặc định)
				if (minimizeToTrayOnClose)
				{
					e.Cancel = true;
					SuspendApp(); // Kích hoạt quy trình Ẩn & Giảm RAM
				}
				// NẾU CÀI ĐẶT TRONG JSON LÀ "THOÁT HẲN"
				else
				{
					// Kiểm tra xem có đang tải game hay đang chơi game không
					if (!CanExitApp())
					{
						e.Cancel = true; // Kẹt tiến trình -> Hủy lệnh tắt
						return;
					}

					// Vượt qua ải an toàn -> Bật cờ khóa mọi giao tiếp với WebView2
					isExiting = true;

					CleanupBeforeExit(); // Dọn dẹp TrayIcon và Discord
										 // Sau dòng này Windows sẽ tự kết liễu Form
				}
			}
		}

		// 2. HÀM NGỦ ĐÔNG VÀ VẮT KIỆT RAM (Bản chống đạn tuyệt đối)
		private async void SuspendApp()
		{
			try
			{
				// KHIÊN 1: Chặn ngay từ cửa nếu app đang có dấu hiệu bị tắt
				if (isExiting || this.IsDisposed) return;

				this.Hide(); // Ẩn cửa sổ

				// KHIÊN 2: Kiểm tra sinh tồn của WebView2 trước khi đụng vào
				if (webView21 != null && !webView21.IsDisposed && webView21.CoreWebView2 != null)
				{
					// Lệnh bất đồng bộ rất dễ bị "đột tử" giữa chừng
					await webView21.CoreWebView2.TrySuspendAsync();

					GC.Collect();
					GC.WaitForPendingFinalizers();
					TrimTotalMemory();
				}
			}
			// --- LƯỚI HỨNG ĐẠN TẦNG 3 ---
			catch (ObjectDisposedException)
			{
				// WebView2 bị xóa mất xác -> Bỏ qua vì đằng nào RAM cũng đã được giải phóng
			}
			catch (InvalidCastException)
			{
				// Lỗi COM Component đặc trưng của nhân Chromium khi nó bị rút điện đột ngột -> Bỏ qua
			}
			catch (Exception ex)
			{
				// Ghi log các lỗi khác nếu có
				System.Diagnostics.Debug.WriteLine("Lỗi nhẹ khi Suspend WebView2: " + ex.Message);
			}
		}

		// 3. HÀM TẮT TỪ MENU CHUỘT PHẢI DƯỚI KHAY HỆ THỐNG
		private void TrayExit_Click(object sender, EventArgs e)
		{
			if (!CanExitApp()) return; // Chặn nếu đang tải/chơi game

			// Bật cờ khóa WebView2
			isExiting = true;

			CleanupBeforeExit(); // Dọn dẹp
			Application.Exit();  // Tắt chết app
		}
		// Dùng biểu thức => cho ngắn gọn với các sự kiện gọi 1 hàm
		private void TrayIcon_DoubleClick(object sender, EventArgs e) => ShowApp();
		private void TrayOpen_Click(object sender, EventArgs e) => ShowApp();



		// Khai báo biến toàn cục để lưu danh sách App dùng chung cho toàn bộ Launcher
		// Hàm này CHỈ làm nhiệm vụ đi lấy data và xử lý nghiệp vụ (Registry/Conf...)
		// Tách riêng hàm xử lý data ra một góc cho sạch sẽ
		private async Task<List<GameApp>> GetSyncedAppListAsync(string jsonUrl)
		{
			try
			{
				using (HttpClient client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("User-Agent", "HieuGLLite-Launcher");
					string jsonString = await client.GetStringAsync(jsonUrl);

					// 1. Đúc JSON vào Model (Lúc này tất cả isInstalled đều = false)
					List<GameApp> appList = JsonConvert.DeserializeObject<List<GameApp>>(jsonString);

					// 2. CHẠY MÁY QUÉT KIỂM TRA CÀI ĐẶT
					if (appList != null)
					{
						foreach (var app in appList)
						{
							CheckAppInstallation(app);
							// Sau dòng này, app nào cài rồi thì isInstalled sẽ tự biến thành true!
						}
					}

					// 3. Trả về danh sách đã được "bơm" đầy đủ trạng thái thực tế
					return appList;
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Lỗi xử lý Data: {ex.Message}");
				return new List<GameApp>();
			}
		}
		private void CheckAppInstallation(GameApp app)
		{
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

						// --- XỬ LÝ VERSION NGAY TỪ ĐẦU ---
						string rawVersion = key.GetValue("Version")?.ToString();
						string cleanVersion = null;
						if (!string.IsNullOrEmpty(rawVersion))
						{
							// Cắt chuỗi và chỉ lấy 3 số đầu
							string[] parts = rawVersion.Split('.');
							cleanVersion = parts.Length >= 3 ? $"{parts[0]}.{parts[1]}.{parts[2]}" : rawVersion;
						}
						// ---------------------------------

						string fullExePath = Path.Combine(installDir ?? "", app.exeName ?? "");

						if (!string.IsNullOrEmpty(installDir) && System.IO.File.Exists(fullExePath))
						{
							if (registryAppID == app.id)
							{
								app.isInstalled = true;
								app.programPath = installDir;
								app.dataPath = dataDir;
								app.installedVersion = cleanVersion; // Gán chuỗi đã gọt sạch tinh tươm
							}
							else
							{
								// PHÁT HIỆN XUNG ĐỘT
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
			// Sử dụng ID ứng dụng bạn đã có
			discordClient = new DiscordRpcClient("1475485221028626483");

			// Lắng nghe các sự kiện (tùy chọn)
			discordClient.OnReady += (sender, e) =>
			{
				Debug.WriteLine($"Discord RPC sẵn sàng cho user: {e.User.Username}");
			};

			// Kết nối
			discordClient.Initialize();

			// Đặt trạng thái ban đầu là đang xem danh sách game
			UpdatePresence("", "Phiên bản: " + FE_version, "base", "");
		}

		// Hàm dùng chung để cập nhật trạng thái linh hoạt
		public void UpdatePresence(string state, string details, string largeKey, string smallKey)
		{
			if (discordClient == null || discordClient.IsDisposed) return;

			discordClient.SetPresence(new RichPresence()
			{
				State = state,
				Details = details,
				Timestamps = Timestamps.Now,
				Assets = new Assets()
				{
					// Sử dụng các key được truyền vào từ đối tượng App
					LargeImageKey = largeKey,
					LargeImageText = "",
					SmallImageKey = smallKey,
					SmallImageText = ""
				}
			});
		}
		private DateTime lastRoleFetchTime = DateTime.MinValue;
		// Main.cs
		public void GetData(bool forceRefresh = false)
		{
			Task.Run(async () =>
			{
				// 1. Kiểm tra: Bắt buộc làm mới HOẶC chưa có role HOẶC đã trễ hơn 2 phút
				if (forceRefresh || currentUserRoles == null || (DateTime.Now - lastRoleFetchTime).TotalMinutes > 2)
				{
					bool loginSuccess = await AutoLoginDiscord();
					if (loginSuccess)
					{
						lastRoleFetchTime = DateTime.Now; // Đánh dấu thời gian vừa lấy thành công
					}
				}

				string jsonUrl = jsonURL + "appsList.json";

				// 2. Lấy toàn bộ danh sách từ Server
				var allApps = await GetSyncedAppListAsync(jsonUrl);
				if (allApps == null) return;

				globalAppList = allApps;

				foreach (var app in globalAppList)
				{
					CheckAppInstallation(app);
				}

				// 3. LỌC DỮ LIỆU
				List<string> userRoles = currentUserRoles ?? new List<string>();
				var filteredApps = globalAppList.Where(app =>
					string.IsNullOrEmpty(app.requiredRoleID) ||
					userRoles.Contains(app.requiredRoleID)
				).ToList();

				// 4. Gửi dữ liệu
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
		// Chuyển đổi Bytes sang GB để hiển thị và so sánh
		private double BytesToGB(long bytes)
		{
			return (double)bytes / (1024 * 1024 * 1024);
		}

		private double GetFreeSpaceGB(string path)
		{
			try
			{
				// Nếu path rỗng, mặc định kiểm tra ổ đĩa chứa Launcher
				string targetPath = string.IsNullOrEmpty(path) ? Application.StartupPath : path;
				string driveName = Path.GetPathRoot(Path.GetFullPath(targetPath));

				DriveInfo drive = new DriveInfo(driveName);
				if (drive.IsReady)
				{
					return (double)drive.AvailableFreeSpace / (1024 * 1024 * 1024);
				}
			}
			catch { /* Xử lý lỗi path */ }
			return 0;
		}

		// Main.cs
		private void HandleInstallRequest(dynamic message)
		{
			if (isDownloading)
			{
				this.Invoke((MethodInvoker)delegate
				{
					MessageBox.Show("Hiện đang có một ứng dụng khác đang được xử lý!", "Thông báo");
				});
				return;
			}

			// 1. Lấy thông tin từ message và danh sách app
			string appId = message["appId"]?.ToString();
			string selectedPath = message["installPath"]?.ToString(); // Đường dẫn người dùng vừa chọn

			var app = globalAppList.FirstOrDefault(a => a.id == appId);
			if (app == null) return;

			// 2. Xác định đường dẫn cuối cùng để kiểm tra ổ đĩa
			// Ưu tiên: Đường dẫn vừa chọn > Đường dẫn đã cài cũ > Ổ C mặc định
			string checkPath = !string.IsNullOrEmpty(selectedPath) ? selectedPath :
							  (!string.IsNullOrEmpty(app.programPath) ? app.programPath : @"C:\");

			// 3. Tính toán dung lượng (Server trả về Byte, chia để lấy GB)
			double requiredGB = (double)app.requiredSize / (1024 * 1024 * 1024);
			double freeGB = GetFreeSpaceGB(checkPath);

			// 4. KIỂM TRA VÀ CHẶN (Gating)
			if (freeGB < requiredGB)
			{
				SetTaskbarState(TaskbarProgressBarStatus.Error);
				this.Invoke((MethodInvoker)delegate
				{
					MessageBox.Show(
						$"Ổ đĩa {Path.GetPathRoot(checkPath)} không đủ dung lượng!\n" +
						$"- Cần: {requiredGB:F2} GB\n" +
						$"- Hiện có: {freeGB:F2} GB",
						"Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				});

				// PHẢI CÓ RETURN Ở ĐÂY để dừng hẳn, không chạy xuống Task.Run
				SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
				SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
				return;
			}
			Task.Run(async () =>
			{
				if (app.isInstalled)
				{
					DialogResult result = DialogResult.None;

					this.Invoke((MethodInvoker)delegate {
						result = MessageBox.Show(
							$"Bạn đang thực hiện thay đổi phiên bản cho {app.name}.\n\n" +
							"Hành động này sẽ XÓA SẠCH toàn bộ dữ liệu, ứng dụng và cài đặt của phiên bản hiện tại trước khi cài đặt phiên bản mới.\n\n" +
							"Bạn có chắc chắn muốn tiếp tục?",
							"Cảnh báo thay đổi phiên bản",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Warning
						);
					});

					if (result == DialogResult.Yes)
					{
						// Gọi luôn hàm dọn dẹp bằng OEM thần thánh lúc nãy để xóa trắng bản cũ
						bool cleanSuccess = await Task.Run(() => CleanConflictingApp(app));

						if (!cleanSuccess)
						{
							// Nếu tool dọn dẹp thất bại -> Báo Vue hủy bỏ cài đặt
							SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
							return;
						}
						// Nếu dọn dẹp thành công, nó sẽ tự động chạy tiếp xuống dưới để tải bản mới!
					}
					else
					{
						// Nếu bấm "Không" -> Hủy ngay
						SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
						return;
					}
				}
				// ================= CHỐT CHẶN 2: PHÁT HIỆN XUNG ĐỘT (CỦA BẠN ĐÃ VIẾT) =================
				else if (app.isConflict)
				{
					DialogResult result = DialogResult.None;
					string conflictName = !string.IsNullOrEmpty(app.conflictAppName) ? app.conflictAppName : "một phiên bản giả lập khác (cùng lõi hệ thống)";

					this.Invoke((MethodInvoker)delegate {
						result = MessageBox.Show(
							$"Máy bạn đang cài đặt {conflictName} bị xung đột với phiên bản này.\n\n" +
							$"Bạn có muốn gỡ cài đặt nó để tiếp tục cài {app.name} không?",
							"Phát hiện xung đột",
							MessageBoxButtons.YesNo,
							MessageBoxIcon.Warning
						);
					});

					if (result == DialogResult.Yes)
					{
						bool cleanSuccess = await Task.Run(() => CleanConflictingApp(app));
						if (!cleanSuccess)
						{
							SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
							return;
						}
					}
					else
					{
						SendStatusToVue(appId, "DOWNLOAD_CANCELLED", new { });
						return;
					}
				}



				// Sau khi dọn dẹp hoặc nếu không có xung đột, tiến hành tải và cài đặt
				try
				{
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
			Directory.CreateDirectory(DownloadFolder);
			Directory.CreateDirectory(StateFolder);

			// 1. Tạo mới một công tắc cho appId này
			var cts = new CancellationTokenSource();
			cancellationTokens[appid] = cts;
			try
			{
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);
				string exePath = Path.Combine(DownloadFolder, data["exeName"].ToString());
				string androidPath = Path.Combine(DownloadFolder, data["androidName"].ToString());

				// BƯỚC 1: Tải EXE - Truyền token vào để có thể ngắt giữa chừng
				if (!await DownloadStepAsync(appid, data["exeUrl"].ToString(), exePath, "DOWNLOADING_EXE", cts.Token))
				{
					isDownloading = false; // Hạ cờ tải xuống nếu bị hủy ở bước này
					SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
					return;
				}

				// BƯỚC 2: Tải Android - Nếu bước 1 bị hủy, dòng này sẽ không bao giờ chạy
				if (!await DownloadStepAsync(appid, data["androidUrl"].ToString(), androidPath, "DOWNLOADING_ANDROID", cts.Token))
				{
					isDownloading = false; // Hạ cờ tải xuống nếu bị hủy ở bước này
					SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
					return;
				}

				// Bước 3: Xác thực SHA256 (Tệp đã sẵn sàng vì downloader đã disposed ở bước trên)
				SendStatusToVue(appid, "DOWNLOAD_STATUS", new { status = "VERIFYING" });
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);
				await Task.Delay(300); // Tạo độ trễ nhỏ để người dùng thấy trạng thái trên UI

				bool isExeOk = VerifySHA256(exePath, data["exeHash"]?.ToString());
				bool isAndroidOk = VerifySHA256(androidPath, data["androidHash"]?.ToString());

				if (isExeOk && isAndroidOk)
				{
					// Bước 4: Chạy cài đặt
					await RunSilentInstallerAsync(appid, exePath, data["androidCode"].ToString(), data["installPath"].ToString());
				}
				else
				{
					SendStatusToVue(appid, "DOWNLOAD_FAILED", new { error = "Lỗi xác thực tệp (SHA256 Mismatch)!" });
					SetTaskbarState(TaskbarProgressBarStatus.Error);
					// Main.cs
					string detailMessage = "Kiểm tra tính toàn vẹn thất bại, vui lòng thử lại sau!\n\n" +
										   "Chi tiết trạng thái tệp:\n" +
										   $"- Tệp cài đặt: {(isExeOk ? "Hợp lệ" : "Bị lỗi/Hỏng")}\n" +
										   $"- Tệp hệ điều hành: {(isAndroidOk ? "Hợp lệ" : "Bị lỗi/Hỏng")}";

					MessageBox.Show(
						detailMessage,
						"Lỗi xác thực SHA256",
						MessageBoxButtons.OK,
						MessageBoxIcon.Error
					);
					//Cleanup files
					if (System.IO.File.Exists(exePath)) System.IO.File.Delete(exePath);
					if (System.IO.File.Exists(androidPath)) System.IO.File.Delete(androidPath);
					SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
				}
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine($"Download for {appid} was canceled.");
			}
			finally
			{
				// Cưỡng ép mở khóa hệ thống
				isDownloading = false;

				// Dọn dẹp bộ nhớ
				cancellationTokens.Remove(appid);
				activeDownloads.Remove(appid);

				// Gửi lệnh xóa thanh Progress trên Vue
				SendStatusToVue(appid, "DOWNLOAD_CANCELLED", new { });

				Debug.WriteLine($"[Hệ thống] Đã giải phóng hoàn toàn tiến trình cho {appid}");
			}
		}
		private async Task<bool> DownloadStepAsync(string appid, string url, string savePath, string statusType, CancellationToken token)
		{
			using (MultiThreadedDownloader downloader = new MultiThreadedDownloader())
			{
				activeDownloads[appid] = downloader;
				downloader.CurrentStatusType = statusType; // Lưu lại để dùng khi Pause

				string statePath = Path.Combine(StateFolder, $"{appid}_{statusType}.json");

				downloader.OnProgressChanged += (percent, speed, downloaded) =>
				{
					SendStatusToVue(appid, "DOWNLOAD_PROGRESS", new { percent, speed, downloaded, status = statusType });

					this.Invoke((MethodInvoker)delegate
					{
						SetTaskbarProgressValue((int)percent);
					});
				};

				// --- TÍCH HỢP RESUME TỰ ĐỘNG ---
				if (System.IO.File.Exists(statePath))
				{
					try
					{
						var json = System.IO.File.ReadAllText(statePath);
						var package = JsonConvert.DeserializeObject<DownloadPackage>(json);
						package.Urls = new string[] { url }; // Bơm URL tươi mới
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

				if (token.IsCancellationRequested) return false;

				// Xóa state khi xong 100%
				if (System.IO.File.Exists(statePath)) System.IO.File.Delete(statePath);
				await Task.Delay(200);
			}
			return true;
		}

		private async Task RunSilentInstallerAsync(string appid, string exePath, string androidCode, string installPath)
		{
			var app = globalAppList.FirstOrDefault(a => a.id == appid);
			// 1. Báo Vue chuyển sang màu cam (INSTALLING)
			SendStatusToVue(appid, "DOWNLOAD_STATUS", new { status = "INSTALLING" });

			try
			{
				SetTaskbarState(TaskbarProgressBarStatus.Indeterminate);
				using (Process p = new Process())
				{
					p.StartInfo.FileName = exePath;
					// Truyền tham số cấu hình: Silent, Phiên bản Android, và Thư mục lưu Data
					p.StartInfo.Arguments = $"-s -DefaultImageName={androidCode} -ImageToLaunch={androidCode} -PDDir=\"{installPath}\"";
					p.StartInfo.UseShellExecute = true;
					p.EnableRaisingEvents = true;

					var tcs = new TaskCompletionSource<bool>();
					p.Exited += (s, e) =>
					{
						// Chỉ cần biết ExitCode <= 1 là coi như thành công
						tcs.SetResult(p.ExitCode <= 1);
					};

					p.Start();
					bool processSuccess = await tcs.Task; // Chờ tiến trình cài đặt đóng lại

					// --- BẮT ĐẦU CHỐT CHẶN CHỐNG ROLLBACK VÀ DELAY I/O ---

					// 1. Nghỉ 2 giây để Windows xả Cache File và chốt khóa Registry
					await Task.Delay(2000);
					CheckAppInstallation(app);

					// 2. Vòng lặp kiên nhẫn: Nếu chưa thấy file, thử tìm lại thêm tối đa 3 lần (Mỗi lần cách nhau 2 giây)
					// (Phòng trường hợp ổ cứng HDD/SSD của máy khách hàng bị chậm)
					int retryCount = 0;
					while (!app.isInstalled && retryCount < 3)
					{
						await Task.Delay(2000);
						CheckAppInstallation(app);
						retryCount++;
					}

					// CHỈ KHI: Máy quét xác nhận 100% file đã tồn tại trên máy (Bỏ qua luôn ExitCode vì nhiều bộ cài trả về sai)
					if (app.isInstalled)
					{
						// --- DỌN DẸP RÁC SAU KHI THÀNH CÔNG ---
						if (System.IO.File.Exists(exePath)) System.IO.File.Delete(exePath);

						string stateFile = Path.Combine(StateFolder, $"{appid}_state.json");
						if (System.IO.File.Exists(stateFile)) System.IO.File.Delete(stateFile);

						// Cập nhật lại đường dẫn Data và lưu JSON
						app.dataPath = installPath;
						SaveAppListToDisk();

						isDownloading = false;

						// Báo Vue cài thành công
						SendStatusToVue(appid, "DOWNLOAD_COMPLETED", new { savedPath = app.programPath });
						ShowToastWithIconAsync(appid, "Cài đặt hoàn tất", $"{app.name} đã được cài đặt", app.icon);
					}
					else
					{
						// NẾU LỌT VÀO ĐÂY: Quét 4 lần vẫn không thấy -> 100% là Rollback hoặc lỗi!
						SendStatusToVue(appid, "DOWNLOAD_FAILED", new { error = "Cài đặt thất bại, bị xung đột hoặc lỗi hệ thống (Bộ cài đã hoàn tác)." });
						MessageBox.Show("Cài đặt thất bại, trình cài đặt hiện đã hoàn tác thay đổi", "Cài đặt thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
						SetTaskbarState(TaskbarProgressBarStatus.Error);
					}
				}
			}
			catch (Exception ex)
			{
				SendStatusToVue(appid, "DOWNLOAD_FAILED", new { error = ex.Message });
			}
			finally
			{
				// Xóa máy tải khỏi danh sách quản lý
				activeDownloads.Remove(appid);
				SetTaskbarState(TaskbarProgressBarStatus.NoProgress);
			}
		}

		// Hàm gửi message qua WebView2
		private void SendStatusToVue(string appid, string type, object extraData)
		{
			this.Invoke((MethodInvoker)delegate
			{
				// Tạo đối tượng JSON chuẩn
				JObject response = JObject.FromObject(new { type = type, appId = appid });
				JObject extra = JObject.FromObject(extraData);

				// Gộp dữ liệu extra vào response gốc
				response.Merge(extra, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });

				webView21.CoreWebView2.PostWebMessageAsJson(response.ToString());
			});
		}

		// Hàm cập nhật trạng thái app trong RAM
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

				// Chỉ lọc lấy những app đã cài để lưu, bỏ qua mọi thông tin nhạy cảm từ server
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
				System.IO.File.WriteAllText(ConfigFile, json);
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Lưu thất bại: " + ex.Message);
			}
		}

		private void LoadAppListFromDisk()
		{
			// Kiểm tra file ConfigFile đã quy hoạch ở phần Static Properties
			if (!System.IO.File.Exists(ConfigFile)) return;

			try
			{
				string json = System.IO.File.ReadAllText(ConfigFile);
				// Nạp danh sách trạng thái rút gọn
				var localData = JsonConvert.DeserializeObject<List<LocalAppStatus>>(json);

				if (localData == null) return;

				foreach (var local in localData)
				{
					var serverApp = globalAppList.FirstOrDefault(a => a.id == local.id);
					if (serverApp != null)
					{
						// Kiểm tra xem đường dẫn cũ trong JSON có còn file EXE không
						string realExePath = Path.Combine(local.programPath ?? "", serverApp.exeName ?? "");

						if (System.IO.File.Exists(realExePath))
						{
							serverApp.isInstalled = local.isInstalled;
							serverApp.programPath = local.programPath;
						}
						else
						{
							// File mất rồi thì ép về false luôn cho sạch
							serverApp.isInstalled = false;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Lỗi hợp nhất dữ liệu: " + ex.Message);
			}
		}

		private void LoadAndMergeStatus()
		{
			if (!System.IO.File.Exists(ConfigFile)) return;

			try
			{
				string json = System.IO.File.ReadAllText(ConfigFile);
				var localData = JsonConvert.DeserializeObject<List<LocalAppStatus>>(json);

				foreach (var local in localData)
				{
					// Tìm app tương ứng trong danh sách server vừa tải về
					var serverApp = globalAppList.FirstOrDefault(a => a.id == local.id);
					if (serverApp != null)
					{
						// Chỉ ghi đè trạng thái vật lý, giữ nguyên dữ liệu meta từ server
						serverApp.isInstalled = local.isInstalled;
						serverApp.programPath = local.programPath;
						serverApp.dataPath = local.dataPath;
						serverApp.installedVersion = local.installedVersion;
					}
				}
			}
			catch { /* Xử lý file lỗi */ }
		}

		//public void SaveStateWithoutUrl(string appid)
		//{
		//	// Lấy package hiện tại từ downloader
		//	var package = _downloader.Package;

		//	if (package != null)
		//	{
		//		// XÓA DẤU VẾT: Gán URL về rỗng trước khi chuyển thành JSON
		//		package.Urls = new string[0];

		//		string json = JsonConvert.SerializeObject(package, Formatting.Indented);
		//		string stateFilePath = Path.Combine(StateFolder, $"{appid}_state.json");

		//		System.IO.File.WriteAllText(stateFilePath, json);
		//	}
		//}
		public async Task ResumeDownloadAsync(string appid, string currentUrlFromManifest)
		{
			string statePath = Path.Combine(StateFolder, $"{appid}_state.json");

			if (System.IO.File.Exists(statePath))
			{
				var package = JsonConvert.DeserializeObject<DownloadPackage>(System.IO.File.ReadAllText(statePath));

				// NẠP LẠI URL: Lấy URL mới nhất từ server thay vì dùng cái cũ đã xóa
				package.Urls = new string[] { currentUrlFromManifest };

				// Tiếp tục tải từ package đã được nạp lại URL
				await _downloader.ResumeFromPackageAsync(package);
			}
		}
		private List<InstanceInfo> ParseBlueStacksInstances(string confPath)
		{
			var instances = new Dictionary<string, InstanceInfo>();

			if (!System.IO.File.Exists(confPath)) return new List<InstanceInfo>();

			try
			{
				string[] lines = System.IO.File.ReadAllLines(confPath);
				foreach (string line in lines)
				{
					// --- 1. ĐỌC CÁC BẢN MOD ĐÃ TÍCH HỢP (Từ bst.installed_images) ---
					if (line.StartsWith("bst.installed_images="))
					{
						string currentImages = line.Split(new char[] { '=' }, 2)[1].Trim('\"');
						string[] codes = currentImages.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (string code in codes)
						{
							if (!string.IsNullOrEmpty(code) && !instances.ContainsKey(code))
							{
								instances[code] = new InstanceInfo { name = code, displayName = code };
							}
						}
					}

					// --- 2. ĐỌC CÁC BẢN SAO (INSTANCE) ĐÃ TẠO ---
					if (line.StartsWith("bst.instance."))
					{
						string[] parts = line.Split(new char[] { '=' }, 2);
						if (parts.Length == 2)
						{
							string key = parts[0].Trim();
							string value = parts[1].Trim('\"', ' ');

							string[] keyParts = key.Split('.');
							if (keyParts.Length >= 4)
							{
								string instanceName = keyParts[2];
								string property = keyParts[3];

								if (!instances.ContainsKey(instanceName))
								{
									instances[instanceName] = new InstanceInfo
									{
										name = instanceName,
										displayName = instanceName
									};
								}

								if (property == "display_name")
								{
									instances[instanceName].displayName = value;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Lỗi đọc file conf: " + ex.Message);
			}

			return instances.Values.ToList();
		}

		private bool IsVersionGreaterOrEqual(string currentVersion, string targetVersion)
		{
			try
			{
				if (Version.TryParse(currentVersion, out Version v1) && Version.TryParse(targetVersion, out Version v2))
				{
					return v1 >= v2;
				}
			}
			catch { }
			return false;
		}

		private bool minimizeToTrayOnClose = true;
		private string currentTheme = "system";

		private void LoadSettings()
		{
			if (System.IO.File.Exists(SettingsFile))
			{
				try
				{
					string json = System.IO.File.ReadAllText(SettingsFile);

					// DÙNG MODEL ĐỂ GIẢI MÃ: Nhanh, chuẩn xác và không sợ sai kiểu dữ liệu
					var settings = JsonConvert.DeserializeObject<AppSettingsModel>(json);

					if (settings != null)
					{
						minimizeToTrayOnClose = settings.minimizeToTray;
						currentTheme = settings.theme;
					}
				}
				catch (Exception ex)
				{
					// In ra lỗi thay vì nuốt chửng để dễ debug
					System.Diagnostics.Debug.WriteLine("Lỗi đọc file Cài đặt JSON: " + ex.Message);
				}
			}
		}

		private void SaveSettings()
		{
			try
			{
				Directory.CreateDirectory(RootFolder);

				// ĐÓNG GÓI VÀO MODEL TRƯỚC KHI LƯU
				var settings = new AppSettingsModel
				{
					minimizeToTray = minimizeToTrayOnClose,
					theme = currentTheme
				};

				// Dùng Formatting.Indented để file JSON khi mở bằng Notepad nhìn tự động thụt đầu dòng cực đẹp
				System.IO.File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(settings, Formatting.Indented));
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Lỗi lưu file Cài đặt JSON: " + ex.Message);
			}
		}

		private void PatchBstkFile(string engineFolder, string androidCode)
		{
			string bstkPath = Path.Combine(engineFolder, $"{androidCode}.bstk");
			string bstkInPath = Path.Combine(engineFolder, "Android.bstk.in");

			// Phải có đủ 2 file mới phẫu thuật được
			if (!System.IO.File.Exists(bstkPath) || !System.IO.File.Exists(bstkInPath)) return;

			try
			{
				XDocument docIn = XDocument.Load(bstkInPath);
				XDocument docTarget = XDocument.Load(bstkPath);

				var hardDisksIn = docIn.Descendants().Where(x => x.Name.LocalName == "HardDisk").ToList();
				var hardDisksTarget = docTarget.Descendants().Where(x => x.Name.LocalName == "HardDisk").ToList();

				// 1. CẬP NHẬT FILE ROOT
				var rootIn = hardDisksIn.FirstOrDefault(x => x.Attribute("location")?.Value.Contains("Root", StringComparison.OrdinalIgnoreCase) == true);
				var rootTarget = hardDisksTarget.FirstOrDefault(x => x.Attribute("location")?.Value.Contains("Root", StringComparison.OrdinalIgnoreCase) == true);
				if (rootIn != null && rootTarget != null)
				{
					rootTarget.SetAttributeValue("uuid", rootIn.Attribute("uuid")?.Value);
					rootTarget.SetAttributeValue("location", rootIn.Attribute("location")?.Value);
				}

				// 2. CẬP NHẬT FILE FASTBOOT
				var fastbootIn = hardDisksIn.FirstOrDefault(x => x.Attribute("location")?.Value.Contains("fastboot", StringComparison.OrdinalIgnoreCase) == true);
				var fastbootTarget = hardDisksTarget.FirstOrDefault(x => x.Attribute("location")?.Value.Contains("fastboot", StringComparison.OrdinalIgnoreCase) == true);
				if (fastbootIn != null && fastbootTarget != null)
				{
					fastbootTarget.SetAttributeValue("uuid", fastbootIn.Attribute("uuid")?.Value);
					fastbootTarget.SetAttributeValue("location", fastbootIn.Attribute("location")?.Value);
				}

				// 3. LƯU LẠI
				docTarget.Save(bstkPath);
				Debug.WriteLine("Đã Patch thành công file BSTK cho phiên bản mới!");
			}
			catch (Exception ex)
			{
				Debug.WriteLine("Lỗi sửa file BSTK: " + ex.Message);
			}
		}



		//private void SaveDownloadState(string appid)
		//{
		//	if (activeDownloads.TryGetValue(appid, out var downloader))
		//	{
		//		var package = downloader.GetCurrentPackage();
		//		if (package != null)
		//		{
		//			// Xóa URL trước khi lưu để bảo mật
		//			package.Urls = new string[0];

		//			string json = JsonConvert.SerializeObject(package);
		//			System.IO.File.WriteAllText(Path.Combine(StateFolder, $"{appid}_state.json"), json);
		//		}
		//	}
		//}
	}
}
