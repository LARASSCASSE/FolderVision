using System.Threading.Tasks;
using System.Windows;

namespace FolderVision.Wpf
{
    public partial class App : System.Windows.Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Catch any unhandled exception and show a MessageBox instead of silently closing
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

            // Show splash immediately
            var splash = new SplashWindow();
            splash.Show();

            // Load MainWindow asynchronously so UI stays responsive
            await Task.Run(() => System.Threading.Thread.Sleep(100)); // let splash render first

            splash.SetStatus("Initializing...");
            var mainWindow = new MainWindow();

            splash.SetStatus("Ready");
            await Task.Delay(200); // brief pause so user sees "Ready"

            mainWindow.Show();
            splash.Close();

            MainWindow = mainWindow;
        }
    }
}
