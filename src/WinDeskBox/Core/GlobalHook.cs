using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 全域滑鼠低階 Hook 監聽器，精準偵測桌面空白處之雙擊事件以切換收納盒隱藏/顯示
    /// </summary>
    public static class GlobalHook
    {
        public static event Action? DesktopDoubleClicked;

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        private static LowLevelMouseProc? _proc;
        private static IntPtr _hookId = IntPtr.Zero;
        private static uint _lastClickTime = 0;
        private static POINT _lastClickPos;

        public static void Install()
        {
            if (_hookId != IntPtr.Zero) return;

            try
            {
                _proc = HookCallback;
                using var curProcess = Process.GetCurrentProcess();
                using var curModule = curProcess.MainModule;
                if (curModule != null)
                {
                    _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GlobalHook] 安裝滑鼠 Hook 失敗: {ex.Message}");
            }
        }

        public static void Uninstall()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _proc = null;
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                uint currentTime = (uint)Environment.TickCount;
                uint doubleClickTime = GetDoubleClickTime();

                int dx = Math.Abs(hookStruct.pt.x - _lastClickPos.x);
                int dy = Math.Abs(hookStruct.pt.y - _lastClickPos.y);

                if ((currentTime - _lastClickTime) <= doubleClickTime && dx <= 4 && dy <= 4)
                {
                    // 檢查點擊處底下的視窗是否為桌面 (WorkerW / Progman / SysListView32)
                    IntPtr targetHwnd = WindowFromPoint(hookStruct.pt);
                    if (IsDesktopWindow(targetHwnd))
                    {
                        Application.Current?.Dispatcher.BeginInvoke(() =>
                        {
                            DesktopDoubleClicked?.Invoke();
                        });
                    }
                    _lastClickTime = 0; // 重設避免連續三擊誤判
                }
                else
                {
                    _lastClickTime = currentTime;
                    _lastClickPos = hookStruct.pt;
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static bool IsDesktopWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return false;

            StringBuilder className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            string name = className.ToString();

            if (name == "Progman" || name == "WorkerW" || name == "SysListView32" || name == "SHELLDLL_DefView")
            {
                return true;
            }

            // 往上檢查父視窗
            IntPtr parent = GetParent(hWnd);
            if (parent != IntPtr.Zero)
            {
                GetClassName(parent, className, className.Capacity);
                string parentName = className.ToString();
                if (parentName == "Progman" || parentName == "WorkerW" || parentName == "SHELLDLL_DefView")
                {
                    return true;
                }
            }

            return false;
        }

        #region Native Win32 API
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern uint GetDoubleClickTime();

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetParent(IntPtr hWnd);
        #endregion
    }
}