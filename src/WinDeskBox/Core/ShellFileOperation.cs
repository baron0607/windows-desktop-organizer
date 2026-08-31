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

        /// <summary>
        /// 將檔案或資料夾陣列放入 Windows 系統剪貼簿（支援 複製 或 剪下 語意）
        /// </summary>
        public static void SetClipboardFiles(string[] filePaths, bool isCut)
        {
            if (filePaths == null || filePaths.Length == 0) return;

            try
            {
                var dropList = new System.Collections.Specialized.StringCollection();
                dropList.AddRange(filePaths);

                var data = new System.Windows.DataObject();
                data.SetFileDropList(dropList);

                // 設置 Preferred DropEffect 區分 複製 (5 = DROPEFFECT_COPY) 或 剪下 (2 = DROPEFFECT_MOVE)
                byte[] moveEffect = new byte[] { (byte)(isCut ? 2 : 5), 0, 0, 0 };
                using var ms = new MemoryStream(moveEffect);
                data.SetData("Preferred DropEffect", ms);

                System.Windows.Clipboard.SetDataObject(data, true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShellFileOperation] 設置剪貼簿失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 從 Windows 系統剪貼簿讀取檔案清單與剪下/複製狀態
        /// </summary>
        public static (string[] files, bool isCut)? GetClipboardFiles()
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsFileDropList()) return null;

                var dropList = System.Windows.Clipboard.GetFileDropList();
                if (dropList == null || dropList.Count == 0) return null;

                var files = new string[dropList.Count];
                dropList.CopyTo(files, 0);

                bool isCut = false;
                var data = System.Windows.Clipboard.GetDataObject();
                if (data != null && data.GetDataPresent("Preferred DropEffect"))
                {
                    if (data.GetData("Preferred DropEffect") is MemoryStream ms)
                    {
                        byte[] bytes = ms.ToArray();
                        if (bytes.Length > 0 && bytes[0] == 2)
                        {
                            isCut = true;
                        }
                    }
                }
                return (files, isCut);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShellFileOperation] 讀取剪貼簿失敗: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 完整遞迴複製整個目錄及其所有子目錄與檔案（確保來源端完好無缺）
        /// </summary>
        public static void CopyDirectory(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) return;

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                string nextTargetSubDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectory(subDir.FullName, nextTargetSubDir);
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