using System;
using System.Collections.Generic;
using System.IO;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 資安防護守衛，嚴格限制所有檔案操作的授權邊界，防止 Path Traversal 與系統敏感目錄毀損
    /// </summary>
    public static class SecurityGuard
    {
        // 系統核心保護目錄黑名單（絕對禁止映射或操作）
        private static readonly HashSet<string> SystemBlacklistedPaths = new(StringComparer.OrdinalIgnoreCase);

        static SecurityGuard()
        {
            InitializeBlacklist();
        }

        private static void InitializeBlacklist()
        {
            AddPathToBlacklist(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            AddPathToBlacklist(Environment.GetFolderPath(Environment.SpecialFolder.System));
            AddPathToBlacklist(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86));
            AddPathToBlacklist(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            AddPathToBlacklist(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
            AddPathToBlacklist(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles));
            AddPathToBlacklist(Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86));
            
            // 系統磁碟根目錄保護（如 C:\ 根目錄不可作為直接映射區）
            string sysDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            AddPathToBlacklist(sysDrive);
        }

        private static void AddPathToBlacklist(string? path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try
                {
                    string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    SystemBlacklistedPaths.Add(fullPath);
                }
                catch
                {
                    // 忽略無效路徑
                }
            }
        }

        /// <summary>
        /// 驗證指定資料夾路徑是否安全合法（非系統核心黑名單目錄、未遭受 Path Traversal 竄改）
        /// </summary>
        /// <param name="folderPath">要驗證的資料夾路徑</param>
        /// <param name="sanitizedPath">正規化後的乾淨完整路徑</param>
        /// <returns>若合法安全則傳回 true，否則傳回 false</returns>
        public static bool ValidateFolderPath(string folderPath, out string sanitizedPath)
        {
            sanitizedPath = string.Empty;
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            try
            {
                // 1. 路徑正規化（消除 ..\ 與相對符號，徹底防禦 Path Traversal）
                string fullPath = Path.GetFullPath(folderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                // 2. 檢查是否命中系統黑名單
                if (SystemBlacklistedPaths.Contains(fullPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[SecurityGuard] 阻斷嘗試映射系統核心目錄: {fullPath}");
                    return false;
                }

                // 3. 檢查是否為系統黑名單目錄的直接父子級關鍵路徑
                foreach (var blacklisted in SystemBlacklistedPaths)
                {
                    if (fullPath.Equals(blacklisted, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                sanitizedPath = fullPath;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SecurityGuard] 路徑檢驗異常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 驗證單一檔案操作是否在已授權的目錄邊界之內
        /// </summary>
        public static bool IsFileWithinAuthorizedScope(string filePath, string authorizedFolder)
        {
            try
            {
                string fullFilePath = Path.GetFullPath(filePath);
                string fullFolderPath = Path.GetFullPath(authorizedFolder);

                if (!fullFolderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    fullFolderPath += Path.DirectorySeparatorChar;
                }

                return fullFilePath.StartsWith(fullFolderPath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}