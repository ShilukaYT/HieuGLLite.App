using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HieuGLLite.Apps
{
    internal class AppModel
    {
		public class GameApp
		{
			public string id { get; set; }
			public string name { get; set; }
			public string desc { get; set; }
			public string oem { get; set; }
			public string icon { get; set; }
			public string background { get; set; }
			public long requiredSize { get; set; }
			public string exeName { get; set; } // Đổi từ fileName thành exeName

			public string badge { get; set; } = "";

			public List<TagItem> tags { get; set; } = new List<TagItem>();

			// --- CÁC BIẾN TRẠNG THÁI (C# TỰ QUÉT VÀ BƠM VÀO) ---
			public string programPath { get; set; } = "";
			public string dataPath { get; set; } = "";
			public bool isInstalled { get; set; } = false;
			public bool isLauncherRequired { get; set; } = false;

			// NHỮNG BIẾN ĐỌC THÊM TỪ REGISTRY
			public string installedVersion { get; set; } = "";

			// biến đọc thêm từ việc quét tiến trình đang chạy

			public List<int> runningProcessIds { get; set; } = new List<int>();

			public List<AppVersion> versions { get; set; }
		}

		public class TagItem
		{
			public string text { get; set; } // Lưu chữ: "Google Play Games"
			public string icon { get; set; } // Lưu đường dẫn: "assets/google_play.svg" hoặc "mdi-leaf"
		}

		public class AppVersion
		{
			public string ver { get; set; }
			public string downloadURL { get; set; }
			public string fileName { get; set; } // THÊM fileName cho bản cài gốc
			public long fileSize { get; set; }

			public string SHA256 { get; set; }
			public List<AndroidCore> androids { get; set; }
		}

		public class AndroidCore
		{
			public string name { get; set; }
			public string code { get; set; }
			public string downloadURL { get; set; }
			public string fileName { get; set; } // THÊM fileName cho file data của nhân
			public long fileSize { get; set; }
			public string SHA256 { get; set; }
			public string systemFile { get; set; }

			// Các biến trạng thái C# tự bơm vào sau khi quét Registry / File .conf
			public bool isDownloaded { get; set; } = false;
			public List<AndroidInstance> instances { get; set; } = new List<AndroidInstance>();
		}

		public class AndroidInstance
		{
			public string id { get; set; }
			public string name { get; set; }
		}
	}
}
