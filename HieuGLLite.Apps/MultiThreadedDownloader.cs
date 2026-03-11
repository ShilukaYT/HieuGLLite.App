// MultiThreadedDownloader.cs
using System.ComponentModel;
using Downloader;

namespace HieuGLLite.Apps
{
	public class MultiThreadedDownloader : IDisposable
	{
		private DownloadService _downloader;
		private bool _disposed = false;

		// Thêm biến này để biết đang tải bước nào (DOWNLOADING_EXE / DOWNLOADING_ANDROID)
		public string CurrentStatusType { get; set; }

		public delegate void ProgressHandler(double percentage, string speed, string downloadedMB);
		public event ProgressHandler OnProgressChanged;
		public event EventHandler OnDownloadCompleted;
		public event EventHandler<string> OnDownloadFailed;
		public bool IsCancelled { get; private set; } = false;

		private DateTime _lastUpdate = DateTime.MinValue;

		public MultiThreadedDownloader()
		{
			var downloadOpt = new DownloadConfiguration()
			{
				ChunkCount = 8,
				ParallelDownload = true,
				ClearPackageOnCompletionWithFailure = true
			};

			_downloader = new DownloadService(downloadOpt);
			_downloader.DownloadProgressChanged += OnDownloadProgressChanged;
			_downloader.DownloadFileCompleted += OnDownloadFileCompleted;
		}

		// HÀM QUAN TRỌNG: Cầu nối để Resume từ Package cũ
		public async Task ResumeFromPackageAsync(DownloadPackage package)
		{
			if (_downloader != null)
			{
				// Thư viện Downloader dùng chung tên hàm này cho cả tải mới và Resume
				await _downloader.DownloadFileTaskAsync(package);
			}
		}

		public async Task StartDownloadAsync(string url, string savePath)
		{
			await _downloader.DownloadFileTaskAsync(url, savePath);
		}

		public void Pause()
		{
			if (_downloader.Status == DownloadStatus.Running) _downloader.Pause();
		}

		public void Resume()
		{
			if (_downloader.Status == DownloadStatus.Paused) _downloader.Resume();
		}

		public void Cancel()
		{
			IsCancelled = true;
			_downloader.CancelAsync();
		}

		public DownloadPackage GetCurrentPackage() => _downloader?.Package;

		public void Dispose()
		{
			if (!_disposed)
			{
				_downloader.DownloadProgressChanged -= OnDownloadProgressChanged;
				_downloader.DownloadFileCompleted -= OnDownloadFileCompleted;
				_downloader?.Dispose();
				_disposed = true;
				GC.SuppressFinalize(this);
			}
		}

		private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			if ((DateTime.Now - _lastUpdate).TotalMilliseconds < 150 && e.ProgressPercentage < 100) return;
			_lastUpdate = DateTime.Now;

			string sizeDisplay = $"{(e.ReceivedBytesSize / 1048576.0):0.00} / {(e.TotalBytesToReceive / 1048576.0):0.00} MB";
			string speedText = $"{(e.BytesPerSecondSpeed / 1048576.0):0.00} MB/s";

			OnProgressChanged?.Invoke(e.ProgressPercentage, speedText, sizeDisplay);
		}

		private void OnDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
			if (e.Error != null) OnDownloadFailed?.Invoke(this, e.Error.Message);
			else if (!e.Cancelled) OnDownloadCompleted?.Invoke(this, EventArgs.Empty);
		}
	}
}