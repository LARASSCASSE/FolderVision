using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FolderVision.Wpf
{
    public partial class App : System.Windows.Application
    {
        // ── Single-instance enforcement ───────────────────────────────────────
        private static Mutex? _singleInstanceMutex;

        [DllImport("user32.dll")] private static extern IntPtr FindWindow(string? cls, string title);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_RESTORE = 9;

        protected override void OnStartup(StartupEventArgs e)
        {
            // ── Single-instance guard (before anything else) ──────────────────
            _singleInstanceMutex = new Mutex(initiallyOwned: true,
                name: "FolderVision_SingleInstance_Mutex",
                out bool createdNew);

            if (!createdNew)
            {
                // Another instance is already running — bring it to the front
                IntPtr hWnd = FindWindow(null, "FolderVision");
                if (hWnd != IntPtr.Zero)
                {
                    ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                }
                Shutdown(0);
                return;
            }

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

        protected override void OnExit(ExitEventArgs e)
        {
            try { _singleInstanceMutex?.ReleaseMutex(); } catch { }
            _singleInstanceMutex?.Dispose();
            base.OnExit(e);
        }
    }
}
