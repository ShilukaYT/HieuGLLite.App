using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HieuGLLite.Apps
{
	public static class FontManager
	{
		// Bộ sưu tập font "bí mật" không cần cài vào Windows
		private static PrivateFontCollection _pfc = new PrivateFontCollection();
		private static bool _isLoaded = false;

		public static void LoadCustomFont()
		{
			if (_isLoaded) return;

			// Đường dẫn tới file font của bạn (Sửa lại tên file cho đúng nhé)
			string fontPath = Path.Combine(Application.StartupPath, "Assets", "Fonts", "GoogleSans-Medium.ttf");
			Main.WriteLog("INFO", "Loading font");

			if (File.Exists(fontPath))
			{
				_pfc.AddFontFile(fontPath);
				_isLoaded = true;
			}
			else
			{
				Main.WriteLog("WARNING", "Font not found");
				System.Diagnostics.Debug.WriteLine("Không tìm thấy file font!");
			}
		}

		// Hàm để lấy font với kích cỡ và kiểu dáng tùy ý
		public static Font GetFont(float size, FontStyle style = FontStyle.Regular)
		{
			// Nếu đã load thành công, trả về Custom Font
			if (_isLoaded && _pfc.Families.Length > 0)
			{
				Main.WriteLog("INFO", "Applying font");
				return new Font(_pfc.Families[0], size, style);
			}

			// Nếu lỗi, trả về font mặc định chữa cháy
			Main.WriteLog("WARNING", "Font not found, returning default font");
			return new Font("Segoe UI", size, style);
		}
	}
}
