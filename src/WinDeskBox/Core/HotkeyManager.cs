using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 使用 Windows 標準 RegisterHotKey API 管理全域快速鍵（安全、標準、100% 絕不觸發任何防毒軟體監控）
    /// </summary>
    public class HotkeyManager : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9001;

        // Modifiers
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;
        private const uint MOD_NOREPEAT = 0x4000;

        // VK_D = 0x44 ('D')
        private const uint VK_D = 0x44;

        private readonly HwndSource _hwndSource;
        private readonly IntPtr _hwnd;
        private readonly Action _onHotkeyPressed;
        private bool _isRegistered = false;

        public HotkeyManager(Action onHotkeyPressed)
        {
            _onHotkeyPressed = onHotkeyPressed;

            var parameters = new HwndSourceParameters("WinDeskBox_HotkeyMsgWindow")
            {
                WindowStyle = 0,
                Width = 0,
                Height = 0
            };
            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(HwndHook);
            _hwnd = _hwndSource.Handle;

            Register();
        }

        public void Register()
        {
            if (_isRegistered) return;

            // 註冊 Alt + D 快速鍵
            _isRegistered = RegisterHotKey(_hwnd, HOTKEY_ID, MOD_ALT | MOD_NOREPEAT, VK_D);
            if (!_isRegistered)
            {
                // 若 Alt + D 被其他程式佔用，嘗試註冊 Ctrl + Alt + D 作為備援
                _isRegistered = RegisterHotKey(_hwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_D);
            }
        }

        public void Unregister()
        {
            if (_isRegistered)
            {
                UnregisterHotKey(_hwnd, HOTKEY_ID);
                _isRegistered = false;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                Application.Current?.Dispatcher.BeginInvoke(() =>
                {
                    _onHotkeyPressed?.Invoke();
                });
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Unregister();
            _hwndSource.Dispose();
        }

        #region Native Win32 API
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        #endregion
    }
}