using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 非同步系統檔案與資料夾圖示載入器，具備記憶體快取以防止重複 I/O 阻塞 UI
    /// </summary>
    public static class ShellIconLoader
    {
        private static readonly ConcurrentDictionary<string, ImageSource> IconCache = new(StringComparer.OrdinalIgnoreCase);
        private static ImageSource? _defaultFolderIcon;
        private static ImageSource? _defaultFileIcon;

        /// <summary>
        /// 非同步取得檔案或資料夾的高解析度系統圖示（自動命中快取）
        /// </summary>
        public static async Task<ImageSource> GetIconAsync(string fullPath, bool isDirectory)
        {
            string cacheKey = isDirectory ? "__DIR__" : Path.GetExtension(fullPath).ToLowerInvariant();
            if (string.IsNullOrEmpty(cacheKey)) cacheKey = "__FILE_NO_EXT__";

            // 特殊副檔名（.exe, .lnk, .ico, .url）每個檔案圖示可能不同，不共用副檔名快取
            bool isUniqueIcon = !isDirectory && (cacheKey == ".exe" || cacheKey == ".lnk" || cacheKey == ".ico" || cacheKey == ".url");
            if (isUniqueIcon)
            {
                cacheKey = fullPath;
            }

            if (IconCache.TryGetValue(cacheKey, out var cachedIcon))
            {
                return cachedIcon;
            }

            return await Task.Run(() =>
            {
                var icon = ExtractIcon(fullPath, isDirectory);
                if (icon != null)
                {
                    icon.Freeze(); // 凍結以便跨執行緒共用
                    IconCache[cacheKey] = icon;
                    return icon;
                }
                return isDirectory ? GetDefaultFolderIcon() : GetDefaultFileIcon();
            });
        }

        private static ImageSource? ExtractIcon(string path, bool isDirectory)
        {
            try
            {
                uint flags = SHGFI_ICON | SHGFI_LARGEICON;
                uint attributes = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    flags |= SHGFI_USEFILEATTRIBUTES;
                }

                SHFILEINFO shinfo = new();
                IntPtr hImg = SHGetFileInfo(path, attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                if (shinfo.hIcon != IntPtr.Zero)
                {
                    try
                    {
                        var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                            shinfo.hIcon,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        return bitmapSource;
                    }
                    finally
                    {
                        DestroyIcon(shinfo.hIcon);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShellIconLoader] 擷取圖示失敗 ({path}): {ex.Message}");
            }
            return null;
        }

        private static ImageSource GetDefaultFolderIcon()
        {
            if (_defaultFolderIcon != null) return _defaultFolderIcon;
            var ico = ExtractIcon(Environment.GetFolderPath(Environment.SpecialFolder.Windows), true);
            if (ico != null) { ico.Freeze(); _defaultFolderIcon = ico; return ico; }
            return new BitmapImage();
        }

        private static ImageSource GetDefaultFileIcon()
        {
            if (_defaultFileIcon != null) return _defaultFileIcon;
            var ico = ExtractIcon("dummy.txt", false);
            if (ico != null) { ico.Freeze(); _defaultFileIcon = ico; return ico; }
            return new BitmapImage();
        }

        #region Win32 API
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        #endregion
    }
}