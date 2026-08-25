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

        private readonly HwndSource _hwndSource;
        private readonly IntPtr _hwnd;
        private readonly Action _onHotkeyPressed;
        private bool _isRegistered = false;

        private string _currentModifiers = "Alt";
        private string _currentKey = "D";

        public HotkeyManager(Action onHotkeyPressed, string modifiers = "Alt", string key = "D")
        {
            _onHotkeyPressed = onHotkeyPressed;
            _currentModifiers = modifiers;
            _currentKey = key;

            var parameters = new HwndSourceParameters("WinDeskBox_HotkeyMsgWindow")
            {
                WindowStyle = 0,
                Width = 0,
                Height = 0
            };
            _hwndSource = new HwndSource(parameters);
            _hwndSource.AddHook(HwndHook);
            _hwnd = _hwndSource.Handle;

            Register(_currentModifiers, _currentKey);
        }

        public bool UpdateHotkey(string modifiers, string key)
        {
            Unregister();
            _currentModifiers = modifiers;
            _currentKey = key;
            return Register(_currentModifiers, _currentKey);
        }

        public bool Register(string modifiersStr, string keyStr)
        {
            if (_isRegistered) return true;

            var (fsModifiers, vk) = ParseHotkey(modifiersStr, keyStr);
            if (vk == 0) return false;

            _isRegistered = RegisterHotKey(_hwnd, HOTKEY_ID, fsModifiers | MOD_NOREPEAT, vk);
            if (!_isRegistered)
            {
                // 若被佔用且修飾鍵為 Alt，嘗試加 Ctrl 作為自動備援
                if (fsModifiers == MOD_ALT)
                {
                    _isRegistered = RegisterHotKey(_hwnd, HOTKEY_ID, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, vk);
                }
            }

            return _isRegistered;
        }

        public void Unregister()
        {
            if (_isRegistered)
            {
                UnregisterHotKey(_hwnd, HOTKEY_ID);
                _isRegistered = false;
            }
        }

        public static (uint fsModifiers, uint vk) ParseHotkey(string modifiersStr, string keyStr)
        {
            uint mod = 0;
            if (modifiersStr.Contains("Alt", StringComparison.OrdinalIgnoreCase)) mod |= MOD_ALT;
            if (modifiersStr.Contains("Ctrl", StringComparison.OrdinalIgnoreCase) || modifiersStr.Contains("Control", StringComparison.OrdinalIgnoreCase)) mod |= MOD_CONTROL;
            if (modifiersStr.Contains("Shift", StringComparison.OrdinalIgnoreCase)) mod |= MOD_SHIFT;
            if (modifiersStr.Contains("Win", StringComparison.OrdinalIgnoreCase)) mod |= MOD_WIN;

            // 預設如果都沒勾選，給予 Alt
            if (mod == 0) mod = MOD_ALT;

            uint vk = 0x44; // 預設 'D'
            string k = keyStr.Trim().ToUpperInvariant();

            if (k.Length == 1)
            {
                char c = k[0];
                if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                {
                    vk = (uint)c;
                }
            }
            else if (k.StartsWith("F") && int.TryParse(k.Substring(1), out int fNum) && fNum >= 1 && fNum <= 12)
            {
                vk = (uint)(0x70 + (fNum - 1)); // VK_F1 ~ VK_F12 (0x70 - 0x7B)
            }
            else if (k == "SPACE")
            {
                vk = 0x20; // VK_SPACE
            }
            else if (k == "TAB")
            {
                vk = 0x09; // VK_TAB
            }
            else if (k == "TILDE" || k == "`" || k == "~")
            {
                vk = 0xC0; // VK_OEM_3
            }

            return (mod, vk);
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