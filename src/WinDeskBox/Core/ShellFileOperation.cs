using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WinDeskBox.Core
{
    /// <summary>
    /// Windows 原生 Shell 檔案操作封裝，支援安全刪除至資源回收筒
    /// </summary>
    public static class ShellFileOperation
    {
        private const int FO_DELETE = 0x0003;
        private const int FOF_ALLOWUNDO = 0x0040;
        private const int FOF_NOCONFIRMATION = 0x0010;
        private const int FOF_SILENT = 0x0004;

        /// <summary>
        /// 將指定的檔案或資料夾安全移入「資源回收筒」
        /// </summary>
        public static bool MoveToRecycleBin(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return false;
            }

            try
            {
                SHFILEOPSTRUCT shf = new SHFILEOPSTRUCT
                {
                    wFunc = FO_DELETE,
                    pFrom = path + '\0' + '\0', // 必須以雙 Null 結尾
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT
                };

                int result = SHFileOperationW(ref shf);
                return result == 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShellFileOperation] 移至回收筒失敗 ({path}): {ex.Message}");
                return false;
            }
        }

        #region Native Win32 API
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public int wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperationW(ref SHFILEOPSTRUCT lpFileOp);
        #endregion
    }
}