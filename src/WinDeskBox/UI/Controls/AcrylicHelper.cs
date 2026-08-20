using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WinDeskBox.UI.Controls
{
    /// <summary>
    /// Windows 11 DWM Acrylic / Mica 毛玻璃效果與圓角視窗 API 封裝器
    /// </summary>
    public static class AcrylicHelper
    {
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        // DWM_WINDOW_CORNER_PREFERENCE
        private const int DWMWCP_ROUND = 2;

        // DWM_SYSTEMBACKDROP_TYPE
        private const int DWMSBT_NONE = 1;
        private const int DWMSBT_MAINWINDOW = 2; // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3; // Acrylic
        private const int DWMSBT_TABBEDWINDOW = 4; // Mica Alt

        /// <summary>
        /// 為指定視窗套用 Windows 11 Fluent Acrylic 毛玻璃效果與圓角
        /// </summary>
        public static void EnableAcrylic(Window window, bool isDarkMode = false)
        {
            var helper = new WindowInteropHelper(window);
            IntPtr hWnd = helper.EnsureHandle();

            if (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000)
            {
                try
                {
                    // 1. 啟用圓角
                    int cornerPreference = DWMWCP_ROUND;
                    DwmSetWindowAttribute(hWnd, DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPreference, sizeof(int));

                    // 2. 深淺色主題適配
                    int darkMode = isDarkMode ? 1 : 0;
                    DwmSetWindowAttribute(hWnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                    // 3. 不套用 DWM 系統著色材質，避免桌布色調滲透，由 WPF 硬體 Alpha 渲染純粹透明中性灰
                    int backdropType = DWMSBT_NONE;
                    DwmSetWindowAttribute(hWnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdropType, sizeof(int));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AcrylicHelper] 套用 DWM 特效失敗: {ex.Message}");
                }
            }
        }

        #region Native DWM API
        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        #endregion
    }
}