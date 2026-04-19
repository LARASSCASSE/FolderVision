using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using FolderVision.Core;
using FolderVision.Core.Logging;
using FolderVision.Exporters;
using FolderVision.Models;

namespace FolderVision.Wpf
{
    public partial class MainWindow : Window
    {
        private ScanEngine? _scanEngine;
        private ScanResult? _lastScanResult;
        private bool _isScanning;
        // Preview tabs: index 0 in RightTabControl = "Folder Structure" (fixed), 1..N = preview tabs
        private readonly List<TabItem> _previewTabs = new();

        // Tab strip scroll
        private System.Windows.Controls.ScrollViewer? _tabHeaderScroll;
        private System.Windows.Controls.Button? _tabScrollLeftBtn;
        private System.Windows.Controls.Button? _tabScrollRightBtn;

        // Track current handler to avoid stale subscriptions
        private EventHandler<ProgressEventArgs>? _progressHandler;

        // Progress polling: background threads write the latest value, UI timer reads it.
        // Replaces Dispatcher.InvokeAsync — avoids queue accumulation from parallel threads.
        private volatile int _latestProgressPct;
        private string _latestProgressMsg = string.Empty;
        private readonly object _progressMsgLock = new();
        private System.Windows.Threading.DispatcherTimer? _progressTimer;
        private const int ProgressPollMs = 120; // ~8 refreshes/sec

        // Set immediately on cancel so wind-down tasks don't overwrite the UI
        private bool _isCancelling;

        // Pause state
        private bool _isPaused;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Close();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (_scanEngine != null && _progressHandler != null)
                _scanEngine.ProgressChanged -= _progressHandler;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PATH MANAGEMENT
        // ─────────────────────────────────────────────────────────────────────

        private void AddPathButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select one or more folders to scan",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;

            var added = AddFolderPaths(dialog.FolderNames);
            if (added > 0)
                SetStatus(added == 1 ? $"Added: {dialog.FolderNames[0]}" : $"{added} paths added.");
        }

