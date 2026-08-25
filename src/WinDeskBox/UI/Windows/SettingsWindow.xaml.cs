using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WinDeskBox.Config;
using WinDeskBox.Core;

namespace WinDeskBox.UI.Windows
{
    public partial class SettingsWindow : Window
    {
        private readonly AppConfig _config;
        private readonly ObservableCollection<BoxConfig> _boxes = new();
        private readonly ObservableCollection<TabConfig> _tabs = new();
        private bool _isUpdatingUi = false;
        public bool HasSavedChanges { get; private set; } = false;

        public SettingsWindow(AppConfig config, string? targetBoxId = null)
        {
            InitializeComponent();
            _config = config;

            // 載入一般設定
            ChkStartup.IsChecked = _config.General.LaunchOnStartup;
            ChkGlobalLock.IsChecked = _config.General.GlobalLockPositions;

            // 初始化快速鍵按鍵下拉清單
            var availableKeys = new System.Collections.Generic.List<string>();
            for (char c = 'A'; c <= 'Z'; c++) availableKeys.Add(c.ToString());
            for (char c = '0'; c <= '9'; c++) availableKeys.Add(c.ToString());
            for (int f = 1; f <= 12; f++) availableKeys.Add($"F{f}");
            availableKeys.Add("Space");
            availableKeys.Add("Tab");
            availableKeys.Add("~");
            CmbHotkeyKey.ItemsSource = availableKeys;

            // 載入當前快速鍵設定
            string currentMods = _config.General.HotkeyModifiers ?? "Alt";
            ChkModAlt.IsChecked = currentMods.Contains("Alt", StringComparison.OrdinalIgnoreCase);
            ChkModCtrl.IsChecked = currentMods.Contains("Ctrl", StringComparison.OrdinalIgnoreCase);
            ChkModShift.IsChecked = currentMods.Contains("Shift", StringComparison.OrdinalIgnoreCase);
            ChkModWin.IsChecked = currentMods.Contains("Win", StringComparison.OrdinalIgnoreCase);

            string currentKey = _config.General.HotkeyKey ?? "D";
            CmbHotkeyKey.SelectedItem = availableKeys.FirstOrDefault(k => string.Equals(k, currentKey, StringComparison.OrdinalIgnoreCase)) ?? "D";

            // 載入收納盒清單
            foreach (var b in _config.Boxes)
            {
                _boxes.Add(b);
            }
            LstBoxes.ItemsSource = _boxes;
            LstTabs.ItemsSource = _tabs;

            // 即時同步收納盒標題文字輸入至左側清單
            TxtBoxTitle.TextChanged += (s, e) =>
            {
                if (_isUpdatingUi) return;
                if (LstBoxes.SelectedItem is BoxConfig box)
                {
                    box.Title = TxtBoxTitle.Text;
                    LstBoxes.Items.Refresh();
                }
            };

            // 即時同步分頁名稱與路徑文字輸入
            TxtTabName.TextChanged += (s, e) =>
            {
                if (_isUpdatingUi) return;
                if (LstTabs.SelectedItem is TabConfig tab)
                {
                    tab.Name = TxtTabName.Text;
                    LstTabs.Items.Refresh();
                }
            };

            TxtTabFolder.TextChanged += (s, e) =>
            {
                if (_isUpdatingUi) return;
                if (LstTabs.SelectedItem is TabConfig tab)
                {
                    tab.FolderPath = TxtTabFolder.Text;
                }
            };

            // 若有指定目標收納盒，直接切換至第二分頁並選取該收納盒
            if (!string.IsNullOrEmpty(targetBoxId))
            {
                MainTabControl.SelectedIndex = 1;
                var target = _boxes.FirstOrDefault(b => b.Id == targetBoxId);
                if (target != null)
                {
                    LstBoxes.SelectedItem = target;
                }
            }
            else if (_boxes.Count > 0)
            {
                LstBoxes.SelectedIndex = 0;
            }
        }

