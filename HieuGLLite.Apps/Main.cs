using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using DiscordRPC;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;
using static HieuGLLite.Apps.AppModel;



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

		//Các biến toàn cục khác
		public bool isDevMode = true;

		public string AppName = "Hieu GL Lite (beta)";
		public string version = "26.3.0";
		public int versioncode = 260300;


		public bool isDownloading; // Biến trạng thái tải về (Dùng để Vue có thể bật loading khi cần)

		public bool isRunning; // Biến trạng thái app đang chạy hay không (Dùng để Vue đổi nút Play thành màu xanh khi đang mở app)

		public readonly string hostURL = "https://shilukayt.github.io/HieuGLLiteFE/";
		public readonly string jsonURL = "https://github.com/ShilukaYT/HieuGLLiteFE/raw/refs/heads/main/";
		private readonly string authURL = "https://shilukayt.github.io/DiscordAuth/";

		private List<GameApp> globalAppList = new List<GameApp>();



		public Main()
		{
			SetCurrentProcessExplicitAppUserModelID("HieuGLLite.Launcher.v1");
			InitializeComponent();
			ChangeWindowMessageFilter(WM_COPYDATA, MSGFLT_ADD);

			bool isDark = IsWindowsDarkMode();
			this.BackColor = isDark ? Color.FromArgb(18, 18, 18) : Color.White;
		}

		private void SetupSystemTray()
		{
			trayMenu = new ContextMenuStrip();
			trayMenu.Items.Add("Mở Hieu GL Lite", null, TrayOpen_Click);
			trayMenu.Items.Add("Thoát hoàn toàn", null, TrayExit_Click);

			trayIcon = new NotifyIcon();
			trayIcon.Text = "Hieu GL Lite";

			// MẸO TEST: Dùng icon mặc định của Windows để chắc chắn 100% nó hiển thị
			trayIcon.Icon = this.Icon;

			// Nếu bạn có file .ico riêng thì dùng dòng này (bỏ comment):
			// trayIcon.Icon = new Icon("duong_dan_toi_file_icon.ico");

			trayIcon.ContextMenuStrip = trayMenu;
			trayIcon.DoubleClick += TrayIcon_DoubleClick;

			// ÉP HIỆN NGAY LẬP TỨC
			trayIcon.Visible = true;
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
				MessageBox.Show("App 1 ĐÃ CHỘP ĐƯỢC LINK: " + receivedUrl);

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
			SetupSystemTray();
			// 2. Đồng bộ Theme hệ thống cho thanh Title
			ApplyTheme(IsWindowsDarkMode());

			this.Text = AppName;

			// 3. Cấu hình WebView2 trước khi khởi tạo
			// Quan trọng: Đặt DefaultBackgroundColor trùng màu App để xóa "đen màn"
			webView21.DefaultBackgroundColor = IsWindowsDarkMode() ? Color.FromArgb(18, 18, 18) : Color.White;
			webView21.ZoomFactor = 0.8;

			// Đăng ký các sự kiện
			webView21.CoreWebView2InitializationCompleted += WebView21_CoreWebView2InitializationCompleted;
			webView21.WebMessageReceived += WebView_WebMessageReceived;
			webView21.NavigationCompleted += webview21_NavigationCompleted;
			SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;

			// 4. Khởi tạo môi trường dữ liệu người dùng
			string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps");
			var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

			await webView21.EnsureCoreWebView2Async(env);

			webView21.CoreWebView2.SetVirtualHostNameToFolderMapping(
	"app.local",
	System.IO.Path.Combine(Application.StartupPath, "Assets"),
	CoreWebView2HostResourceAccessKind.Allow
);



			webView21.Source = new Uri(isDevMode ? "http://localhost:5173" : hostURL);

		}
		private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			if (e.Category == UserPreferenceCategory.General)
			{
				bool isDarkMode = IsWindowsDarkMode();
				this.Invoke((MethodInvoker)delegate
				{
					ApplyTheme(isDarkMode);
				});
			}
		}
		private void WebView21_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
		{
			if (e.IsSuccess)
			{
				// Cấu hình WebView2 sau khi khởi tạo thành công
				webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
				webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
				webView21.CoreWebView2.Settings.AreDevToolsEnabled = true;
				webView21.CoreWebView2.Settings.IsBuiltInErrorPageEnabled = false;
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
			try
			{
				var message = JObject.Parse(e.WebMessageAsJson); string type = message["type"]?.ToString();

				if (type == "SELECT_FOLDER")
				{
					// VẪN PHẢI GIỮ Invoke nhé, vì WebView2 event có thể khác luồng UI
					this.Invoke((MethodInvoker)delegate
					{
						using (FolderBrowserDialog fbd = new FolderBrowserDialog())
						{
							fbd.Description = "Chọn thư mục cài đặt giả lập";
							fbd.UseDescriptionForTitle = true; // Lên .NET 8 thì dòng này hoàn toàn hợp lệ!

							if (fbd.ShowDialog(this) == DialogResult.OK)
							{
								string selectedPath = fbd.SelectedPath.Replace("\\", "\\\\");

								string jsonResponse = $@"{{
                            ""type"": ""FOLDER_SELECTED"",
                            ""path"": ""{selectedPath}""
                        }}";

								webView21.CoreWebView2.PostWebMessageAsJson(jsonResponse);
							}
						}
					});
				}
				else if (type == "THEME_CHANGED")
				{
					string mode = message["mode"]?.ToString();
					this.Invoke((MethodInvoker)delegate
					{
						ApplyTheme(mode == "dark");
					});
				}
				else if (type == "RELOAD_APP")
				{
					this.Invoke((MethodInvoker)delegate
					{
						webView21.Source = new Uri("http://localhost:5173/");
					});
				}
				else if (type == "GET_CLIENT_VERSION")
				{
					this.Invoke((MethodInvoker)delegate
					{
						string jsonResponse = $@"{{
                            ""type"": ""CLIENT_VERSION"",
                            ""version"": ""{this.version}"",
                            ""versioncode"": ""{this.versioncode}""
                        }}";
						webView21.CoreWebView2.PostWebMessageAsJson(jsonResponse);

					});
				}
				else if (type == "CHANGE_TITLE")
				{
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
				}
				else if (type == "OPEN_URL")
				{
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
				}
				else if (type == "LOGIN_DISCORD")
				{
					// Bỏ cái Task.Run vào đây để app không bị đơ lúc chờ mở trình duyệt
					Task.Run(() =>
					{
						HandleDiscordLogin();
					});
				}
				else if (type == "CHECK_AUTO_LOGIN")
				{ // Vue vừa bật lên sẽ gọi cái này
					Task.Run(() => { AutoLoginDiscord(); });
				}
				else if (type == "LOGOUT_DISCORD")
				{ // Vue gọi khi bấm nút Đăng xuất
					if (System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat")))
					{
						System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat")); // Xóa trí nhớ
					}
				}
				else if (type == "GET_APPS")
				{
					Task.Run(async () =>
					{
						string jsonUrl = jsonURL + "appsList.json";

						// 2. CHỈ GÁN DỮ LIỆU, KHÔNG KHAI BÁO LẠI (Xóa chữ List<GameApp> ở đầu)
						globalAppList = await GetSyncedAppListAsync(jsonUrl);

						var response = new
						{
							type = "APPS_DATA",
							data = globalAppList
						};
						string jsonResponse = JsonConvert.SerializeObject(response);

						this.Invoke((MethodInvoker)delegate
						{
							webView21.CoreWebView2.PostWebMessageAsJson(jsonResponse);
						});
					});
				}

				else if (type == "PLAY")
				{
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
											this.Invoke((MethodInvoker)delegate {
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
				}
				else if (type == "KILL_APP")
				{
					string appId = message["appId"]?.ToString();
					Task.Run(() =>
					{
						var app = globalAppList.FirstOrDefault(a => a.id == appId);

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
				}
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
				//else if (type == "CLOSE_WINDOW")
				//{
				//	this.Invoke((MethodInvoker)delegate { this.Close(); });
				//}
			}

			catch (Exception ex)
			{
				MessageBox.Show("Lỗi C#: " + ex.Message, "Debug C#");
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

		// Đưa HttpListener ra làm biến của Class để dễ quản lý
		private HttpListener discordListener;
		private readonly HttpClient httpClient = new HttpClient();
        

        private void HandleDiscordLogin()
		{
			// 1. Điền Client ID từ trang Discord Developer Portal của bạn vào đây
			string clientId = "1475485221028626483"; // <--- THAY BẰNG SỐ CỦA BẠN

			// ĐỔI REDIRECT URI THÀNH TRANG WEB TRUNG GIAN CỦA BẠN (Phải khớp 100% với trên Discord)
			string redirectUri = authURL;

			string discordAuthUrl = $"https://discord.com/oauth2/authorize?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}&response_type=code&scope=identify";

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

			// SỬA LỖI: Link này phải khớp 100% với hàm HandleDiscordLogin và trên web Discord
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

						// 3. Lấy thông tin User (Tên, Avatar, Email) ngay lập tức
						var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
						userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
						// MessageBox.Show("Đã lấy được Access Token: " + accessToken); // Debug token
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
		private async void AutoLoginDiscord()
		{
			try
			{
				string savedToken = LoadSecureToken();

				if (!string.IsNullOrEmpty(savedToken))
				{
					var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
					userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);
					var userResponse = await httpClient.SendAsync(userRequest);

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
					}
					else
					{
						System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat"));
					}
				}
			}
			catch { /* Bỏ qua lỗi ngầm */ }
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
				System.IO.File.WriteAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat"), encryptedBytes);

				// (Tùy chọn) Ẩn file đi để người dùng không ngứa tay xóa mất
				System.IO.File.SetAttributes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat"), System.IO.FileAttributes.Hidden);
			}
			catch (Exception ex) { MessageBox.Show("Lỗi lưu token: " + ex.Message); }
		}

		private string LoadSecureToken()
		{
			if (!System.IO.File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat"))) return null;

			try
			{
				// Đọc file nhị phân đã mã hóa
				byte[] encryptedBytes = System.IO.File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat"));

				// Giải mã (Windows sẽ tự động dùng key của tài khoản hiện tại)
				byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);

				// Trả về token nguyên bản
				return Encoding.UTF8.GetString(plainBytes);
			}
			catch
			{
				// Giải mã thất bại (File bị lỗi hoặc bị mang sang máy khác)
				// Xóa luôn file rác này đi
				System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat"));
				return null;
			}
		}

		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason == CloseReason.UserClosing)
			{
				// Hỏi người dùng có chắc muốn thoát hay ẩn đi không
				var result = MessageBox.Show("Bạn có chắc muốn thoát hoàn toàn? Bấm 'Không' sẽ chỉ ẩn ứng dụng xuống khay hệ thống.", "Xác nhận thoát", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

				if (result == DialogResult.Yes)
				{
					// --- BẮT ĐẦU KIỂM TRA AN NINH ---
					if (isDownloading)
					{
						MessageBox.Show("Ứng dụng hiện đang cài đặt, bạn không thể thoát ngay lúc này.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						e.Cancel = true; // Chặn Windows giết app
						return; // Thoát hàm ngay
					}

					if (isRunning)
					{
						MessageBox.Show("Một ứng dụng đang chạy, bạn không thể thoát hoàn toàn lúc này.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						e.Cancel = true; // Chặn Windows giết app
						return; // Thoát hàm ngay
					}

					// --- NẾU QUA ĐƯỢC HẾT CÁC ẢI ---
					// Người dùng chọn "Có" và không có app nào chạy/tải -> Cho phép thoát hoàn toàn
					trayIcon.Visible = false;
					trayIcon.Dispose();
					// Không cần gọi Application.Exit() ở đây vì form đang tự đóng rồi.
				}
				else if (result == DialogResult.No)
				{
					e.Cancel = true; // Chặn Windows giết app
					this.Hide();     // Giấu cửa sổ đi (Icon dưới khay vẫn đang hiện sẵn rồi)
				}
				else
				{
					// Bấm Cancel hoặc đóng hộp thoại
					e.Cancel = true;
				}
			}
		}
		private void TrayIcon_DoubleClick(object sender, EventArgs e)
		{
			ShowApp();
		}

		// Khi bấm nút "Mở" trên menu chuột phải
		private void TrayOpen_Click(object sender, EventArgs e)
		{
			ShowApp();
		}

		// Hàm dùng chung để lôi app lên lại mặt đất
		private void ShowApp()
		{
			this.Show();                               // Hiện lại Form
			this.WindowState = FormWindowState.Normal; // Đảm bảo không bị thu nhỏ
			this.Activate();                           // Đưa lên trên cùng các cửa sổ khác
		}

		// Khi bấm "Thoát hoàn toàn" trên menu chuột phải
		private void TrayExit_Click(object sender, EventArgs e)
		{
			// 1. Check xem có đang tải file không?
			if (isDownloading)
			{
				MessageBox.Show("Ứng dụng hiện đang cài đặt, bạn không thể thoát ngay lúc này.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return; // Đuổi về, không chạy tiếp xuống dưới
			}

			// 2. Check xem có giả lập nào đang mở không?
			if (isRunning)
			{
				MessageBox.Show("Một ứng dụng đang chạy, bạn không thể thoát hoàn toàn lúc này.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return; // Đuổi về tiếp
			}

			// 3. Vượt qua được 2 vòng bảo vệ trên thì mới cho phép tắt!
			trayIcon.Visible = false;
			trayIcon.Dispose();     // Dọn dẹp RAM
			Application.Exit();     // Tắt chết app thực sự!
		}

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

			try
			{
				using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
				{
					using (RegistryKey key = baseKey.OpenSubKey(regPath))
					{
						if (key != null)
						{
							// Lấy InstallDir làm nơi chứa file chạy (.exe)
							string installDir = key.GetValue("InstallDir")?.ToString();

							// Lấy UserDefinedDir làm nơi chứa Data (.conf, vhd...)
							string userDefinedDir = key.GetValue("UserDefinedDir")?.ToString();

							// Lấy Version gốc
							string rawVersion = key.GetValue("Version")?.ToString();

							// Nếu có đủ 2 đường dẫn quan trọng thì xác nhận Đã Cài Đặt
							if (!string.IsNullOrEmpty(installDir) && !string.IsNullOrEmpty(userDefinedDir))
							{
								app.isInstalled = true;
								app.programPath = installDir;
								app.dataPath = userDefinedDir; // Gán chéo theo ý bạn

								// Xử lý cắt Version lấy 3 số (VD: 5.22.164.1002 -> 5.22.164)
								if (!string.IsNullOrEmpty(rawVersion))
								{
									var parts = rawVersion.Split('.');
									if (parts.Length >= 3)
									{
										app.installedVersion = $"{parts[0]}.{parts[1]}.{parts[2]}";
									}
									else
									{
										app.installedVersion = rawVersion; // Đề phòng version ngắn
									}
								}

								// Đọc khóa đặc biệt (VD: bs5_vip) nếu bạn có dùng
								// string specialId = key.GetValue("HGL_ID")?.ToString();

								// Gọi hàm đọc file .conf từ cái userDefinedDir (dataPath) vừa lấy được
								// LoadInstancesFromConf(app);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Lỗi quét Registry: {ex.Message}");
			}
		}
	}
}
