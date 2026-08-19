using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using WinDeskBox.Core;

namespace WinDeskBox.UI.Tray
{
    /// <summary>
    /// 純 Win32 Shell_NotifyIcon 系統匣管理器，原生整合自訂圖示與 WPF 現代化右鍵選單
    /// </summary>
    public class TrayIconManager : IDisposable
    {
        private const int WM_USER = 0x0400;
        private const int WM_TRAYICON = WM_USER + 101;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_LBUTTONDBLCLK = 0x0203;

        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;

        private const int IDI_APPLICATION = 32512;

        private readonly HwndSource _hwndSource;
        private readonly IntPtr _hwnd;
        private readonly ContextMenu _contextMenu;
        private readonly Action _onOpenSettings;
        private readonly Action _onAddNewBox;
        private readonly Action<bool> _onToggleLockAll;
        private readonly Action _onExit;

        private IntPtr _hTrayIcon = IntPtr.Zero;
        private bool _isAllLocked = false;
        private MenuItem? _lockMenuItem;

        public TrayIconManager(
            Action onOpenSettings,
            Action onAddNewBox,
            Action<bool> onToggleLockAll,
            Action onExit)
        {
            _onOpenSettings = onOpenSettings;
            _onAddNewBox = onAddNewBox;
            _onToggleLockAll = onToggleLockAll;
            _onExit = onExit;

            // 建立隱藏的訊息視窗以接收系統匣滑鼠點擊
            var parameters = new HwndSourceParameters("WinDeskBox_TrayMsgWindow")
            {
                WindowStyle = 0,
                Width = 0,
                Height = 0
            };
            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(HwndHook);
            _hwnd = _hwndSource.Handle;

            _contextMenu = new ContextMenu();
            InitializeMenu();

            AddTrayIcon();
        }

        private void InitializeMenu()
        {
            _contextMenu.Items.Clear();

            var titleItem = new MenuItem
            {
                Header = "📦 WinDeskBox 桌面收納箱",
                IsEnabled = false,
                FontWeight = FontWeights.Bold
            };
            _contextMenu.Items.Add(titleItem);
            _contextMenu.Items.Add(new Separator());

            var addBoxItem = new MenuItem { Header = "➕ 新增收納盒" };
            addBoxItem.Click += (s, e) => _onAddNewBox();
            _contextMenu.Items.Add(addBoxItem);

            _lockMenuItem = new MenuItem { Header = "🔒 鎖定所有收納盒位置", IsCheckable = true, IsChecked = _isAllLocked };
            _lockMenuItem.Click += (s, e) =>
            {
                _isAllLocked = !_isAllLocked;
                _lockMenuItem.IsChecked = _isAllLocked;
                _onToggleLockAll(_isAllLocked);
            };
            _contextMenu.Items.Add(_lockMenuItem);

            _contextMenu.Items.Add(new Separator());

            var organizeItem = new MenuItem { Header = "🧹 立即整理桌面散亂檔案" };
            organizeItem.Click += (s, e) =>
            {
                int count = SafetyManager.OrganizeDesktop("_桌面收納箱");
                MessageBox.Show($"已成功整理歸納 {count} 個檔案至「_桌面收納箱」目錄。", "整理完成", MessageBoxButton.OK, MessageBoxImage.Information);
            };
            _contextMenu.Items.Add(organizeItem);

            var restoreItem = new MenuItem { Header = "↩️ 一鍵還原檔案至桌面" };
            restoreItem.Click += (s, e) =>
            {
                if (MessageBox.Show("確定要將收納箱內的所有檔案還原回桌面嗎？", "確認還原", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    int count = SafetyManager.RestoreDesktop("_桌面收納箱");
                    MessageBox.Show($"已將 {count} 個檔案移回桌面。", "還原完成", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            _contextMenu.Items.Add(restoreItem);

            _contextMenu.Items.Add(new Separator());

            var settingsItem = new MenuItem { Header = "⚙️ 設定與管理" };
            settingsItem.Click += (s, e) => _onOpenSettings();
            _contextMenu.Items.Add(settingsItem);

            var exitItem = new MenuItem { Header = "❌ 結束程式" };
            exitItem.Click += (s, e) => _onExit();
            _contextMenu.Items.Add(exitItem);
        }

        private void AddTrayIcon()
        {
            _hTrayIcon = IntPtr.Zero;

            // 優先從當前執行檔擷取內嵌之 WinDeskBox 專屬高畫質圖示
            try
            {
                string exePath = Environment.ProcessPath ?? "";
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    ExtractIconEx(exePath, 0, out IntPtr hLarge, out IntPtr hSmall, 1);
                    _hTrayIcon = (hSmall != IntPtr.Zero) ? hSmall : hLarge;
                }
            }
            catch
            {
                // 忽略錯誤並降級回通用圖示
            }

            if (_hTrayIcon == IntPtr.Zero)
            {
                _hTrayIcon = LoadIcon(IntPtr.Zero, (IntPtr)IDI_APPLICATION);
            }

            var nid = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hTrayIcon,
                szTip = "WinDeskBox 桌面收納箱"
            };

            Shell_NotifyIcon(NIM_ADD, ref nid);
        }

        private void RemoveTrayIcon()
        {
            var nid = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _hwnd,
                uID = 1
            };

            Shell_NotifyIcon(NIM_DELETE, ref nid);

            if (_hTrayIcon != IntPtr.Zero)
            {
                DestroyIcon(_hTrayIcon);
                _hTrayIcon = IntPtr.Zero;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_TRAYICON)
            {
                int mouseMsg = lParam.ToInt32();
                if (mouseMsg == WM_RBUTTONUP)
                {
                    // 彈出現代化右鍵選單
                    SetForegroundWindow(_hwnd);
                    _contextMenu.IsOpen = true;
                    handled = true;
                }
                else if (mouseMsg == WM_LBUTTONDBLCLK)
                {
                    _onOpenSettings();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            RemoveTrayIcon();
            _hwndSource.Dispose();
        }

        #region Native Win32 API
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uTimeoutOrVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
            public Guid guidItem;
            public IntPtr hBalloonIcon;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx(string szFileName, int nIconIndex, out IntPtr phiconLarge, out IntPtr phiconSmall, uint nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        #endregion
    }
}