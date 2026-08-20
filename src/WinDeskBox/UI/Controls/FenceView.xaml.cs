using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WinDeskBox.Config;
using WinDeskBox.Core;

namespace WinDeskBox.UI.Controls
{
    public partial class FenceView : Window
    {
        public BoxConfig Config { get; }
        public ObservableCollection<FileItemViewModel> FileItems { get; } = new();

        private FolderPortalWatcher? _portalWatcher;
        private bool _isCollapsed = false;
        private double _expandedHeight = 280;

        public static readonly IValueConverter BoolToVis = new BooleanToVisibilityConverter();
        public static readonly IValueConverter InverseBoolToVis = new InverseBooleanToVisibilityConverter();

        public FenceView(BoxConfig config)
        {
            InitializeComponent();
            Config = config;
            DataContext = this;
            FilesItemsControl.ItemsSource = FileItems;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. 套用 Windows 11 DWM Acrylic 毛玻璃特效
            AcrylicHelper.EnableAcrylic(this, true);

            // 2. 釘在桌面底層圖層 (WorkerW / HWND_BOTTOM)
            DesktopWorker.PinToDesktopBottom(this);

            // 3. 掛載 Win32 訊息攔截以支援上下左右邊框 8 向自由拉伸
            var helper = new WindowInteropHelper(this);
            var source = HwndSource.FromHwnd(helper.Handle);
            source?.AddHook(WndProc);

            // 4. 恢復幾何尺寸與座標
            ScreenBoundsGuard.ClampToScreen(Config);
            Left = Config.Left;
            Top = Config.Top;
            Width = Config.Width;
            Height = Config.Height;
            _expandedHeight = Config.Height;

            TxtBoxTitle.Text = Config.Title;
            UpdateLockState(Config.IsLocked);

            // 5. 初始化 Tabs 分頁與資料夾內容載入
            RenderTabs();
            SwitchToTab(Config.ActiveTabIndex);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isCollapsed && IsLoaded)
            {
                Config.Width = Width;
                Config.Height = Height;
                _expandedHeight = Height;
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (IsLoaded)
            {
                Config.Left = Left;
                Config.Top = Top;
            }
        }

        #region Win32 8 向邊界感應拉伸調整尺寸
        private const int WM_NCHITTEST = 0x0084;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                if (Config.IsLocked || _isCollapsed)
                {
                    return IntPtr.Zero;
                }

                int x = (short)(lParam.ToInt32() & 0xFFFF);
                int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

                Point pt = PointFromScreen(new Point(x, y));
                double w = ActualWidth;
                double h = ActualHeight;
                double b = 7; // 邊框感應厚度 7px

                bool left = pt.X <= b;
                bool right = pt.X >= w - b;
                bool top = pt.Y <= b;
                bool bottom = pt.Y >= h - b;

