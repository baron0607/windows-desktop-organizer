using System;
using System.Collections.ObjectModel;
using System.IO;
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

        public SettingsWindow(AppConfig config)
        {
            InitializeComponent();
            _config = config;

            // 載入一般設定
            ChkStartup.IsChecked = _config.General.LaunchOnStartup;
            ChkDoubleClickHide.IsChecked = _config.General.DoubleClickHideDesktop;
            ChkGlobalLock.IsChecked = _config.General.GlobalLockPositions;

            // 載入收納盒清單
            foreach (var b in _config.Boxes)
            {
                _boxes.Add(b);
            }
            LstBoxes.ItemsSource = _boxes;
            LstTabs.ItemsSource = _tabs;

            if (_boxes.Count > 0)
            {
                LstBoxes.SelectedIndex = 0;
            }
        }

        private void LstBoxes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstBoxes.SelectedItem is BoxConfig box)
            {
                BoxDetailPanel.Visibility = Visibility.Visible;
                TxtBoxTitle.Text = box.Title;
                ChkEnableRollup.IsChecked = box.EnableRollup;

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
                TxtTabName.Text = tab.Name;
                TxtTabFolder.Text = tab.FolderPath;
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
                    TxtTabFolder.Text = sanitized;
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
            }
            if (LstTabs.SelectedItem is TabConfig currentTab)
            {
                currentTab.Name = TxtTabName.Text.Trim();
                currentTab.FolderPath = TxtTabFolder.Text.Trim();
            }

            // 儲存一般設定
            bool startup = ChkStartup.IsChecked ?? true;
            _config.General.LaunchOnStartup = startup;
            _config.General.DoubleClickHideDesktop = ChkDoubleClickHide.IsChecked ?? true;
            _config.General.GlobalLockPositions = ChkGlobalLock.IsChecked ?? false;

            // 同步開機啟動狀態
            StartupManager.SetStartup(startup);

            // 同步 Box 清單
            _config.Boxes.Clear();
            foreach (var b in _boxes)
            {
                _config.Boxes.Add(b);
            }

            ConfigManager.Save(_config);
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}