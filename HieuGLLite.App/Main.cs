using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json.Linq;

namespace HieuGLLite.App
{
	public partial class Main : Form
	{
		public Main()
		{
			InitializeComponent();
		}

		private async void Form1_Load(object sender, EventArgs e)
		{
			this.Text = "Hieu GL Lite";
			webView21.AllowExternalDrop = true;
			webView21.DefaultBackgroundColor = Color.Black;
			webView21.ZoomFactor = 0.8;

			// Gom chung việc đăng ký sự kiện khởi tạo vào 1 chỗ
			webView21.CoreWebView2InitializationCompleted += WebView21_CoreWebView2InitializationCompleted;
			webView21.WebMessageReceived += WebView_WebMessageReceived;

			await webView21.EnsureCoreWebView2Async(null);

			webView21.Source = new Uri("http://localhost:5173/");
		}

		private void WebView21_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
		{
			if (e.IsSuccess)
			{
				// Cấu hình WebView2 sau khi khởi tạo thành công
				webView21.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
				webView21.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
				webView21.CoreWebView2.Settings.AreDevToolsEnabled = true;
			}
			else
			{
				MessageBox.Show("WebView2 initialization failed: " + e.InitializationException);
			}
		}

		private void WebView_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
		{
			try
			{
				var message = JObject.Parse(e.TryGetWebMessageAsString());
				string type = message["type"]?.ToString();

				if (type == "SELECT_FOLDER")
				{
					// Ép chạy trên luồng giao diện chính
					this.Invoke((MethodInvoker)delegate
					{
						using (FolderBrowserDialog fbd = new FolderBrowserDialog())
						{
							fbd.Description = "Chọn thư mục để cài đặt phần mềm";

							// --- DÒNG CHÌA KHÓA GIẢI QUYẾT LỖI ---
							// Tắt giao diện mới, ép dùng giao diện cơ bản để tránh lỗi "Class not registered"
							fbd.AutoUpgradeEnabled = false;

							if (fbd.ShowDialog(this) == DialogResult.OK)
							{
								string selectedPath = fbd.SelectedPath;
								selectedPath = selectedPath.Replace("\\", "\\\\");

								string jsonResponse = $@"{{
                            ""type"": ""FOLDER_SELECTED"",
                            ""path"": ""{selectedPath}""
                        }}";

								webView21.CoreWebView2.PostWebMessageAsJson(jsonResponse);
							}
						}
					});
				}
				else if (type == "INSTALL")
				{
					// Logic cài đặt...
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Lỗi C#: " + ex.Message, "Debug C#");
			}
		}
	}
}