                if (top && left) { handled = true; return (IntPtr)HTTOPLEFT; }
                if (top && right) { handled = true; return (IntPtr)HTTOPRIGHT; }
                if (bottom && left) { handled = true; return (IntPtr)HTBOTTOMLEFT; }
                if (bottom && right) { handled = true; return (IntPtr)HTBOTTOMRIGHT; }
                if (left) { handled = true; return (IntPtr)HTLEFT; }
                if (right) { handled = true; return (IntPtr)HTRIGHT; }
                if (top) { handled = true; return (IntPtr)HTTOP; }
                if (bottom) { handled = true; return (IntPtr)HTBOTTOM; }
            }
            return IntPtr.Zero;
        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _portalWatcher?.Dispose();
        }

        #region Tabs 分頁管理
        private void RenderTabs()
        {
            TabsPanel.Children.Clear();
            if (Config.Tabs.Count <= 1) return; // 只有一個分頁時隱藏分頁列

            for (int i = 0; i < Config.Tabs.Count; i++)
            {
                int tabIndex = i;
                var tab = Config.Tabs[i];
                var tabBtn = new Button
                {
                    Content = tab.Name,
                    Margin = new Thickness(2, 0, 2, 0),
                    Padding = new Thickness(10, 3, 10, 3),
                    Cursor = Cursors.Hand,
                    Style = (Style)FindResource("FluentTabButton"),
                    Tag = tabIndex
                };

                if (i == Config.ActiveTabIndex)
                {
                    tabBtn.Background = new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF));
                    tabBtn.BorderBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF));
                    tabBtn.Foreground = Brushes.White;
                }
                else
                {
                    tabBtn.Background = Brushes.Transparent;
                    tabBtn.BorderBrush = Brushes.Transparent;
                    tabBtn.Foreground = (Brush)FindResource("SubTextBrush");
                }

                tabBtn.Click += (s, ev) => SwitchToTab(tabIndex);
                TabsPanel.Children.Add(tabBtn);
            }
        }

        public void SwitchToTab(int index)
        {
            if (index < 0 || index >= Config.Tabs.Count) index = 0;
            Config.ActiveTabIndex = index;
            var activeTab = Config.Tabs[index];

            // 更新分頁按鈕視覺
            for (int i = 0; i < TabsPanel.Children.Count; i++)
            {
                if (TabsPanel.Children[i] is Button btn)
                {
                    bool isActive = (i == index);
                    btn.Background = isActive ? new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF)) : Brushes.Transparent;
                    btn.BorderBrush = isActive ? new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF)) : Brushes.Transparent;
                    btn.Foreground = isActive ? Brushes.White : (Brush)FindResource("SubTextBrush");
                }
            }

            // 套用檢視樣式 (Grid / List)
            ApplyViewMode(activeTab.ViewMode);

            // 更新監聽器與載入檔案
            _portalWatcher?.Dispose();
            if (!string.IsNullOrWhiteSpace(activeTab.FolderPath))
            {
                if (SecurityGuard.ValidateFolderPath(activeTab.FolderPath, out string sanitized))
                {
                    Directory.CreateDirectory(sanitized);
                    _portalWatcher = new FolderPortalWatcher(sanitized, () => _ = LoadFilesAsync());
                }
            }

            _ = LoadFilesAsync();
        }

        private void ApplyViewMode(string viewMode)
        {
            if (viewMode == "List")
            {
                FilesItemsControl.ItemTemplate = (DataTemplate)FindResource("ListItemTemplate");
            }
            else
            {
                FilesItemsControl.ItemTemplate = (DataTemplate)FindResource("GridItemTemplate");
            }
        }
        #endregion

        #region 檔案載入與展示
        public async Task LoadFilesAsync()
        {
            if (Config.ActiveTabIndex < 0 || Config.ActiveTabIndex >= Config.Tabs.Count) return;
            var tab = Config.Tabs[Config.ActiveTabIndex];
            string folderPath = tab.FolderPath;

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                FileItems.Clear();
                return;
            }

            try
            {
                var dirInfo = new DirectoryInfo(folderPath);
                var subDirs = dirInfo.GetDirectories().Where(d => (d.Attributes & FileAttributes.Hidden) == 0);
                var files = dirInfo.GetFiles().Where(f => (f.Attributes & FileAttributes.Hidden) == 0 && !SafetyManager.IsWhitelisted(f.FullName));

                var items = new System.Collections.Generic.List<FileItemViewModel>();

                // 1. 載入子資料夾
                foreach (var dir in subDirs)
                {
                    items.Add(new FileItemViewModel
                    {
                        Name = dir.Name,
                        FullPath = dir.FullName,
                        IsDirectory = true,
                        FormattedSize = "資料夾",
                        LastModified = dir.LastWriteTime.ToString("yyyy/MM/dd HH:mm")
                    });
                }

                // 2. 載入檔案
                foreach (var file in files)
                {
                    items.Add(new FileItemViewModel
                    {
                        Name = file.Name,
                        FullPath = file.FullName,
                        IsDirectory = false,
                        FormattedSize = FormatFileSize(file.Length),
                        LastModified = file.LastWriteTime.ToString("yyyy/MM/dd HH:mm")
                    });
                }

                // 3. 排序邏輯（子資料夾始終在前，其餘依設定排序）
                IOrderedEnumerable<FileItemViewModel> sorted;
                if (tab.SortBy == "Name")
                {
                    sorted = tab.SortAscending ? items.OrderByDescending(x => x.IsDirectory).ThenBy(x => x.Name) : items.OrderByDescending(x => x.IsDirectory).ThenByDescending(x => x.Name);
                }
                else if (tab.SortBy == "Extension")
                {
                    sorted = tab.SortAscending ? items.OrderByDescending(x => x.IsDirectory).ThenBy(x => Path.GetExtension(x.FullPath)) : items.OrderByDescending(x => x.IsDirectory).ThenByDescending(x => Path.GetExtension(x.FullPath));
                }
                else // ModifiedTime
                {
                    sorted = tab.SortAscending ? items.OrderByDescending(x => x.IsDirectory).ThenBy(x => x.LastModified) : items.OrderByDescending(x => x.IsDirectory).ThenByDescending(x => x.LastModified);
                }

                FileItems.Clear();
                foreach (var item in sorted)
                {
                    FileItems.Add(item);
                }

                // 4. 非同步擷取圖示（背景載入不卡頓）
                foreach (var item in FileItems)
                {
                    item.Icon = await ShellIconLoader.GetIconAsync(item.FullPath, item.IsDirectory);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FenceView] 載入資料夾內容異常: {ex.Message}");
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }
        #endregion

        #region 互動操作 (點擊開啟、右鍵選單、拖曳存入)
        private void Item_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2 && sender is FrameworkElement elem && elem.DataContext is FileItemViewModel item)
            {
                OpenFileOrDirectory(item);
            }
        }

        private void OpenFileOrDirectory(FileItemViewModel item)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = item.FullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"無法開啟檔案：{ex.Message}", "WinDeskBox 提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedMenuItemContext(sender) is FileItemViewModel item)
            {
                OpenFileOrDirectory(item);
            }
        }

        private void MenuShowInExplorer_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedMenuItemContext(sender) is FileItemViewModel item)
            {
                try
                {
                    Process.Start("explorer.exe", $"/select,\"{item.FullPath}\"");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"無法在檔案總管中顯示: {ex.Message}");
                }
            }
        }

        private void MenuCopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedMenuItemContext(sender) is FileItemViewModel item)
            {
                Clipboard.SetText(item.FullPath);
            }
        }

        private void MenuRename_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedMenuItemContext(sender) is FileItemViewModel item)
            {
                item.EditingName = item.Name;
                item.IsRenaming = true;
            }
        }

        private void RenameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is FileItemViewModel item)
            {
                if (e.Key == Key.Enter)
                {
                    CommitRename(item);
                }
                else if (e.Key == Key.Escape)
                {
                    item.IsRenaming = false;
                }
            }
        }

        private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is FileItemViewModel item && item.IsRenaming)
            {
                CommitRename(item);
            }
        }

        private void CommitRename(FileItemViewModel item)
        {
            item.IsRenaming = false;
            string newName = item.EditingName.Trim();
            if (string.IsNullOrEmpty(newName) || newName.Equals(item.Name, StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                string dir = Path.GetDirectoryName(item.FullPath) ?? "";
                string newPath = Path.Combine(dir, newName);

                if (item.IsDirectory)
                {
                    Directory.Move(item.FullPath, newPath);
                }
                else
                {
                    File.Move(item.FullPath, newPath);
                }
                _ = LoadFilesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"重新命名失敗：{ex.Message}", "WinDeskBox 提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuDelete_Click(object sender, RoutedEventArgs e)
        {
            if (GetSelectedMenuItemContext(sender) is FileItemViewModel item)
            {
                if (ShellFileOperation.MoveToRecycleBin(item.FullPath))
                {
                    _ = LoadFilesAsync();
                }
            }
        }

        private static FileItemViewModel? GetSelectedMenuItemContext(object sender)
        {
            if (sender is MenuItem mi && mi.DataContext is FileItemViewModel item)
            {
                return item;
            }
            return null;
        }
        #endregion

        #region 拖曳存入支援 (Drag and Drop)
        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Move | DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (Config.ActiveTabIndex >= 0 && Config.ActiveTabIndex < Config.Tabs.Count)
                {
                    string targetFolder = Config.Tabs[Config.ActiveTabIndex].FolderPath;
                    if (Directory.Exists(targetFolder))
                    {
                        foreach (string source in files)
                        {
                            try
                            {
                                string fileName = Path.GetFileName(source);
                                string dest = Path.Combine(targetFolder, fileName);

                                if (File.Exists(dest) || Directory.Exists(dest))
                                {
                                    var dialog = new ConflictDialog(fileName);
                                    if (dialog.ShowDialog() == true)
                                    {
                                        if (dialog.Result == ConflictResolution.KeepBoth)
                                        {
                                            dest = SafetyManager.GetSafeDestinationPath(targetFolder, fileName);
                                        }
                                        else if (dialog.Result == ConflictResolution.Skip)
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if (File.Exists(source))
                                {
                                    File.Move(source, dest);
                                }
                                else if (Directory.Exists(source))
                                {
                                    Directory.Move(source, dest);
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"[FenceView] 拖曳移動失敗 ({source}): {ex.Message}");
                            }
                        }
                        _ = LoadFilesAsync();
                    }
                }
            }
        }
        #endregion

        #region 視窗拖動、位置鎖定、折疊與吸附
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!Config.IsLocked && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
                SnapToScreenEdge();
                SavePosition();
            }
        }

        private void SnapToScreenEdge()
        {
            double snapDist = 15;
            double workLeft = SystemParameters.WorkArea.Left;
            double workTop = SystemParameters.WorkArea.Top;
            double workRight = workLeft + SystemParameters.WorkArea.Width;
            double workBottom = workTop + SystemParameters.WorkArea.Height;

            if (Math.Abs(Left - workLeft) < snapDist) Left = workLeft;
            if (Math.Abs(Top - workTop) < snapDist) Top = workTop;
            if (Math.Abs((Left + Width) - workRight) < snapDist) Left = workRight - Width;
            if (Math.Abs((Top + Height) - workBottom) < snapDist) Top = workBottom - Height;
        }

        private void SavePosition()
        {
            Config.Left = Left;
            Config.Top = Top;
            Config.Width = Width;
            if (!_isCollapsed) Config.Height = Height;
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current is App app)
            {
                app.OpenSettingsForBox(Config.Id);
            }
        }

        private void BtnToggleView_Click(object sender, RoutedEventArgs e)
        {
            if (Config.ActiveTabIndex >= 0 && Config.ActiveTabIndex < Config.Tabs.Count)
            {
                var tab = Config.Tabs[Config.ActiveTabIndex];
                tab.ViewMode = (tab.ViewMode == "Grid") ? "List" : "Grid";
                ApplyViewMode(tab.ViewMode);
            }
        }

        private void BtnLockPosition_Click(object sender, RoutedEventArgs e)
        {
            UpdateLockState(!Config.IsLocked);
        }

        public void UpdateLockState(bool locked)
        {
            Config.IsLocked = locked;
            PathLockIcon.Data = Geometry.Parse(locked
                ? "M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17M18,8A2,2 0 0,1 20,10V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V10C4,8.89 4.89,8 6,8H7V6A5,5 0 0,1 12,1A5,5 0 0,1 17,6V8H18M12,3A3,3 0 0,0 9,6V8H15V6A3,3 0 0,0 12,3Z"
                : "M18,8A2,2 0 0,1 20,10V20A2,2 0 0,1 18,22H6A2,2 0 0,1 4,20V10A2,2 0 0,1 6,8H15V6A3,3 0 0,0 12,3A3,3 0 0,0 9,6H7A5,5 0 0,1 12,1A5,5 0 0,1 17,6V8H18M12,17A2,2 0 0,0 14,15C14,13.89 13.1,13 12,13A2,2 0 0,0 10,15A2,2 0 0,0 12,17Z");
            PathLockIcon.Opacity = locked ? 1.0 : 0.7;
        }

        private void BtnRollup_Click(object sender, RoutedEventArgs e)
        {
            ToggleRollup(!_isCollapsed);
        }

        private void ToggleRollup(bool collapse)
        {
            _isCollapsed = collapse;
            if (collapse)
            {
                _expandedHeight = Height;
                Height = 38;
                ContentScrollViewer.Visibility = Visibility.Collapsed;
                PathRollupIcon.Data = Geometry.Parse("M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z");
            }
            else
            {
                Height = _expandedHeight;
                ContentScrollViewer.Visibility = Visibility.Visible;
                PathRollupIcon.Data = Geometry.Parse("M7.41,15.41L12,10.83L16.59,15.41L18,14L12,8L6,14L7.41,15.41Z");
            }
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_isCollapsed && Config.EnableRollup)
            {
                ToggleRollup(false);
            }
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!_isCollapsed && Config.EnableRollup)
            {
                ToggleRollup(true);
            }
        }
        #endregion
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v != Visibility.Visible;
        }
    }
}