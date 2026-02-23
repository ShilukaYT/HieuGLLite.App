using System.Diagnostics;
using System.Runtime.InteropServices;

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

				// Chạy Form chính của bạn
				Application.Run(new Main());

				// Giải phóng khi thoát
				mutex.ReleaseMutex();
			}
			else
			{
				// Nếu đã chạy rồi, tìm ứng dụng cũ và đưa nó lên phía trước
				Process current = Process.GetCurrentProcess();
				foreach (Process process in Process.GetProcessesByName(current.ProcessName))
				{
					if (process.Id != current.Id)
					{
						IntPtr handle = process.MainWindowHandle;
						ShowWindow(handle, SW_RESTORE);
						SetForegroundWindow(handle);
						break;
					}
				}
			}

			static void ShowError(Exception ex)
			{
				// Khi app sắp sập, nó sẽ hiện lên cái bảng này để bạn biết nguyên nhân thực sự
				MessageBox.Show($"ỨNG DỤNG BỊ LỖI VÀ PHẢI ĐÓNG:\n\n{ex.Message}\n\nChi tiết: {ex.StackTrace}", "Hệ thống cứu hộ");
			}
		}
	}
}