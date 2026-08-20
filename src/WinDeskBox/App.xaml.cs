using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using WinDeskBox.Config;
using WinDeskBox.Core;
using WinDeskBox.UI.Controls;
using WinDeskBox.UI.Tray;
using WinDeskBox.UI.Windows;

namespace WinDeskBox
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private AppConfig _config = new();
        public AppConfig Config => _config;
        private readonly List<FenceView> _fenceViews = new();
        private TrayIconManager? _trayManager;
        private bool _isDesktopHidden = false;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. 單一執行個體互斥鎖 (Single Instance Mutex)
            const string mutexName = "Global\\WinDeskBox_SingleInstance_Mutex";
            _mutex = new Mutex(true, mutexName, out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("WinDeskBox 桌面收納箱已在背景執行中！\n請由工作列右下角系統匣圖示開啟或管理。", "WinDeskBox 提示", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // 2. 載入本機設定
            _config = ConfigManager.Load();

            // 3. 啟動所有桌面收納盒視窗
            LaunchAllFences();

            // 4. 安裝桌面雙擊全域滑鼠 Hook
            if (_config.General.DoubleClickHideDesktop)
            {
                GlobalHook.DesktopDoubleClicked += OnDesktopDoubleClicked;
                GlobalHook.Install();
            }

            // 5. 註冊多螢幕與解析度自適應防護
            ScreenBoundsGuard.DisplayChanged += OnDisplayChanged;

            // 6. 初始化系統匣圖示
            _trayManager = new TrayIconManager(
                onOpenSettings: OpenSettingsWindow,
                onAddNewBox: AddNewBox,
                onToggleLockAll: ToggleLockAllBoxes,
                onExit: ExitApplication
            );
        }

        private void LaunchAllFences()
        {
            // 關閉既有視窗
            foreach (var view in _fenceViews)
            {
                view.Close();
            }
            _fenceViews.Clear();

            // 根據設定開啟各收納盒
            foreach (var boxConfig in _config.Boxes)
            {
                var fenceView = new FenceView(boxConfig);
                fenceView.Show();
                _fenceViews.Add(fenceView);
            }
        }

        private void OnDesktopDoubleClicked()
        {
            _isDesktopHidden = !_isDesktopHidden;
            foreach (var fence in _fenceViews)
            {
                fence.Visibility = _isDesktopHidden ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void OnDisplayChanged()
        {
            foreach (var fence in _fenceViews)
            {
                ScreenBoundsGuard.ClampToScreen(fence.Config);
                fence.Left = fence.Config.Left;
                fence.Top = fence.Config.Top;
            }
        }

        public void ReloadAllFences()
        {
            _config = ConfigManager.Load();
            LaunchAllFences();
        }

        public void OpenSettingsForBox(string? boxId = null)
        {
            var settings = new SettingsWindow(_config, boxId);
            if (settings.ShowDialog() == true)
            {
                ReloadAllFences();
            }
        }

        private void OpenSettingsWindow()
        {
            OpenSettingsForBox(null);
        }

        private void AddNewBox()
        {
            var newBox = new BoxConfig
            {
                Title = $"收納盒 {_config.Boxes.Count + 1}",
                Left = 150 + (_config.Boxes.Count * 25),
                Top = 150 + (_config.Boxes.Count * 25),
                Width = 340,
                Height = 280
            };
            newBox.Tabs.Add(new TabConfig
            {
                Name = "新分頁",
                FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            });

            _config.Boxes.Add(newBox);
            ConfigManager.Save(_config);

            var fenceView = new FenceView(newBox);
            fenceView.Show();
            _fenceViews.Add(fenceView);
        }

        private void ToggleLockAllBoxes(bool locked)
        {
            _config.General.GlobalLockPositions = locked;
            foreach (var fence in _fenceViews)
            {
                fence.UpdateLockState(locked);
            }
            ConfigManager.Save(_config);
        }

        private void ExitApplication()
        {
            // 儲存目前視窗位置
            foreach (var fence in _fenceViews)
            {
                fence.Config.Left = fence.Left;
                fence.Config.Top = fence.Top;
                fence.Config.Width = fence.Width;
                fence.Config.Height = fence.Height;
            }
            ConfigManager.Save(_config);

            GlobalHook.Uninstall();
            _trayManager?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            GlobalHook.Uninstall();
            _trayManager?.Dispose();
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}