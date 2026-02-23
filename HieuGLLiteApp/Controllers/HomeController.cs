using System.Diagnostics;
using HieuGLLiteApp.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text.Json;
using WebCore.Models;

namespace HieuGLLiteApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

		// DỮ LIỆU GIẢ (Thay vì đọc JSON/Database)
		private static List<Software> _fakeDb = new List<Software>
		{
			new Software { Id = "sw_01", Name = "BlueStacks Tweaker", Version = "6.8.5", Description = "Công cụ mod BlueStacks mạnh nhất.", IconUrl = "https://img.icons8.com/color/96/android-os.png", IsInstalled = false },
			new Software { Id = "sw_02", Name = "LDPlayer Manager", Version = "4.0.2", Description = "Quản lý đa nhiệm LDPlayer.", IconUrl = "https://img.icons8.com/color/96/console.png", IsInstalled = true },
			new Software { Id = "sw_03", Name = "Auto Click Pro", Version = "2.1.0", Description = "Tự động click không chiếm chuột.", IconUrl = "https://img.icons8.com/color/96/mouse.png", IsInstalled = false }
		};

		public IActionResult Index()
		{
			return View();
		}

		// API trả về JSON cho Frontend
		[HttpGet]
		public IActionResult GetSoftwares()
		{
			return Json(_fakeDb);
		}

		// API giả lập hành động cài đặt
		[HttpPost]
		public IActionResult Install(string id)
		{
			var soft = _fakeDb.FirstOrDefault(x => x.Id == id);
			if (soft != null)
			{
				// Ở đây sau này bạn sẽ viết code tải file thật
				// Tạm thời mình chỉ trả về thành công
				return Ok(new { message = $"Đang bắt đầu tải {soft.Name}..." });
			}
			return NotFound();
		}

		public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
