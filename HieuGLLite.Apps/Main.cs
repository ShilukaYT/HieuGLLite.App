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



namespace HieuGLLite.Apps
{
	public partial class Main : Form
	{
		// --- Windows API để làm đẹp UI ---
		[DllImport("dwmapi.dll")]
		public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

		[DllImport("shell32.dll", SetLastError = true)]
		static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

		public string version = "26.2.0";
		public int versioncode = 260200;

		public Main()
		{
			// 1. Gom nhóm tiến trình Task Manager ngay lập tức (Giống Outlook)
			SetCurrentProcessExplicitAppUserModelID("HieuGLLite.Launcher.v1");
			InitializeComponent();


			// Đặt màu nền Form ngay từ Constructor để tránh lóe trắng
			bool isDark = IsWindowsDarkMode();
			this.BackColor = isDark ? Color.FromArgb(18, 18, 18) : Color.White;
		}

		private async void Main_Load(object sender, EventArgs e)
		{
			// 2. Đồng bộ Theme hệ thống cho thanh Title
			ApplyTheme(IsWindowsDarkMode());

			this.Text = "Hieu GL Lite";

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


			webView21.Source = new Uri("https://shilukayt.github.io/HieuGLLite.App/Webview-Frontend/dist/");

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
					if (File.Exists(offlineFilePath))
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
							fbd.Description = "Chọn thư mục cài đặt Game/Giả lập";
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
						newTitle = title + " - Hieu GL Lite";
					}
					else

