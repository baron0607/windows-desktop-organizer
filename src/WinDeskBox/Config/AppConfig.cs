using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace WinDeskBox.Config
{
    /// <summary>
    /// 收納盒內單一分頁 (Tab) 的組態模型
    /// </summary>
    public class TabConfig
    {
        /// <summary>
        /// 分頁標題名稱
        /// </summary>
        public string Name { get; set; } = "預設分頁";

        /// <summary>
        /// 映射之實體本機資料夾路徑（例如 D:\forest\桌面區\執行中）
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 檢視模式：Grid (大圖示網格) 或 List (詳細清單)
        /// </summary>
        public string ViewMode { get; set; } = "Grid";

        /// <summary>
        /// 排序依據：Name (名稱), ModifiedTime (修改時間), Extension (副檔名)
        /// </summary>
        public string SortBy { get; set; } = "ModifiedTime";

        /// <summary>
        /// 是否遞增排序（預設 false，修改時間最新者在最前）
        /// </summary>
        public bool SortAscending { get; set; } = false;
    }

    /// <summary>
    /// 單一桌面收納盒 (Fence / Box) 的設定模型
    /// </summary>
    public class BoxConfig
    {
        /// <summary>
        /// 收納盒唯一識別碼
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 收納盒標題
        /// </summary>
        public string Title { get; set; } = "收納盒";

        /// <summary>
        /// 所包含之資料夾映射分頁清單
        /// </summary>
        public List<TabConfig> Tabs { get; set; } = new List<TabConfig>();

        /// <summary>
        /// 當前啟用的分頁索引
        /// </summary>
        public int ActiveTabIndex { get; set; } = 0;

        /// <summary>
        /// 桌面視窗 X 座標
        /// </summary>
        public double Left { get; set; } = 100;

        /// <summary>
        /// 桌面視窗 Y 座標
        /// </summary>
        public double Top { get; set; } = 100;

        /// <summary>
        /// 視窗寬度
        /// </summary>
        public double Width { get; set; } = 340;

        /// <summary>
        /// 視窗高度
        /// </summary>
        public double Height { get; set; } = 280;

        /// <summary>
        /// 背景毛玻璃不透明度 (0.1 ~ 1.0)
        /// </summary>
        public double Opacity { get; set; } = 0.75;

        /// <summary>
        /// 是否鎖定收納盒位置（鎖定時禁止拖動與縮放視窗）
        /// </summary>
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// 是否啟用隱私防護鎖定（鎖定時模糊內容並顯示綠色盾牌解鎖畫面）
        /// </summary>
        public bool IsPrivacyLocked { get; set; } = false;

        /// <summary>
        /// 是否啟用滑鼠移出自動收合 (Roll-up)
        /// </summary>
        public bool EnableRollup { get; set; } = false;

        /// <summary>
        /// 主題強調色彩（十六進位，如 #0078D4）
        /// </summary>
        public string AccentColor { get; set; } = string.Empty;
    }

    /// <summary>
    /// 全域通用設定
    /// </summary>
    public class GeneralConfig
    {
        /// <summary>
        /// 是否隨 Windows 開機自動啟動
        /// </summary>
        public bool LaunchOnStartup { get; set; } = true;

        /// <summary>
        /// 是否啟用雙擊桌面空白處一鍵隱藏/顯示所有收納盒
        /// </summary>
        public bool DoubleClickHideDesktop { get; set; } = true;

        /// <summary>
        /// 視窗邊緣與收納盒間磁吸吸附距離 (像素)
        /// </summary>
        public double MagnetSnapDistance { get; set; } = 15;

        /// <summary>
        /// 全域鎖定所有收納盒位置
        /// </summary>
        public bool GlobalLockPositions { get; set; } = false;

        /// <summary>
        /// 主題模式：System (跟隨系統), Dark (暗黑), Light (淺色)
        /// </summary>
        public string ThemeMode { get; set; } = "System";

        /// <summary>
        /// 桌面一鍵整理預設目標目錄名稱
        /// </summary>
        public string OrganizeDirectoryName { get; set; } = "_桌面收納箱";
    }

    /// <summary>
    /// WinDeskBox 完整應用程式組態模型
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// 全域一般設定
        /// </summary>
        public GeneralConfig General { get; set; } = new GeneralConfig();

        /// <summary>
        /// 桌面所有收納盒清單
        /// </summary>
        public List<BoxConfig> Boxes { get; set; } = new List<BoxConfig>();
    }
}