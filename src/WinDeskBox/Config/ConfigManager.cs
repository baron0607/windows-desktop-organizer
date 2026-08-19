using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WinDeskBox.Config
{
    /// <summary>
    /// 設定檔管理器，負責本機 config.json 的序列化、反序列化與預設值生成
    /// </summary>
    public static class ConfigManager
    {
        private static readonly string ConfigFileName = "config.json";
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// 取得設定檔完整路徑（位於程式執行檔同目錄下，確保完全 Portable 綠色化）
        /// </summary>
        public static string ConfigPath
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, ConfigFileName);
            }
        }

        /// <summary>
        /// 載入設定檔，若檔案不存在或損毀則自動建立預設設定
        /// </summary>
        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
                    if (config != null)
                    {
                        EnsureValidConfig(config);
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 載入設定檔失敗: {ex.Message}，將採用預設設定。");
            }

            var defaultConfig = CreateDefaultConfig();
            Save(defaultConfig);
            return defaultConfig;
        }

        /// <summary>
        /// 儲存設定至本機 config.json
        /// </summary>
        public static void Save(AppConfig config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConfigManager] 儲存設定檔失敗: {ex.Message}");
            }
        }

        /// <summary>
        /// 建立預設收納盒設定（自動提供文件、圖片、專案常用等預設映射）
        /// </summary>
        private static AppConfig CreateDefaultConfig()
        {
            var config = new AppConfig();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string organizeRoot = Path.Combine(desktopPath, config.General.OrganizeDirectoryName);

            // 預設收納盒 1：常用專案 / 捷徑映射
            var workBox = new BoxConfig
            {
                Title = "專案與捷徑",
                Left = 80,
                Top = 60,
                Width = 360,
                Height = 300,
                Opacity = 0.8
            };
            workBox.Tabs.Add(new TabConfig
            {
                Name = "執行中專案",
                FolderPath = Path.Combine(organizeRoot, "執行中"),
                ViewMode = "Grid",
                SortBy = "ModifiedTime",
                SortAscending = false
            });
            workBox.Tabs.Add(new TabConfig
            {
                Name = "常用捷徑",
                FolderPath = Path.Combine(organizeRoot, "常用捷徑"),
                ViewMode = "Grid",
                SortBy = "Name",
                SortAscending = true
            });

            // 預設收納盒 2：常用文件與下載
            var docsBox = new BoxConfig
            {
                Title = "文件與圖片",
                Left = 460,
                Top = 60,
                Width = 360,
                Height = 300,
                Opacity = 0.8
            };
            docsBox.Tabs.Add(new TabConfig
            {
                Name = "文件檔案",
                FolderPath = Path.Combine(organizeRoot, "文件"),
                ViewMode = "Grid",
                SortBy = "ModifiedTime",
                SortAscending = false
            });
            docsBox.Tabs.Add(new TabConfig
            {
                Name = "圖片與素材",
                FolderPath = Path.Combine(organizeRoot, "圖片"),
                ViewMode = "Grid",
                SortBy = "ModifiedTime",
                SortAscending = false
            });

            config.Boxes.Add(workBox);
            config.Boxes.Add(docsBox);
            return config;
        }

        private static void EnsureValidConfig(AppConfig config)
        {
            config.General ??= new GeneralConfig();
            config.Boxes ??= new List<BoxConfig>();
            foreach (var box in config.Boxes)
            {
                box.Tabs ??= new List<TabConfig>();
                if (box.Tabs.Count == 0)
                {
                    box.Tabs.Add(new TabConfig { Name = "預設" });
                }
                if (box.Width < 150) box.Width = 300;
                if (box.Height < 100) box.Height = 200;
            }
        }
    }
}