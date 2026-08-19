using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 開機自動啟動管理器，透過使用者個人 Startup 啟動區捷徑實現免安裝綠色自啟
    /// </summary>
    public static class StartupManager
    {
        private static readonly string ShortcutName = "WinDeskBox.lnk";

        private static string StartupFolderPath =>
            Environment.GetFolderPath(Environment.SpecialFolder.Startup);

        private static string ShortcutPath =>
            Path.Combine(StartupFolderPath, ShortcutName);

        private static string CurrentExePath =>
            Environment.ProcessPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WinDeskBox.exe");

        /// <summary>
        /// 檢查目前是否已啟用開機自動啟動
        /// </summary>
        public static bool IsStartupEnabled()
        {
            try
            {
                if (File.Exists(ShortcutPath))
                {
                    // 捷徑存在即視為已啟用
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StartupManager] 檢查啟動捷徑失敗: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// 設定開機自動啟動狀態
        /// </summary>
        /// <param name="enable">true 為啟用開機自啟，false 為關閉</param>
        public static bool SetStartup(bool enable)
        {
            try
            {
                if (enable)
                {
                    CreateShortcut(ShortcutPath, CurrentExePath, "--startup");
                    return true;
                }
                else
                {
                    if (File.Exists(ShortcutPath))
                    {
                        File.Delete(ShortcutPath);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StartupManager] 設定開機自啟失敗: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 透過原生 COM IShellLink 介面建立 Windows 捷徑
        /// </summary>
        private static void CreateShortcut(string shortcutPath, string targetPath, string arguments)
        {
            IShellLinkW link = (IShellLinkW)new ShellLink();
            link.SetPath(targetPath);
            link.SetArguments(arguments);
            link.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);
            link.SetDescription("WinDeskBox 桌面收納箱");

            IPersistFile file = (IPersistFile)link;
            file.Save(shortcutPath, false);
        }

        #region Native COM Interop for Shortcut Creation
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }
        #endregion
    }
}