using System.Windows;

namespace WinDeskBox.UI.Controls
{
    public enum ConflictResolution
    {
        KeepBoth,
        Overwrite,
        Skip
    }

    public partial class ConflictDialog : Window
    {
        public ConflictResolution Result { get; private set; } = ConflictResolution.Skip;

        public ConflictDialog(string fileName)
        {
            InitializeComponent();
            TxtFileName.Text = fileName;
        }

        private void BtnKeepBoth_Click(object sender, RoutedEventArgs e)
        {
            Result = ConflictResolution.KeepBoth;
            DialogResult = true;
            Close();
        }

        private void BtnOverwrite_Click(object sender, RoutedEventArgs e)
        {
            Result = ConflictResolution.Overwrite;
            DialogResult = true;
            Close();
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            Result = ConflictResolution.Skip;
            DialogResult = false;
            Close();
        }
    }
}