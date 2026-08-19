using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 桌面底層鎖定管理器，負責將收納盒視窗鎖定在桌面桌布圖層，永不遮擋工作視窗並完美相容 Win+D
    /// </summary>
    public static class DesktopWorker
    {
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;

        /// <summary>
        /// 將指定的 WPF 視窗釘在桌面最底層（桌布層）
        /// </summary>
        public static void PinToDesktopBottom(Window window)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.EnsureHandle();

            // 1. 設定 ToolWindow 樣式，避免出現在 Alt+Tab 或工作列
            int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
            SetWindowLong(hWnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

            // 2. 將視窗層級置於最底層 (HWND_BOTTOM)
            SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

            // 3. 掛載 HwndSource 訊息攔截器，當視窗層級改變時強制維持在底部
            var source = HwndSource.FromHwnd(hWnd);
            source?.AddHook(WndProc);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WINDOWPOSCHANGING)
            {
                // 當系統嘗試調整視窗層級時（如 Win+D 觸發），確保保持在 HWND_BOTTOM
                WINDOWPOS wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);
                wp.hwndInsertAfter = HWND_BOTTOM;
                wp.flags &= ~SWP_NOACTIVATE;
                Marshal.StructureToPtr(wp, lParam, true);
            }
            return IntPtr.Zero;
        }

        #region Native Win32 API
        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        #endregion
    }
}