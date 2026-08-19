using System;
using System.Collections.Generic;
using System.IO;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 副檔名分類規則引擎，負責將桌面散亂檔案精準歸類至對應之分類資料夾
    /// </summary>
    public static class FileClassifier
    {
        private static readonly Dictionary<string, string> ExtensionCategoryMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // 常用文件
            { ".pdf", "文件" }, { ".doc", "文件" }, { ".docx", "文件" },
            { ".xls", "文件" }, { ".xlsx", "文件" }, { ".csv", "文件" },
            { ".ppt", "文件" }, { ".pptx", "文件" }, { ".txt", "文件" },
            { ".md", "文件" }, { ".rtf", "文件" }, { ".epub", "文件" },

            // 圖片相片
            { ".jpg", "圖片" }, { ".jpeg", "圖片" }, { ".png", "圖片" },
            { ".gif", "圖片" }, { ".webp", "圖片" }, { ".bmp", "圖片" },
            { ".svg", "圖片" }, { ".psd", "圖片" }, { ".ai", "圖片" },
            { ".ico", "圖片" }, { ".raw", "圖片" }, { ".tif", "圖片" },

            // 壓縮與安裝包
            { ".zip", "壓縮與安裝檔" }, { ".rar", "壓縮與安裝檔" }, { ".7z", "壓縮與安裝檔" },
            { ".tar", "壓縮與安裝檔" }, { ".gz", "壓縮與安裝檔" }, { ".exe", "壓縮與安裝檔" },
            { ".msi", "壓縮與安裝檔" }, { ".iso", "壓縮與安裝檔" }, { ".dmg", "壓縮與安裝檔" },

            // 程式捷徑
            { ".lnk", "捷徑" }, { ".url", "捷徑" },

            // 影音娛樂
            { ".mp4", "影音" }, { ".mkv", "影音" }, { ".avi", "影音" },
            { ".mov", "影音" }, { ".wmv", "影音" }, { ".flv", "影音" },
            { ".mp3", "影音" }, { ".wav", "影音" }, { ".flac", "影音" },
            { ".m4a", "影音" }, { ".aac", "影音" }
        };

        /// <summary>
        /// 依據檔案副檔名取得歸屬之分類資料夾名稱
        /// </summary>
        public static string GetCategory(string filePath)
        {
            string ext = Path.GetExtension(filePath);
            if (ExtensionCategoryMap.TryGetValue(ext, out var category))
            {
                return category;
            }
            return "其他";
        }
    }
}