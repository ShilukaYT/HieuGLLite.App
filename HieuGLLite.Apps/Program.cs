using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using static HieuGLLite.Apps.Main;

namespace HieuGLLite.Apps
{
	internal static class Program
	{
		// Tạo một chuỗi định danh duy nhất (có thể dùng GUID)
		private static string appGuid = "69ba7d1a-d023-476b-86e2-7652abebb84d";
		private static Mutex mutex = new Mutex(true, appGuid);

		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		private const int SW_RESTORE = 9;
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// 1. Bắt lỗi ở cấp độ toàn ứng dụng (Global Exception Handler)
			Application.ThreadException += (s, e) => ShowError(e.Exception);
			AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowError((Exception)e.ExceptionObject);
			// Kiểm tra xem Mutex đã tồn tại chưa
			if (mutex.WaitOne(TimeSpan.Zero, true))
			{
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);
				// 1. KIỂM TRA WEBVIEW2 TRƯỚC KHI CHẠY FORM
				if (!IsWebView2Installed())
				{
					// Nếu chưa cài, hiện bảng thông báo
					DialogResult result = MessageBox.Show(
						"Ứng dụng cần Microsoft Edge WebView2 Runtime để hiển thị giao diện.\n\nBạn có muốn tải và cài đặt nó ngay bây giờ không?",
						"Thiếu thành phần hệ thống",
						MessageBoxButtons.YesNo,
						MessageBoxIcon.Warning);

					if (result == DialogResult.Yes)
					{
						// Mở link tải trực tiếp file cài đặt siêu nhẹ (Evergreen Bootstrapper) của Microsoft
						System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
						{
							FileName = System.IO.Path.Combine(Application.StartupPath, "runtimes", "MicrosoftEdgeWebView2Setup.exe"),
							UseShellExecute = true
						});
					}

					// Dừng app lại, không cho chạy tiếp để tránh Crash
					return;
				}

				RegisterCustomProtocol();

				// Chạy Form chính của bạn
				Application.Run(new Main());

				// Giải phóng khi thoát
				mutex.ReleaseMutex();
			}
			else
			{
				// Nếu đã chạy rồi, tìm ứng dụng cũ và đưa nó lên phía trước
				// Đọc tham số dòng lệnh xem có link Discord không
				string[] args = Environment.GetCommandLineArgs();
				string discordUrl = (args.Length > 1) ? args[1] : "";

				Process current = Process.GetCurrentProcess();
				foreach (Process process in Process.GetProcessesByName(current.ProcessName))
				{
					if (process.Id != current.Id)
					{
						IntPtr handle = process.MainWindowHandle;

						// 1. Đưa Launcher cũ lên mặt đất (Code của bạn)
						ShowWindow(handle, 9); // 9 là SW_RESTORE
						SetForegroundWindow(handle);

						// 2. NẾU CÓ LINK DISCORD -> Ném thẳng sang Launcher cũ
						if (!string.IsNullOrEmpty(discordUrl) && discordUrl.StartsWith("hieugllite://"))
						{
							// Đặt thông báo ở đây xem App 2 có thực sự chạy không
							MessageBox.Show("App 2 đang chuẩn bị ném link: " + discordUrl); 

							COPYDATASTRUCT cds = new COPYDATASTRUCT();
							cds.dwData = IntPtr.Zero;
							cds.cbData = (discordUrl.Length + 1) * 2;
							// Cấp phát vùng nhớ không bị dọn dẹp
							cds.lpData = Marshal.StringToHGlobalUni(discordUrl);

							// Bắn tín hiệu!
							SendMessage(handle, WM_COPYDATA, IntPtr.Zero, ref cds);

							// Dọn dẹp RAM sau khi bắn xong
							Marshal.FreeHGlobal(cds.lpData);
						}
						Environment.Exit(0);
					}
				}
			}

			static void ShowError(Exception ex)
			{
				// Khi app sắp sập, nó sẽ hiện lên cái bảng này để bạn biết nguyên nhân thực sự
				MessageBox.Show($"ỨNG DỤNG BỊ LỖI VÀ PHẢI ĐÓNG:\n\n{ex.Message}\n\nChi tiết: {ex.StackTrace}", "Hệ thống cứu hộ");
			}
		}

		private static void RegisterCustomProtocol()
		{
			// Tên giao thức của bạn (chỉ viết chữ thường, không có ://)
			string protocol = "hieugllite";

			// Đường dẫn tuyệt đối tới file .exe đang chạy
			string appPath = Application.ExecutablePath;

			try
			{
				// Tạo nhánh trong HKEY_CURRENT_USER để KHÔNG đòi quyền Admin
				string keyPath = $@"Software\Classes\{protocol}";

				using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
				{
					// 1. Khai báo đây là một URL Protocol
					key.SetValue("", $"URL:{protocol} Protocol");
					key.SetValue("URL Protocol", ""); // Value rỗng nhưng bắt buộc phải có key này

					// 2. Trỏ Icon mặc định về chính file exe này (Giúp nhận diện tốt hơn)
					using (RegistryKey iconKey = key.CreateSubKey("DefaultIcon"))
					{
						iconKey.SetValue("", $"\"{appPath}\",0");
					}

					// 3. Cấu hình lệnh khởi chạy (Mở file exe và truyền tham số link vào)
					using (RegistryKey commandKey = key.CreateSubKey(@"shell\open\command"))
					{
						// Lấy đường dẫn đang lưu trong Registry (nếu có)
						string currentCommand = (string)commandKey.GetValue("");

						// Cú pháp chuẩn của Windows: "Đường_dẫn_exe" "%1"
						string newCommand = $"\"{appPath}\" \"%1\"";

						// Chỉ ghi đè Registry NẾU đường dẫn app đã bị thay đổi
						if (currentCommand != newCommand)
						{
							commandKey.SetValue("", newCommand);
						}
					}
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Lỗi khi đăng ký Custom Protocol:\n{ex.Message}",
								"Lỗi Registry",
								MessageBoxButtons.OK,
								MessageBoxIcon.Warning);
			}
		}

		// Hàm phụ trợ để kiểm tra WebView2
		private static bool IsWebView2Installed()
		{
			try
			{
				// Thử lấy chuỗi phiên bản của WebView2 trên máy
				string version = CoreWebView2Environment.GetAvailableBrowserVersionString();
				return !string.IsNullOrWhiteSpace(version);
			}
			catch (WebView2RuntimeNotFoundException)
			{
				return false; // Bắt được lỗi nghĩa là chưa cài!
			}
			catch (Exception)
			{
				return false;
			}
		}
	}



}
	