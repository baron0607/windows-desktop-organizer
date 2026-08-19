using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Windows;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 映射目錄變更監聽器，當目錄檔案增刪改時觸發唯讀 UI 刷新事件（具備節流防抖機制）
    /// </summary>
    public class FolderPortalWatcher : IDisposable
    {
        private readonly string _folderPath;
        private FileSystemWatcher? _watcher;
        private Timer? _debounceTimer;
        private readonly Action _onChangedCallback;
        private bool _disposed = false;

        public FolderPortalWatcher(string folderPath, Action onChangedCallback)
        {
            _folderPath = folderPath;
            _onChangedCallback = onChangedCallback;
            Start();
        }

        private void Start()
        {
            if (string.IsNullOrWhiteSpace(_folderPath) || !Directory.Exists(_folderPath))
            {
                return;
            }

            try
            {
                _watcher = new FileSystemWatcher(_folderPath)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                _watcher.Created += OnFileSystemEvent;
                _watcher.Deleted += OnFileSystemEvent;
                _watcher.Renamed += OnFileSystemEvent;
                _watcher.Changed += OnFileSystemEvent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FolderPortalWatcher] 監聽目錄失敗 ({_folderPath}): {ex.Message}");
            }
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            // 防抖 300ms，避免複製大量檔案時頻繁觸發刷新
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    _onChangedCallback?.Invoke();
                });
            }, null, 300, Timeout.Infinite);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _debounceTimer?.Dispose();
                if (_watcher != null)
                {
                    _watcher.EnableRaisingEvents = false;
                    _watcher.Dispose();
                }
            }
        }
    }
}