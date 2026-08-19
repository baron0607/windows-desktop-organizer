using System;
using System.Collections.Generic;
using System.IO;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 桌面整理安全管理器，負責白名單過濾、同名防覆蓋自動重命名與一鍵安全還原
    /// </summary>
    public static class SafetyManager
    {
        // 絕對不移動之白名單檔案與特殊系統項目
        private static readonly HashSet<string> WhitelistFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "desktop.ini",
            "thumbs.db",
            "WinDeskBox.exe",
            "config.json"
        };

        /// <summary>
        /// 判斷檔案是否為受保護之白名單項目
        /// </summary>
        public static bool IsWhitelisted(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            if (WhitelistFileNames.Contains(fileName)) return true;
            if (fileName.StartsWith("~$", StringComparison.OrdinalIgnoreCase)) return true; // Office 暫存檔
            return false;
        }

        /// <summary>
        /// 產生不重複的安全目標路徑（若已有同名檔案，自動採用 檔名 (1).ext 格式）
        /// </summary>
        public static string GetSafeDestinationPath(string destinationDirectory, string sourceFileName)
        {
            string baseName = Path.GetFileNameWithoutExtension(sourceFileName);
            string extension = Path.GetExtension(sourceFileName);
            string targetPath = Path.Combine(destinationDirectory, sourceFileName);

            int counter = 1;
            while (File.Exists(targetPath) || Directory.Exists(targetPath))
            {
                string newName = $"{baseName} ({counter}){extension}";
                targetPath = Path.Combine(destinationDirectory, newName);
                counter++;
            }

            return targetPath;
        }

        /// <summary>
        /// 執行手動一鍵整理：將桌面散亂檔案依分類移入 _桌面收納箱/ 子目錄
        /// </summary>
        public static int OrganizeDesktop(string organizeDirName)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string targetRoot = Path.Combine(desktop, organizeDirName);
            Directory.CreateDirectory(targetRoot);

            int movedCount = 0;
            var files = Directory.GetFiles(desktop);

            foreach (var file in files)
            {
                if (IsWhitelisted(file)) continue;

                string category = FileClassifier.GetCategory(file);
                string categoryDir = Path.Combine(targetRoot, category);
                Directory.CreateDirectory(categoryDir);

                string safeDest = GetSafeDestinationPath(categoryDir, Path.GetFileName(file));
                try
                {
                    File.Move(file, safeDest);
                    movedCount++;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SafetyManager] 移動檔案失敗 ({file}): {ex.Message}");
                }
            }

            return movedCount;
        }

        /// <summary>
        /// 一鍵安全還原：將 _桌面收納箱/ 內的所有檔案批次移回桌面根目錄
        /// </summary>
        public static int RestoreDesktop(string organizeDirName)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string targetRoot = Path.Combine(desktop, organizeDirName);

            if (!Directory.Exists(targetRoot)) return 0;

            int restoredCount = 0;
            var subDirs = Directory.GetDirectories(targetRoot);

            foreach (var subDir in subDirs)
            {
                var files = Directory.GetFiles(subDir);
                foreach (var file in files)
                {
                    string safeDest = GetSafeDestinationPath(desktop, Path.GetFileName(file));
                    try
                    {
                        File.Move(file, safeDest);
                        restoredCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SafetyManager] 還原檔案失敗 ({file}): {ex.Message}");
                    }
                }
            }

            return restoredCount;
        }
    }
}