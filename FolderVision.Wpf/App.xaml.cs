using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FolderVision.Wpf
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Register exception handlers BEFORE anything else
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            base.OnStartup(e);

            // Show splash immediately — wrap in try/catch so any XAML parse error surfaces
            SplashWindow splash;
            try
            {
                splash = new SplashWindow();
                splash.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to load splash screen:\n\n{ex.Message}",
                    "FolderVision", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            // Load MainWindow on next dispatcher cycle (Background priority)
            // so the splash gets a chance to render first
            Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    // Let splash paint itself
                    await Task.Delay(80);

                    splash.SetStatus("Initializing...");
                    await Task.Delay(80);

                    var main = new MainWindow();

                    splash.SetStatus("Ready");
                    await Task.Delay(180);

                    main.Show();
                    MainWindow = main;
                    splash.Close();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Startup error:\n\n{ex.Message}\n\n{ex.StackTrace}",
                        "FolderVision",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    splash.Close();
                    Shutdown(1);
                }
            }, DispatcherPriority.Background);
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(
                $"Unexpected error:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                "FolderVision Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.Windows.MessageBox.Show(
                $"Fatal error:\n\n{e.ExceptionObject}",
                "FolderVision Fatal Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
