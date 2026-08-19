using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;
using WinDeskBox.Config;

namespace WinDeskBox.Core
{
    /// <summary>
    /// 多螢幕與解析度自適應保護器，監聽螢幕變更並自動限制收納盒於可視區域內
    /// </summary>
    public static class ScreenBoundsGuard
    {
        public static event Action? DisplayChanged;

        static ScreenBoundsGuard()
        {
            try
            {
                SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScreenBoundsGuard] 註冊螢幕變更事件失敗: {ex.Message}");
            }
        }

        private static void OnDisplaySettingsChanged(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DisplayChanged?.Invoke();
            });
        }

        /// <summary>
        /// 檢查並校正收納盒座標，若超出目前所有螢幕的工作區範圍則自動平滑拉回
        /// </summary>
        public static void ClampToScreen(BoxConfig box)
        {
            double virtLeft = SystemParameters.VirtualScreenLeft;
            double virtTop = SystemParameters.VirtualScreenTop;
            double virtWidth = SystemParameters.VirtualScreenWidth;
            double virtHeight = SystemParameters.VirtualScreenHeight;

            // 若完全在螢幕外，重設回主螢幕預設可視範圍
            if (box.Left + box.Width < virtLeft || box.Left > virtLeft + virtWidth ||
                box.Top + box.Height < virtTop || box.Top > virtTop + virtHeight)
            {
                box.Left = Math.Max(20, SystemParameters.WorkArea.Left + 40);
                box.Top = Math.Max(20, SystemParameters.WorkArea.Top + 40);
            }
            else
            {
                // 限制在邊界內
                if (box.Left < virtLeft) box.Left = virtLeft;
                if (box.Top < virtTop) box.Top = virtTop;
                if (box.Left + box.Width > virtLeft + virtWidth)
                {
                    box.Left = (virtLeft + virtWidth) - box.Width;
                }
                if (box.Top + box.Height > virtTop + virtHeight)
                {
                    box.Top = (virtTop + virtHeight) - box.Height;
                }
            }
        }
    }
}