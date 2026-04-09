namespace FolderVision.Wpf
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            DispatcherUnhandledException += (s, ex) =>
            {
                System.Windows.MessageBox.Show(
                    $"An unexpected error occurred:\n\n{ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                    "FolderVision Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                System.Windows.MessageBox.Show(
                    $"Fatal error:\n\n{ex.ExceptionObject}",
                    "FolderVision Fatal Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            };

            base.OnStartup(e);
        }
    }
}