        private void LstBoxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstBoxes.SelectedItem is BoxConfig box)
            {
                BoxDetailPanel.Visibility = Visibility.Visible;
                _isUpdatingUi = true;
                TxtBoxTitle.Text = box.Title;
                ChkEnableRollup.IsChecked = box.EnableRollup;
                _isUpdatingUi = false;

                _tabs.Clear();
                foreach (var t in box.Tabs)
                {
                    _tabs.Add(t);
                }
                if (_tabs.Count > 0)
                {
                    LstTabs.SelectedIndex = 0;
                }
                else
                {
                    TabDetailPanel.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                BoxDetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void LstTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstTabs.SelectedItem is TabConfig tab)
            {
                TabDetailPanel.Visibility = Visibility.Visible;
                _isUpdatingUi = true;
                TxtTabName.Text = tab.Name;
                TxtTabFolder.Text = tab.FolderPath;
                _isUpdatingUi = false;
            }
            else
            {
                TabDetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnAddBox_Click(object sender, RoutedEventArgs e)
        {
            var newBox = new BoxConfig
            {
                Title = $"新收納盒 {_boxes.Count + 1}",
                Left = 120 + (_boxes.Count * 30),
                Top = 120 + (_boxes.Count * 30),
                Width = 340,
                Height = 280
            };
            newBox.Tabs.Add(new TabConfig { Name = "預設分頁", FolderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) });
            _boxes.Add(newBox);
            LstBoxes.SelectedItem = newBox;
        }

        private void BtnDeleteBox_Click(object sender, RoutedEventArgs e)
        {
            if (LstBoxes.SelectedItem is BoxConfig box)
            {
                if (MessageBox.Show($"確定要刪除收納盒「{box.Title}」嗎？（不會刪除硬碟中的實體檔案）", "確認刪除", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _boxes.Remove(box);
                }
            }
        }

        private void BtnAddTab_Click(object sender, RoutedEventArgs e)
        {
            if (LstBoxes.SelectedItem is BoxConfig box)
            {
                var newTab = new TabConfig
                {
                    Name = $"分頁 {_tabs.Count + 1}",
                    FolderPath = string.Empty
                };
                box.Tabs.Add(newTab);
                _tabs.Add(newTab);
                LstTabs.SelectedItem = newTab;
            }
        }

        private void BtnDeleteTab_Click(object sender, RoutedEventArgs e)
        {
            if (LstBoxes.SelectedItem is BoxConfig box && LstTabs.SelectedItem is TabConfig tab)
            {
                if (box.Tabs.Count <= 1)
                {
                    MessageBox.Show("收納盒必須保留至少一個分頁！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                box.Tabs.Remove(tab);
                _tabs.Remove(tab);
            }
        }

        private void BtnBrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "選擇要映射至收納盒的本機資料夾",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
            };

            if (dialog.ShowDialog() == true)
            {
                if (SecurityGuard.ValidateFolderPath(dialog.FolderName, out string sanitized))
                {
                    _isUpdatingUi = true;
                    TxtTabFolder.Text = sanitized;
                    _isUpdatingUi = false;

                    if (LstTabs.SelectedItem is TabConfig tab)
                    {
                        tab.FolderPath = sanitized;
                    }
                }
                else
                {
                    MessageBox.Show("此路徑為系統核心受保護目錄，基於資安考量禁止映射！", "路徑受阻", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void BtnOrganizeDesktop_Click(object sender, RoutedEventArgs e)
        {
            int count = SafetyManager.OrganizeDesktop(_config.General.OrganizeDirectoryName);
            MessageBox.Show($"整理完成！已成功分類歸檔 {count} 個桌面檔案至「{_config.General.OrganizeDirectoryName}」目錄中。", "桌面整理完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnRestoreDesktop_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("確定要將收納箱內的所有檔案還原回桌面根目錄嗎？", "確認還原", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int count = SafetyManager.RestoreDesktop(_config.General.OrganizeDirectoryName);
                MessageBox.Show($"還原完成！已將 {count} 個檔案移回桌面。", "桌面還原完成", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 儲存當前編輯中項目的文字
            if (LstBoxes.SelectedItem is BoxConfig currentBox)
            {
                currentBox.Title = TxtBoxTitle.Text.Trim();
                currentBox.EnableRollup = ChkEnableRollup.IsChecked ?? false;
                LstBoxes.Items.Refresh();
            }
            if (LstTabs.SelectedItem is TabConfig currentTab)
            {
                currentTab.Name = TxtTabName.Text.Trim();
                currentTab.FolderPath = TxtTabFolder.Text.Trim();
                LstTabs.Items.Refresh();
            }

            // 儲存一般設定
            bool startup = ChkStartup.IsChecked ?? true;
            _config.General.LaunchOnStartup = startup;
            _config.General.GlobalLockPositions = ChkGlobalLock.IsChecked ?? false;

            // 儲存快速鍵組合
            var modsList = new System.Collections.Generic.List<string>();
            if (ChkModCtrl.IsChecked == true) modsList.Add("Ctrl");
            if (ChkModAlt.IsChecked == true) modsList.Add("Alt");
            if (ChkModShift.IsChecked == true) modsList.Add("Shift");
            if (ChkModWin.IsChecked == true) modsList.Add("Win");
            if (modsList.Count == 0) modsList.Add("Alt"); // 若皆未勾選，預設保留 Alt

            _config.General.HotkeyModifiers = string.Join(" + ", modsList);
            _config.General.HotkeyKey = (CmbHotkeyKey.SelectedItem as string) ?? "D";

            // 同步開機啟動狀態
            StartupManager.SetStartup(startup);

            // 同步 Box 清單
            _config.Boxes.Clear();
            foreach (var b in _boxes)
            {
                _config.Boxes.Add(b);
            }

            ConfigManager.Save(_config);
            HasSavedChanges = true;

            // 立即即時套用變更至桌面所有收納盒與系統匣，不關閉視窗
            if (Application.Current is App app)
            {
                app.ReloadAllFences();
            }

            MessageBox.Show("設定已成功儲存並即時套用！", "儲存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}