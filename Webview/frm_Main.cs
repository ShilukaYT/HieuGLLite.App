using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;

namespace Webview
{
    public partial class frm_Main : Form
    {
        public frm_Main()
        {
            InitializeComponent();
			this.Text = "My Launcher Professional";
			this.Size = new Size(1000, 700);

			// Khởi chạy WebView2
			InitWebView();
		}

		async void InitWebView()
		{
			// Đảm bảo WebView2 đã sẵn sàng
			await webView.EnsureCoreWebView2Async(null);

			// Trỏ vào localhost MVC đang chạy ngầm
			webView.Source = new Uri("http://localhost:5000");

			// Tắt menu chuột phải mặc định cho giống App
			// Ở đây mình bật lại để dễ debug, bạn có thể tắt nếu muốn
			webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
			webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

			// Đăng ký sự kiện nhận tin nhắn từ Web
			webView.CoreWebView2.WebMessageReceived += WebView_WebMessageReceived;
		}

		private async void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
		{
			// 1. Đọc JSON từ Web gửi lên
			string json = e.WebMessageAsJson;
			dynamic data = JsonConvert.DeserializeObject(json);
			string type = data.type;
			string content = data.content;

			// 2. Xử lý logic hiển thị MessageBox
			if (type == "CONFIRM_INSTALL")
			{
				var result = MessageBox.Show(content, "Xác nhận tải", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				string resStr = result == DialogResult.Yes ? "Yes" : "No";

				// Bắn kết quả ngược lại Web
				await webView.ExecuteScriptAsync($"handleWinFormResult('{type}', '{resStr}', null)");
			}
			else if (type == "CONFIRM_CONFIG")
			{
				var result = MessageBox.Show(content, "Cảnh báo Config", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				string resStr = result == DialogResult.Yes ? "Yes" : "No";
				string appId = data.appId;

				// Bắn kết quả kèm ID App để Web biết đường sửa
				await webView.ExecuteScriptAsync($"handleWinFormResult('{type}', '{resStr}', '{appId}')");
			}
		}
	}
}

