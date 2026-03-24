using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HieuGLLite.Apps
{
	public partial class CustomMessageBox : Form
	{// Import thư viện Windows API để bo tròn góc của Form
		[DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
		private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
		[DllImport("user32.dll")]
		public static extern bool ReleaseCapture();

		// Khai báo các Control
		private Label lblTitle;
		private Label lblMessage;
		private CustomRoundButton btnYes;
		private CustomRoundButton btnNo;

		public CustomMessageBox()
		{
			// 1. Cấu hình Form cơ bản
			this.FormBorderStyle = FormBorderStyle.None;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.ShowInTaskbar = false;
			this.TopMost = true;
			this.BackColor = Color.FromArgb(28, 28, 28); // Màu nền tối xám giống ảnh 2
			this.Size = new Size(480, 220); // Kích thước rộng rãi

			// 2. Bo góc sâu cho toàn bộ Form (Bán kính 24)
			this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 24, 24));

			// 3. Tự động vẽ giao diện
			InitializeCustomUI();

			// 4. Cho phép cầm bất kỳ đâu trên Form để kéo đi
			this.MouseDown += DragForm_MouseDown;
			lblTitle.MouseDown += DragForm_MouseDown;
			lblMessage.MouseDown += DragForm_MouseDown;
		}

		private void InitializeCustomUI()
		{
			// --- TIÊU ĐỀ ---
			lblTitle = new Label();
			lblTitle.Text = "Tiêu đề";
			lblTitle.ForeColor = Color.White;
			lblTitle.Font = FontManager.GetFont(16f, FontStyle.Regular); // Font to, thanh lịch
			lblTitle.Location = new Point(30, 30);
			lblTitle.AutoSize = true;
			this.Controls.Add(lblTitle);

			// --- NỘI DUNG ---
			lblMessage = new Label();
			lblMessage.Text = "Nội dung thông báo";
			lblMessage.ForeColor = Color.FromArgb(220, 220, 220); // Màu chữ xám sáng
			lblMessage.Font = FontManager.GetFont(10.5f, FontStyle.Regular);
			lblMessage.Location = new Point(30, 75);
			lblMessage.Size = new Size(this.Width - 60, 60);
			lblMessage.AutoSize = false;
			this.Controls.Add(lblMessage);

			// --- NÚT BẤM CHÍNH (Xanh ngọc ngập tràn) ---
			btnYes = new CustomRoundButton();
			btnYes.Text = "Đồng ý";
			btnYes.Size = new Size(140, 44);
			btnYes.Location = new Point(this.Width - 170, this.Height - 74);
			btnYes.BackColor = Color.FromArgb(29, 245, 143); // Mã màu xanh ngọc giống ảnh
			btnYes.ForeColor = Color.Black;
			btnYes.Font = FontManager.GetFont(12f, FontStyle.Regular);
			btnYes.DialogResult = DialogResult.Yes;
			this.Controls.Add(btnYes);

			// --- NÚT BẤM PHỤ (Nền trong, viền xám) ---
			btnNo = new CustomRoundButton();
			btnNo.Text = "Đã hiểu"; // Tương đương "Got it"
			btnNo.Size = new Size(110, 44);
			btnNo.Location = new Point(this.Width - 290, this.Height - 74);
			btnNo.BackColor = this.BackColor;
			btnNo.ForeColor = Color.White;
			btnNo.BorderColor = Color.FromArgb(100, 100, 100); // Viền xám bọc ngoài
			btnNo.Font = FontManager.GetFont(12f, FontStyle.Regular);
			btnNo.DialogResult = DialogResult.No;
			this.Controls.Add(btnNo);
		}

		// Xử lý kéo thả cửa sổ
		private void DragForm_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ReleaseCapture();
				SendMessage(this.Handle, 0x112, 0xf012, 0);
			}
		}

		// Hàm gọi tĩnh tự động co giãn (Auto-size)
		public static DialogResult Show(string message, string title, bool isQuestion = false)
		{
			using (CustomMessageBox msgBox = new CustomMessageBox())
			{
				msgBox.lblTitle.Text = title;
				msgBox.lblMessage.Text = message;

				// ==========================================
				// 1. ĐO ĐẠC CHIỀU CAO VĂN BẢN (Tự động xuống dòng)
				// ==========================================
				int textWidth = msgBox.Width - 60; // Trừ đi 30px lề mỗi bên
				Size maxSize = new Size(textWidth, int.MaxValue);

				// Thuật toán đo khoảng trống cần thiết để hiển thị hết chữ
				Size requiredSize = TextRenderer.MeasureText(message, msgBox.lblMessage.Font, maxSize, TextFormatFlags.WordBreak);

				// ==========================================
				// 2. KÉO DÀI FORM VÀ LABEL
				// ==========================================
				msgBox.lblMessage.Size = new Size(textWidth, requiredSize.Height + 5);

				// Công thức tính: Lề trên (75) + Chiều cao chữ + Khoảng cách đến nút + Nút(44) + Lề dưới(30)
				int newFormHeight = 75 + requiredSize.Height + 30 + 44 + 30;

				// Chỉ kéo dài nếu nội dung thực sự cần (Giữ mức tối thiểu là 220px cho đẹp)
				if (newFormHeight > 220)
				{
					msgBox.Height = newFormHeight;

					// CHÚ Ý: Vì Form bị đổi size, ta phải dùng khuôn dập cắt lại viền bo tròn 24px
					msgBox.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, msgBox.Width, msgBox.Height, 24, 24));
				}

				// ==========================================
				// 3. DỜI VỊ TRÍ 2 NÚT BẤM XUỐNG ĐÁY MỚI
				// ==========================================
				msgBox.btnYes.Location = new Point(msgBox.Width - msgBox.btnYes.Width - 30, msgBox.Height - 74);
				msgBox.btnNo.Location = new Point(msgBox.Width - msgBox.btnYes.Width - msgBox.btnNo.Width - 40, msgBox.Height - 74);

				// ==========================================
				// 4. CẤU HÌNH LOẠI THÔNG BÁO
				// ==========================================
				if (isQuestion)
				{
					msgBox.btnYes.Text = "Có";
					msgBox.btnNo.Text = "Không";
				}
				else
				{
					msgBox.btnYes.Text = "Đã hiểu";
					msgBox.btnNo.Visible = false;
					// Nếu chỉ có 1 nút thì dời nó sang sát mép phải
					msgBox.btnYes.Location = new Point(msgBox.Width - msgBox.btnYes.Width - 30, msgBox.Height - 74);
				}

				return msgBox.ShowDialog();
			}
		}
	}

	// ==============================================================
	// LỚP VẼ NÚT BẤM HÌNH VIÊN THUỐC (CÓ HOVER & CĂN CHỮ HOÀN HẢO)
	// ==============================================================
	public class CustomRoundButton : Button
	{
		public Color BorderColor { get; set; } = Color.Transparent;

		// Biến theo dõi trạng thái chuột
		private bool isHovered = false;

		public CustomRoundButton()
		{
			this.FlatStyle = FlatStyle.Flat;
			this.FlatAppearance.BorderSize = 0;
			this.Cursor = Cursors.Hand;
		}

		// Bắt sự kiện chuột đưa vào
		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			isHovered = true;
			this.Invalidate(); // Yêu cầu vẽ lại nút
		}

		// Bắt sự kiện chuột đưa ra
		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			isHovered = false;
			this.Invalidate(); // Yêu cầu vẽ lại nút
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

			// Xóa góc vuông răng cưa
			Color parentColor = this.Parent != null ? this.Parent.BackColor : Color.FromArgb(28, 28, 28);
			e.Graphics.Clear(parentColor);

			Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
			int radius = this.Height - 1;

			GraphicsPath path = new GraphicsPath();
			path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
			path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
			path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
			path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
			path.CloseFigure();

			// ==========================================
			// XỬ LÝ MÀU NỀN KHI HOVER
			// ==========================================
			Color fillColor = this.BackColor;
			if (isHovered)
			{
				if (BorderColor != Color.Transparent)
				{
					// NÚT PHỤ (Không): Hover vào sẽ hiện nền xám nhạt
					fillColor = Color.FromArgb(60, 60, 60);
				}
				else
				{
					// NÚT CHÍNH (Có): Hover vào sẽ sáng màu xanh lên một chút
					fillColor = Color.FromArgb(
						Math.Min(255, this.BackColor.R + 30),
						Math.Min(255, this.BackColor.G + 30),
						Math.Min(255, this.BackColor.B + 30)
					);
				}
			}

			// Tô nền nút
			using (SolidBrush brush = new SolidBrush(fillColor))
			{
				e.Graphics.FillPath(brush, path);
			}

			// Vẽ viền (chỉ áp dụng cho nút Phụ)
			if (BorderColor != Color.Transparent)
			{
				using (Pen pen = new Pen(BorderColor, 1.5f))
				{
					e.Graphics.DrawPath(pen, path);
				}
			}

			// ==========================================
			// VẼ CHỮ VÀ ÉP CĂN GIỮA TUYỆT ĐỐI
			// ==========================================
			e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			StringFormat sf = new StringFormat();
			sf.Alignment = StringAlignment.Center;
			sf.LineAlignment = StringAlignment.Center;

			// THỦ THUẬT: Dịch vùng vẽ chữ xuống dưới 2 pixel để bù trừ lỗi đường cơ sở của font
			Rectangle textRect = new Rectangle(0, 2, this.Width, this.Height);

			using (SolidBrush textBrush = new SolidBrush(this.ForeColor))
			{
				e.Graphics.DrawString(this.Text, this.Font, textBrush, textRect, sf);
			}
		}
	}



}
