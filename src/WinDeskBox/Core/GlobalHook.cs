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
        public static event Action<int, int>? DesktopDoubleClicked;

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const uint GA_ROOT = 2;

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

                if ((currentTime - _lastClickTime) <= doubleClickTime && dx <= 6 && dy <= 6)
                {
                    int clickX = hookStruct.pt.x;
                    int clickY = hookStruct.pt.y;

                    // 檢查點擊處是否為「純桌面空白處」（完全不跨進程發送指針消息，100% 防崩潰防黑屏，精準排除 Word/Excel 圖示）
                    if (IsBlankDesktopArea(hookStruct.pt))
                    {
                        Application.Current?.Dispatcher.BeginInvoke(() =>
                        {
                            DesktopDoubleClicked?.Invoke(clickX, clickY);
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

        /// <summary>
        /// 嚴格且安全地判斷點擊位置是否為「純桌面空白處」：
        /// 1. 排除本程式視窗 (WinDeskBox)
        /// 2. 排除檔案總管視窗 (CabinetWClass) 與任何一般應用程式視窗
        /// 3. 透過無崩潰的 Windows Accessibility COM 介面判斷是否命中檔案圖示 (Word/Excel/捷徑)
        /// </summary>
        private static bool IsBlankDesktopArea(POINT screenPt)
        {
            IntPtr targetHwnd = WindowFromPoint(screenPt);
            if (targetHwnd == IntPtr.Zero) return false;

            // 1. 檢查是否屬於本程式自身的視窗
            GetWindowThreadProcessId(targetHwnd, out uint pid);
            if (pid == (uint)Environment.ProcessId) return false;

            // 2. 檢查頂層根視窗 (Root Window)，必須是桌面 (Progman 或 WorkerW)
            IntPtr rootWnd = GetAncestor(targetHwnd, GA_ROOT);
            if (rootWnd == IntPtr.Zero) rootWnd = targetHwnd;

            StringBuilder rootClassName = new StringBuilder(256);
            GetClassName(rootWnd, rootClassName, rootClassName.Capacity);
            string rootClass = rootClassName.ToString();

            if (rootClass != "Progman" && rootClass != "WorkerW")
            {
                // 點擊在一般應用程式或檔案總管 (CabinetWClass) 資料夾視窗上，一律不觸發
                return false;
            }

            // 3. 透過安全無崩潰的 Windows Accessibility API (oleacc.dll) 檢查游標下是否為檔案/捷徑圖示
            try
            {
                int hr = AccessibleObjectFromPoint(screenPt, out IAccessible? acc, out object? varChild);
                if (hr == 0 && acc != null && varChild != null)
                {
                    object role = acc.get_accRole(varChild);
                    if (role is int roleInt)
                    {
                        // ROLE_SYSTEM_LISTITEM = 34 (0x22) 代表點擊在桌面檔案/捷徑圖示上 (Word, Excel, 資料夾等)
                        // ROLE_SYSTEM_TEXT = 42 (0x2A) 代表點擊在圖示文字標籤上
                        if (roleInt == 34 || roleInt == 42)
                        {
                            return false; // 正在雙擊開啟桌面檔案，絕對不觸發收納盒隱藏！
                        }
                    }
                }
            }
            catch
            {
                // 若 Accessibility 查詢發生異常，安全忽略
            }

            return true;
        }

        #region Native Win32 API & Accessibility COM
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

        [ComImport]
        [Guid("618736E0-3C3D-11CF-810C-00AA00389B71")]
        [InterfaceType(ComInterfaceType.InterfaceIsDual)]
        private interface IAccessible
        {
            [DispId(-5000)] object get_accParent { get; }
            [DispId(-5001)] int get_accChildCount { get; }
            [DispId(-5002)] object get_accChild([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5003)] string get_accName([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5004)] string get_accValue([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5005)] string get_accDescription([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5006)] object get_accRole([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5007)] object get_accState([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5008)] string get_accHelp([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5009)] int get_accHelpTopic([Out, MarshalAs(UnmanagedType.BStr)] out string pszHelpFile, [In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5010)] string get_accKeyboardShortcut([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5011)] object get_accFocus { get; }
            [DispId(-5012)] object get_accSelection { get; }
            [DispId(-5013)] string get_accDefaultAction([In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5014)] void accSelect([In] int flagsSelect, [In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5015)] void accLocation([Out] out int pxLeft, [Out] out int pyTop, [Out] out int pcxWidth, [Out] out int pcyHeight, [In, MarshalAs(UnmanagedType.Struct)] object varChild);
            [DispId(-5016)] object accNavigate([In] int navDir, [In, MarshalAs(UnmanagedType.Struct)] object varStart);
            [DispId(-5017)] object accHitTest([In] int xLeft, [In] int yTop);
            [DispId(-5018)] void accDoDefaultAction([In, MarshalAs(UnmanagedType.Struct)] object varChild);
        }

        [DllImport("oleacc.dll")]
        private static extern int AccessibleObjectFromPoint(POINT pt, [Out, MarshalAs(UnmanagedType.Interface)] out IAccessible? ppacc, [Out, MarshalAs(UnmanagedType.Struct)] out object? pvarChild);

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

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        #endregion
    }
}