					{
						newTitle = "Hieu GL Lite";
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

		private async void HandleDiscordLogin()
		{
			string clientId = "1475485221028626483";
			string clientSecret = "5ATtI-m3quy2xg_GHoXoFqdGASACRwf0";
			string redirectUri = "http://localhost:5000/";

			try
			{
				// 1. DỌN DẸP SERVER CŨ: Hủy port 5000 nếu nó đang bị treo từ lần bấm trước
				if (discordListener != null)
				{
					if (discordListener.IsListening)
					{
						discordListener.Stop(); // Dừng nghe
					}
					discordListener.Close(); // Giải phóng hoàn toàn
				}

				// 2. KHỞI TẠO SERVER MỚI SẠCH SẼ
				discordListener = new HttpListener();
				discordListener.Prefixes.Add(redirectUri);
				discordListener.Start();

				string authUrl = $"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={WebUtility.UrlEncode(redirectUri)}&response_type=code&scope=identify%20email";
				Process.Start(new ProcessStartInfo(authUrl) { UseShellExecute = true });

				// Đứng đợi trình duyệt...
				HttpListenerContext context = await discordListener.GetContextAsync();
				string code = context.Request.QueryString["code"];

				// Phản hồi cho trình duyệt
				byte[] buffer = System.Text.Encoding.UTF8.GetBytes("<html><body style='text-align:center;margin-top:50px;font-family:sans-serif;'><h1>Đăng nhập thành công!</h1><p>Bạn có thể tắt trang này và quay lại Launcher.</p></body></html>");
				context.Response.ContentLength64 = buffer.Length;
				context.Response.OutputStream.Write(buffer, 0, buffer.Length);
				context.Response.Close();

				// 3. XONG VIỆC LÀ PHẢI ĐÓNG SERVER NGAY LẬP TỨC
				discordListener.Stop();
				discordListener.Close();

				if (string.IsNullOrEmpty(code)) return;

				// 4. TIẾN HÀNH ĐỔI TOKEN NHƯ CŨ
				var tokenParams = new Dictionary<string, string>
		{
			{ "client_id", clientId },
			{ "client_secret", clientSecret },
			{ "grant_type", "authorization_code" },
			{ "code", code },
			{ "redirect_uri", redirectUri }
		};

				var tokenResponse = await httpClient.PostAsync("https://discord.com/api/oauth2/token", new FormUrlEncodedContent(tokenParams));
				string tokenString = await tokenResponse.Content.ReadAsStringAsync();
				string accessToken = JObject.Parse(tokenString)["access_token"]?.ToString();

				if (string.IsNullOrEmpty(accessToken)) throw new Exception("Không lấy được Token từ Discord");

				// (Tùy chọn) Lưu Token để lần sau auto-login
				SaveSecureToken(accessToken);

				// 5. LẤY PROFILE VÀ GỬI XUỐNG VUE
				var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
				userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
				var userResponse = await httpClient.SendAsync(userRequest);

				string userString = await userResponse.Content.ReadAsStringAsync();
				var userJson = JObject.Parse(userString);

				var profile = new
				{
					name = userJson["username"]?.ToString(),
					email = userJson["email"]?.ToString(),
					avatar = $"https://cdn.discordapp.com/avatars/{userJson["id"]}/{userJson["avatar"]}.png"
				};

				this.Invoke((MethodInvoker)delegate {
					var responseData = new { type = "USER_LOGGED_IN", data = profile };
					webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(responseData));
				});
				// 8. Bắn dữ liệu thẳng xuống Vue và BẬT FORM LÊN
				this.Invoke((MethodInvoker)delegate {
					// 8.1 Gửi dữ liệu xuống Vue (Code cũ của bạn)
					var responseData = new { type = "USER_LOGGED_IN", data = profile };
					webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(responseData));

					// 8.2 --- ĐOẠN CODE MỚI ĐỂ ÉP BẬT FORM LÊN TRÊN CÙNG ---

					// Nếu app đang bị thu nhỏ (Minimize), mở nó lên lại
					if (this.WindowState == FormWindowState.Minimized)
					{
						this.WindowState = FormWindowState.Normal;
					}

					// Mẹo "Steal Focus" (Giật tiêu điểm) của Windows API:
					// Bật TopMost thành true để ép nó nổi lên trên cả trình duyệt
					this.TopMost = true;

					// Yêu cầu Windows kích hoạt và focus vào Form này
					this.Activate();
					this.Focus();

					// Ngay lập tức tắt TopMost đi để Form trở lại bình thường 
					// (nếu không tắt, Launcher của bạn sẽ đè lên mọi app khác vĩnh viễn)
					this.TopMost = false;
				});
			}
			catch (HttpListenerException)
			{
				// Bỏ qua lỗi ngầm khi ta chủ động Stop() cái listener cũ đang chạy dở
			}
			catch (Exception ex)
			{
				MessageBox.Show("Lỗi đăng nhập: " + ex.Message);
			}
		}
		private async void AutoLoginDiscord()
		{
			try
			{
				string savedToken = LoadSecureToken();

				if (!string.IsNullOrEmpty(savedToken))
				{

					// Dùng token cũ hỏi thử Discord xem còn xài được không
					var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
					userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);
					var userResponse = await httpClient.SendAsync(userRequest);

					if (userResponse.IsSuccessStatusCode)
					{
						// Token hợp lệ! Lấy thông tin và báo cho Vue
						string userString = await userResponse.Content.ReadAsStringAsync();
						var userJson = JObject.Parse(userString);
						var profile = new
						{
							name = userJson["username"]?.ToString(),
							email = userJson["email"]?.ToString(),
							avatar = $"https://cdn.discordapp.com/avatars/{userJson["id"]}/{userJson["avatar"]}.png"
						};

						this.Invoke((MethodInvoker)delegate {
							var responseData = new { type = "USER_LOGGED_IN", data = profile };
							webView21.CoreWebView2.PostWebMessageAsJson(JsonConvert.SerializeObject(responseData));
						});
					}
					else
					{
						// Token đã hết hạn hoặc bị lỗi -> Xóa file đi bắt đăng nhập lại
						System.IO.File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HieuGLLite.Apps", "discord_token.dat"));
					}
				}
			}
			catch { /* Bỏ qua lỗi ngầm để không làm crash app lúc khởi động */ }
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
	}
}
