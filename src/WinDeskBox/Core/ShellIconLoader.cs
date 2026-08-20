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
    /// 高畫質系統圖示擷取器（採用 Windows Shell JUMBO 256x256 / ExtraLarge 高解析度圖示清單，支援高 DPI 與平滑抗鋸齒）
    /// </summary>
    public static class ShellIconLoader
    {
        private static readonly ConcurrentDictionary<string, ImageSource> IconCache = new(StringComparer.OrdinalIgnoreCase);
        private static ImageSource? _defaultFolderIcon;
        private static ImageSource? _defaultFileIcon;

        private static readonly Guid IID_IImageList = new("46EB5926-582E-4017-9FDF-E8998DAA0950");
        private const int SHIL_JUMBO = 0x4;       // 256x256 超高畫質圖示
        private const int SHIL_EXTRALARGE = 0x2;  // 48x48 特大圖示
        private const int ILD_TRANSPARENT = 0x00000001;

        /// <summary>
        /// 非同步取得檔案或資料夾的高解析度系統圖示（自動命中快取）
        /// </summary>
        public static async Task<ImageSource> GetIconAsync(string fullPath, bool isDirectory)
        {
            string cacheKey = isDirectory ? "__DIR__" : Path.GetExtension(fullPath).ToLowerInvariant();
            if (string.IsNullOrEmpty(cacheKey)) cacheKey = "__FILE_NO_EXT__";

            // 特殊副檔名（.exe, .lnk, .ico, .url）個別檔案可能有專屬內嵌圖示，依路徑個別快取
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
                var icon = ExtractHighQualityIcon(fullPath, isDirectory);
                if (icon != null)
                {
                    icon.Freeze(); // 凍結以便跨執行緒共用
                    IconCache[cacheKey] = icon;
                    return icon;
                }
                return isDirectory ? GetDefaultFolderIcon() : GetDefaultFileIcon();
            });
        }

        /// <summary>
        /// 透過 Windows Shell IImageList 取得 256x256 / 48x48 高畫質原生圖示
        /// </summary>
        private static ImageSource? ExtractHighQualityIcon(string path, bool isDirectory)
        {
            try
            {
                uint flags = SHGFI_SYSICONINDEX;
                uint attributes = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;

                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    flags |= SHGFI_USEFILEATTRIBUTES;
                }

                SHFILEINFO shinfo = new();
                IntPtr res = SHGetFileInfo(path, attributes, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                if (res != IntPtr.Zero)
                {
                    int iconIndex = shinfo.iIcon;
                    IntPtr hIcon = IntPtr.Zero;

                    // 1. 優先嘗試擷取 256x256 JUMBO 超高畫質圖示
                    Guid iid = IID_IImageList;
                    int hr = SHGetImageList(SHIL_JUMBO, ref iid, out IImageList? imageList);
                    if (hr == 0 && imageList != null)
                    {
                        imageList.GetIcon(iconIndex, ILD_TRANSPARENT, out hIcon);
                    }

                    // 2. 若 JUMBO 不可用，降級嘗試 48x48 Extra Large 特大圖示
                    if (hIcon == IntPtr.Zero)
                    {
                        Guid iid2 = IID_IImageList;
                        hr = SHGetImageList(SHIL_EXTRALARGE, ref iid2, out imageList);
                        if (hr == 0 && imageList != null)
                        {
                            imageList.GetIcon(iconIndex, ILD_TRANSPARENT, out hIcon);
                        }
                    }

                    // 3. 轉換為 WPF BitmapSource
                    if (hIcon != IntPtr.Zero)
                    {
                        try
                        {
                            var bitmapSource = Imaging.CreateBitmapSourceFromHIcon(
                                hIcon,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                            return bitmapSource;
                        }
                        finally
                        {
                            DestroyIcon(hIcon);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ShellIconLoader] 擷取高畫質圖示失敗 ({path}): {ex.Message}");
            }
            return null;
        }

        private static ImageSource GetDefaultFolderIcon()
        {
            if (_defaultFolderIcon != null) return _defaultFolderIcon;
            var ico = ExtractHighQualityIcon(Environment.GetFolderPath(Environment.SpecialFolder.Windows), true);
            if (ico != null) { ico.Freeze(); _defaultFolderIcon = ico; return ico; }
            return new BitmapImage();
        }

        private static ImageSource GetDefaultFileIcon()
        {
            if (_defaultFileIcon != null) return _defaultFileIcon;
            var ico = ExtractHighQualityIcon("dummy.txt", false);
            if (ico != null) { ico.Freeze(); _defaultFileIcon = ico; return ico; }
            return new BitmapImage();
        }

        #region Native Win32 API & COM Interfaces
        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_SYSICONINDEX = 0x000004000;
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

        [DllImport("shell32.dll", EntryPoint = "#727")]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig] int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);
            [PreserveSig] int ReplaceIcon(int i, IntPtr hicon, ref int pi);
            [PreserveSig] int SetOverlayImage(int iImage, int iOverlay);
            [PreserveSig] int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);
            [PreserveSig] int AddMasked(IntPtr hbmImage, int crMask, ref int pi);
            [PreserveSig] int Draw(IntPtr pimldp);
            [PreserveSig] int Remove(int i);
            [PreserveSig] int GetIcon(int i, int flags, out IntPtr picon);
        }
        #endregion
    }
}