        /// <summary>Adds a list of folder paths, skipping duplicates and non-existent ones.</summary>
        private int AddFolderPaths(IEnumerable<string> paths)
        {
            var existing = new HashSet<string>(
                PathsListBox.Items.Cast<string>(),
                StringComparer.OrdinalIgnoreCase);

            var added = 0;
            foreach (var path in paths)
            {
                if (!Directory.Exists(path)) continue;
                if (!existing.Add(path)) continue;  // Add returns false if already present

                PathsListBox.Items.Add(path);
                added++;
            }
            if (added > 0) UpdateStartButtonState();
            return added;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DRAG & DROP
        // ─────────────────────────────────────────────────────────────────────

        private int _dragEnterCount;

        private void Window_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) { e.Effects = System.Windows.DragDropEffects.None; return; }
            _dragEnterCount++;
            if (_dragEnterCount == 1) DragOverlay.Visibility = Visibility.Visible;
            e.Effects = System.Windows.DragDropEffects.Copy;
            e.Handled = true;
        }

        private void Window_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
                ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            _dragEnterCount = Math.Max(0, _dragEnterCount - 1);
            if (_dragEnterCount == 0) DragOverlay.Visibility = Visibility.Collapsed;
        }

        private void Window_Drop(object sender, System.Windows.DragEventArgs e)
        {
            _dragEnterCount = 0;
            DragOverlay.Visibility = Visibility.Collapsed;

            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;
            var paths = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
            var added = AddFolderPaths(paths);
            SetStatus(added == 0 ? "No new folders added." :
                      added == 1 ? $"Added: {paths.First(Directory.Exists)}" :
                                   $"{added} folders added.");
            e.Handled = true;
        }

        private void ClearAllPathsButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop any running scan
            if (_isScanning)
            {
                _isCancelling = true;
                if (_isPaused) { _isPaused = false; _scanEngine?.Resume(); }
                _progressTimer?.Stop();
                _progressTimer = null;
                _scanEngine?.CancelScan();
            }

            // Reset everything to initial state
            PathsListBox.Items.Clear();
            _lastScanResult = null;
            _isScanning = false;

            // Reset scan controls
            SetScanningState(false);
            UpdateProgress(0, string.Empty);
            SetStatus(string.Empty);

            // Reset results panel
            StatsBlock.Visibility = Visibility.Collapsed;
            ExportPdfButton.IsEnabled = false;

            // Reset tree
            FolderTreeView.Items.Clear();
            FolderTreeView.Visibility = Visibility.Collapsed;
            TreePlaceholder.Visibility = Visibility.Visible;

            // Remove dynamically-added preview tabs (keep only the first fixed tab)
            while (RightTabControl.Items.Count > 1)
                RightTabControl.Items.RemoveAt(1);
            RightTabControl.SelectedIndex = 0;
        }

        private void RemovePathItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is string path)
            {
                PathsListBox.Items.Remove(path);
                UpdateStartButtonState();
                SetStatus("Path removed.");
            }
        }

        private void PathsListBox_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
            => System.Windows.Controls.ScrollViewer.SetHorizontalScrollBarVisibility(PathsListBox, System.Windows.Controls.ScrollBarVisibility.Auto);

        private void PathsListBox_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
            => System.Windows.Controls.ScrollViewer.SetHorizontalScrollBarVisibility(PathsListBox, System.Windows.Controls.ScrollBarVisibility.Hidden);

        private void PathsListBox_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var sv = FindScrollViewer(PathsListBox);
            if (sv == null) return;

            if (System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Shift)
                sv.ScrollToHorizontalOffset(sv.HorizontalOffset - e.Delta / 3.0);
            else
                sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3.0);

            e.Handled = true;
        }

        // ── WM_MOUSEHWHEEL hook — precision trackpad horizontal swipe ────────
        private const int WM_MOUSEHWHEEL = 0x020E;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = System.Windows.Interop.HwndSource.FromHwnd(
                new System.Windows.Interop.WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MOUSEHWHEEL)
            {
                // Positive delta = scroll right, negative = scroll left
                var delta = (short)(wParam.ToInt64() >> 16);

                // Paths list box horizontal scroll
                var pathsSv = FindScrollViewer(PathsListBox);
                if (pathsSv != null && IsElementUnderMouse(PathsListBox))
                {
                    pathsSv.ScrollToHorizontalOffset(pathsSv.HorizontalOffset + delta / 3.0);
                    handled = true;
                }

                // Tab strip horizontal scroll
                if (!handled && _tabHeaderScroll != null && IsElementUnderMouse(RightTabControl))
                {
                    _tabHeaderScroll.ScrollToHorizontalOffset(_tabHeaderScroll.HorizontalOffset + delta / 3.0);
                    UpdateTabNavButtons();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private bool IsElementUnderMouse(UIElement element)
        {
            var pos = System.Windows.Input.Mouse.GetPosition(element);
            return pos.X >= 0 && pos.Y >= 0
                && pos.X <= element.RenderSize.Width
                && pos.Y <= element.RenderSize.Height;
        }

        private static System.Windows.Controls.ScrollViewer? FindScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.ScrollViewer sv) return sv;
                var result = FindScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SETTINGS
        // ─────────────────────────────────────────────────────────────────────

        private void ThreadsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ThreadCountLabel.Text = ((int)e.NewValue).ToString();
        }

        private void ReportDepthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var depth = (int)e.NewValue;
            ReportDepthLabel.Text = depth == 0 ? "All" : depth.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SCAN
        // ─────────────────────────────────────────────────────────────────────

        private async void StartScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning) return;

            var paths = GetAddedPaths();
            if (paths.Count == 0)
            {
                System.Windows.MessageBox.Show(
                    "Please add at least one folder path to scan.",
                    "No Paths", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _isScanning = true;
            _isCancelling = false;
            _lastScanResult = null;

            SetScanningState(true);
            UpdateProgress(0, "Starting scan...");
            StatsBlock.Visibility = Visibility.Collapsed;
            FolderTreeView.Items.Clear();
            FolderTreeView.Visibility = Visibility.Collapsed;
            TreePlaceholder.Visibility = Visibility.Visible;
            ExportPdfButton.IsEnabled = false;

            var settings = BuildScanSettings();

            // Always create a fresh ScanEngine and clean up previous handler
            if (_scanEngine != null && _progressHandler != null)
                _scanEngine.ProgressChanged -= _progressHandler;

            _scanEngine = new ScanEngine();

            _latestProgressPct = 0;
            _latestProgressMsg = "Starting scan...";

            // Background threads just write the latest values — no dispatcher involved
            _progressHandler = (s, args) =>
            {
                if (_isCancelling) return;
                _latestProgressPct = Math.Min(95, args.PercentComplete);
                var msg = TruncatePath(args.CurrentPath, 60);
                lock (_progressMsgLock) { _latestProgressMsg = msg; }
            };

            // UI timer polls those values on the UI thread — no queue accumulation
            _progressTimer?.Stop();
            _progressTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ProgressPollMs)
            };
            _progressTimer.Tick += (_, _) =>
            {
                if (!_isScanning) return;
                string msg;
                lock (_progressMsgLock) { msg = _latestProgressMsg; }
                UpdateProgress(_latestProgressPct, msg);
            };
            _progressTimer.Start();
            _scanEngine.ProgressChanged += _progressHandler;

            try
            {
                SetStatus($"Scanning {paths.Count} path(s)...");

                ScanResult? aggregatedResult = null;
                var scanStart = DateTime.Now;

                if (paths.Count == 1)
                {
                    // Task.Run ensures the scan starts on a thread-pool thread immediately,
                    // preventing any synchronous setup inside ScanFolderAsync from freezing the UI
                    aggregatedResult = await Task.Run(async () => await _scanEngine.ScanFolderAsync(paths[0], settings));
                    aggregatedResult?.UpdateTotals();
                }
                else
                {
                    aggregatedResult = new ScanResult { ScanStartTime = scanStart };
                    foreach (var path in paths)
                    {
                        var partialResult = await Task.Run(async () => await _scanEngine.ScanFolderAsync(path, settings));
                        if (partialResult == null) continue;

                        foreach (var root in partialResult.RootFolders)
                            aggregatedResult.AddRootFolder(root);
                        foreach (var p in partialResult.ScannedPaths)
                            aggregatedResult.AddScannedPath(p);
                    }
                    aggregatedResult.SetScanDuration(DateTime.Now);
                    aggregatedResult.UpdateTotals();
                }

                if (_isCancelling)
                {
                    // UI already updated immediately on cancel click — nothing to do
                }
                else if (aggregatedResult != null)
                {
                    _lastScanResult = aggregatedResult;
                    OnScanCompleted(aggregatedResult);
                }
                else
                {
                    SetStatus("Scan produced no results.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Scan failed:\n{ex.Message}",
                    "Scan Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("Scan failed.");
            }
            finally
            {
                _isScanning = false;
                SetScanningState(false);
            }
        }

        private void CancelScanButton_Click(object sender, RoutedEventArgs e)
        {
            // Resume first so paused tasks can see the cancellation token
            if (_isPaused)
            {
                _isPaused = false;
                _scanEngine?.Resume();
            }
            _isCancelling = true;
            _progressTimer?.Stop();
            _progressTimer = null;
            _scanEngine?.CancelScan();
            // Update UI immediately — don't wait for tasks to wind down
            SetStatus("Scan cancelled.");
            UpdateProgress(0, "Cancelled");
            CancelScanButton.IsEnabled = false;
            PauseScanButton.IsEnabled = false;
        }

        private void PauseScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                _isPaused = false;
                _scanEngine?.Resume();
                PauseScanButton.Content = "II";
                SetStatus("Scan resumed...");
            }
            else
            {
                _isPaused = true;
                _scanEngine?.Pause();
                PauseScanButton.Content = "▶";
                SetStatus("Scan paused.");
            }
        }

        private void OnScanCompleted(ScanResult result)
        {
            // Set _isScanning=false first so any timer tick already in the Dispatcher
            // queue sees it and returns early (timer tick checks if (!_isScanning) return)
            _isScanning = false;
            _latestProgressPct = 100;
            lock (_progressMsgLock) { _latestProgressMsg = "Scan complete"; }
            _progressTimer?.Stop();
            _progressTimer = null;
            UpdateProgress(100, "Scan complete");
            // ApplicationIdle fires only when the Dispatcher queue is fully empty —
            // guarantees no stale timer tick can overwrite the final value
            Dispatcher.InvokeAsync(() => UpdateProgress(100, "Scan complete"),
                System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            SetStatus($"Scan complete — {result.TotalFolders:N0} folders, {result.TotalFiles:N0} files in {result.ScanDuration.TotalSeconds:F1}s");

            TotalFoldersLabel.Text = result.TotalFolders.ToString("N0");
            TotalFilesLabel.Text = result.TotalFiles.ToString("N0");
            DurationLabel.Text = result.ScanDuration.TotalSeconds >= 60
                ? $"{(int)result.ScanDuration.TotalMinutes}m {result.ScanDuration.Seconds:D2}s"
                : $"{result.ScanDuration.TotalSeconds:F2}s";
            StatsBlock.Visibility = Visibility.Visible;

            PopulateTree(result);
            RefreshPreviewTabs(result);

            ExportPdfButton.IsEnabled = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TREE VIEW
        // ─────────────────────────────────────────────────────────────────────

        private void PopulateTree(ScanResult result)
        {
            FolderTreeView.Items.Clear();

            foreach (var rootFolder in result.RootFolders)
                FolderTreeView.Items.Add(BuildTreeItem(rootFolder, isRoot: true));

            if (FolderTreeView.Items.Count > 0)
            {
                TreePlaceholder.Visibility = Visibility.Collapsed;
                FolderTreeView.Visibility = Visibility.Visible;

                foreach (TreeViewItem item in FolderTreeView.Items)
                    item.IsExpanded = true;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PREVIEW TABS
        // ─────────────────────────────────────────────────────────────────────

        private void RightTabControl_Loaded(object sender, RoutedEventArgs e)
        {
            _tabHeaderScroll  = RightTabControl.Template.FindName("TabHeaderScroll",  RightTabControl) as System.Windows.Controls.ScrollViewer;
            _tabScrollLeftBtn = RightTabControl.Template.FindName("TabScrollLeftBtn", RightTabControl) as System.Windows.Controls.Button;
            _tabScrollRightBtn= RightTabControl.Template.FindName("TabScrollRightBtn",RightTabControl) as System.Windows.Controls.Button;

            if (_tabHeaderScroll != null)
                _tabHeaderScroll.ScrollChanged += (_, _) => UpdateTabNavButtons();

            UpdateTabNavButtons();
        }

        private void UpdateTabNavButtons()
        {
            if (_tabHeaderScroll == null) return;
            if (_tabScrollLeftBtn  != null)
                _tabScrollLeftBtn.Visibility  = _tabHeaderScroll.HorizontalOffset > 0
                    ? Visibility.Visible : Visibility.Collapsed;
            if (_tabScrollRightBtn != null)
                _tabScrollRightBtn.Visibility = _tabHeaderScroll.HorizontalOffset < _tabHeaderScroll.ScrollableWidth
                    ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TabScrollLeft_Click(object sender, RoutedEventArgs e)
            => _tabHeaderScroll?.ScrollToHorizontalOffset(
                Math.Max(0, _tabHeaderScroll.HorizontalOffset - 160));

        private void TabScrollRight_Click(object sender, RoutedEventArgs e)
        {
            if (_tabHeaderScroll == null) return;
            _tabHeaderScroll.ScrollToHorizontalOffset(
                Math.Min(_tabHeaderScroll.ScrollableWidth, _tabHeaderScroll.HorizontalOffset + 160));
        }

        private void RefreshPreviewTabs(ScanResult result)
        {
            // Remove old preview tabs
            foreach (var tab in _previewTabs)
                RightTabControl.Items.Remove(tab);
            _previewTabs.Clear();

            if (PreviewBeforeExportCheckBox.IsChecked != true) return;

            var maxDepth = (int)ReportDepthSlider.Value;
            int idx = 1;
            foreach (var root in result.RootFolders)
            {
                var content = new PreviewTabContent();
                content.Initialize(root, maxDepth);

                var tab = new TabItem { Content = content };
                tab.Header = BuildPreviewTabHeader($"scan {idx++}", tab);

                _previewTabs.Add(tab);
                RightTabControl.Items.Add(tab);
            }

            Dispatcher.InvokeAsync(UpdateTabNavButtons, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private System.Windows.FrameworkElement BuildPreviewTabHeader(string title, TabItem tab)
        {
            var panel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            panel.Children.Add(new System.Windows.Controls.TextBlock
            {
                Text = title,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });

            var closeBtn = new System.Windows.Controls.Button
            {
                Content = "×",
                Style = (System.Windows.Style)FindResource("TabCloseButton"),
                ToolTip = "Close preview"
            };
            closeBtn.Click += (_, _) =>
            {
                _previewTabs.Remove(tab);
                RightTabControl.Items.Remove(tab);
                Dispatcher.InvokeAsync(UpdateTabNavButtons, System.Windows.Threading.DispatcherPriority.Loaded);
            };
            panel.Children.Add(closeBtn);

            return panel;
        }

        private TreeViewItem BuildTreeItem(FolderInfo folder, bool isRoot = false)
        {
            var displayName = isRoot
                ? folder.FullPath
                : (string.IsNullOrEmpty(folder.Name) ? folder.FullPath : folder.Name);

            var header = $"📁 {displayName}  ({folder.SubFolders.Count} folders | {folder.FileCount} files)";

            var item = new TreeViewItem
            {
                Header = header,
                IsExpanded = false,
                ToolTip = folder.FullPath,
                Margin = isRoot ? new System.Windows.Thickness(0, 40, 0, 4) : new System.Windows.Thickness(0)
            };

            foreach (var sub in folder.SubFolders)
                item.Items.Add(BuildTreeItem(sub));

            return item;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  EXPORT
        // ─────────────────────────────────────────────────────────────────────


        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastScanResult == null) return;

            var roots = _lastScanResult.RootFolders;
            var reportDepth = (int)ReportDepthSlider.Value;
            var pdfOptions = new PdfExportOptions { MaxTreeDepth = reportDepth };

            // If preview tabs are open, export from their checked/renamed nodes
            if (_previewTabs.Count > 0)
            {
                await ExportFromPreviewTabsAsync(pdfOptions);
                return;
            }

            // Single root → one SaveFileDialog, one PDF (existing behaviour)
            if (roots.Count <= 1)
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Save PDF Report",
                    Filter = "PDF Files (*.pdf)|*.pdf",
                    FileName = BuildPdfFileName(_lastScanResult),
                    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };
                if (dialog.ShowDialog() != true) return;

                try
                {
                    ExportPdfButton.IsEnabled = false;
                    SetStatus("Exporting PDF...");
                    await new PdfExporter(pdfOptions).ExportAsync(_lastScanResult, dialog.FileName);
                    SetStatus($"PDF exported: {dialog.FileName}");
                    OpenPdfIfRequested(dialog.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Export failed:\n{ex.Message}",
                        "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SetStatus("PDF export failed.");
                }
                finally { ExportPdfButton.IsEnabled = _lastScanResult != null; }
                return;
            }

            // Multiple roots → one SaveFileDialog per root, one PDF each
            var exported = new List<string>();
            try
            {
                ExportPdfButton.IsEnabled = false;
                for (int i = 0; i < roots.Count; i++)
                {
                    var root = roots[i];
                    var singleResult = new ScanResult();
                    singleResult.AddRootFolder(root);
                    foreach (var p in _lastScanResult.ScannedPaths) singleResult.AddScannedPath(p);
                    singleResult.UpdateTotals();

                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = $"Save PDF – scan {i + 1} of {roots.Count}: {root.FullPath}",
                        Filter = "PDF Files (*.pdf)|*.pdf",
                        FileName = BuildPdfFileNameFromPath(root.FullPath),
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    };
                    if (dialog.ShowDialog() != true) break;

                    SetStatus($"Exporting PDF {i + 1}/{roots.Count}…");
                    await new PdfExporter(pdfOptions).ExportAsync(singleResult, dialog.FileName);
                    exported.Add(dialog.FileName);
                    OpenPdfIfRequested(dialog.FileName);
                }

                if (exported.Count > 0)
                {
                    SetStatus($"{exported.Count} PDF(s) exported.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed:\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("PDF export failed.");
            }
            finally { ExportPdfButton.IsEnabled = _lastScanResult != null; }
        }

        private async Task ExportFromPreviewTabsAsync(PdfExportOptions pdfOptions)
        {
            var exported = new List<string>();
            try
            {
                ExportPdfButton.IsEnabled = false;
                int total = _previewTabs.Count;
                for (int i = 0; i < total; i++)
                {
                    if (_previewTabs[i].Content is not PreviewTabContent previewContent) continue;

                    var filteredRoot = previewContent.BuildFilteredRoot();
                    if (filteredRoot == null) continue;

                    var singleResult = new ScanResult();
                    singleResult.AddRootFolder(filteredRoot);
                    if (_lastScanResult != null)
                        foreach (var p in _lastScanResult.ScannedPaths)
                            singleResult.AddScannedPath(p);
                    singleResult.UpdateTotals();

                    // Per-tab options (inherit base options + tab-specific IncludeHeader)
                    var tabOptions = new PdfExportOptions
                    {
                        MaxTreeDepth    = pdfOptions.MaxTreeDepth,
                        IncludeHeader   = previewContent.IncludeHeader,
                        IncludeFolderTree = pdfOptions.IncludeFolderTree
                    };

                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = total > 1 ? $"Save PDF — scan {i + 1} of {total}" : "Save PDF Report",
                        Filter = "PDF Files (*.pdf)|*.pdf",
                        FileName = BuildPdfFileNameFromPath(filteredRoot.FullPath),
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    };
                    if (dialog.ShowDialog() != true) continue;

                    SetStatus($"Exporting PDF {i + 1}/{total}…");
                    await new PdfExporter(tabOptions).ExportAsync(singleResult, dialog.FileName);
                    exported.Add(dialog.FileName);
                    OpenPdfIfRequested(dialog.FileName);
                }

                if (exported.Count > 0)
                {
                    SetStatus($"{exported.Count} PDF(s) exported.");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Export failed:\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("PDF export failed.");
            }
            finally { ExportPdfButton.IsEnabled = _lastScanResult != null; }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Opens the PDF with the default viewer if the user opted in.</summary>
        private void OpenPdfIfRequested(string path)
        {
            if (OpenAfterExportCheckBox.IsChecked != true) return;
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch { /* viewer not available — silently ignore */ }
        }

        private ScanSettings BuildScanSettings()
        {
            return new ScanSettings
            {
                MaxThreads = Math.Max(1, Math.Min(32, (int)ThreadsSlider.Value)),
                SkipHiddenFolders = SkipHiddenCheckBox.IsChecked == true,
                SkipSystemFolders = SkipSystemCheckBox.IsChecked == true,
                MaxDepth = 500,
                GlobalTimeout = TimeSpan.FromHours(5),
                DirectoryTimeout = TimeSpan.FromMinutes(2),
                NetworkDriveTimeout = TimeSpan.FromMinutes(5),
                LoggingOptions = new LoggingOptions { MinLevel = LogLevel.None }
            };
        }

        private List<string> GetAddedPaths()
        {
            var list = new List<string>();
            foreach (var item in PathsListBox.Items)
                if (item is string s) list.Add(s);
            return list;
        }

        private void UpdateStartButtonState()
        {
            StartScanButton.IsEnabled = PathsListBox.Items.Count > 0 && !_isScanning;
        }

        private void SetScanningState(bool scanning)
        {
            StartScanButton.IsEnabled = !scanning && PathsListBox.Items.Count > 0;
            CancelScanButton.IsEnabled = scanning;
            PauseScanButton.IsEnabled = scanning;
            if (!scanning)
            {
                // Reset pause button for next scan
                _isPaused = false;
                PauseScanButton.Content = "II";
            }
            AddPathButton.IsEnabled = !scanning;
            ThreadsSlider.IsEnabled = !scanning;
            SkipHiddenCheckBox.IsEnabled = !scanning;
            SkipSystemCheckBox.IsEnabled = !scanning;
            ReportDepthSlider.IsEnabled = !scanning;
        }

        private void UpdateProgress(int percent, string message)
        {
            ScanProgressBar.Value = Math.Min(100, Math.Max(0, percent));
            ProgressLabel.Text = message;
        }

        private void SetStatus(string message)
        {
            StatusBarLabel.Text = message;
        }

        /// Builds a default PDF filename from the first scanned root path.
        /// C:\Work                    → "FolderScan Report - C_Work.pdf"
        /// C:\Users\Pascal\Documents  → "FolderScan Report - C_..._Documents.pdf"
        private static string BuildPdfFileName(ScanResult result)
        {
            var path = result.RootFolders.FirstOrDefault()?.FullPath
                       ?? result.ScannedPaths.FirstOrDefault();
            return BuildPdfFileNameFromPath(path);
        }

        private static string BuildPdfFileNameFromPath(string? path)
        {

            if (string.IsNullOrEmpty(path))
                return "FolderScan Report.pdf";

            var parts = path.TrimEnd('\\', '/')
                            .Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

            var label = parts.Length <= 2
                ? path.TrimEnd('\\', '/')          // C:\Work  → "C:\Work"
                : $"{parts[0]}\\...\\{parts[^1]}"; // deeper   → "C:\...\Documents"

            // Sanitize characters not allowed in Windows filenames
            foreach (var c in Path.GetInvalidFileNameChars())
                label = label.Replace(c, '_');

            return $"FolderScan Report - {label}.pdf";
        }

        private static string TruncatePath(string path, int maxLength)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
                return path;

            // Show beginning and end with ellipsis in middle
            var half = (maxLength - 3) / 2;
            return path[..half] + "..." + path[^half..];
        }
    }
}
