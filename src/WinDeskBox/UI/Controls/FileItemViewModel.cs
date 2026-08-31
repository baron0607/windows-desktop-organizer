using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace WinDeskBox.UI.Controls
{
    /// <summary>
    /// 收納盒內單一檔案或子資料夾的視覺項目模型
    /// </summary>
    public class FileItemViewModel : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private ImageSource? _icon;
        private bool _isRenaming;
        private string _editingName = string.Empty;
        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayName)); }
        }

        public string DisplayName =>
            (!IsDirectory && Name.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                ? Path.GetFileNameWithoutExtension(Name)
                : Name;

        public string FullPath { get; set; } = string.Empty;

        public bool IsDirectory { get; set; }

        public ImageSource? Icon
        {
            get => _icon;
            set { _icon = value; OnPropertyChanged(); }
        }

        public string FormattedSize { get; set; } = string.Empty;

        public string LastModified { get; set; } = string.Empty;

        public bool IsRenaming
        {
            get => _isRenaming;
            set { _isRenaming = value; OnPropertyChanged(); }
        }

        public string EditingName
        {
            get => _editingName;
            set { _editingName = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}