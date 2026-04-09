using System.Windows;

namespace FolderVision.Wpf
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string message)
        {
            Dispatcher.Invoke(() => LoadingLabel.Text = message);
        }
    }
}
