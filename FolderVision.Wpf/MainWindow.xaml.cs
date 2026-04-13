using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
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

        // Track current handler to avoid stale subscriptions
        private EventHandler<ProgressEventArgs>? _progressHandler;

        public MainWindow()
        {
            InitializeComponent();
            PathsListBox.SelectionChanged += PathsListBox_SelectionChanged;
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
            PathsListBox.SelectionChanged -= PathsListBox_SelectionChanged;
            if (_scanEngine != null && _progressHandler != null)
                _scanEngine.ProgressChanged -= _progressHandler;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PATH MANAGEMENT
        // ─────────────────────────────────────────────────────────────────────

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select a folder to scan",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                PathInputBox.Text = dialog.SelectedPath;
        }

        private void AddPathButton_Click(object sender, RoutedEventArgs e)
        {
            var path = PathInputBox.Text.Trim();
            if (string.IsNullOrEmpty(path))
            {
                SetStatus("Please enter or browse for a folder path.");
                return;
            }

            if (!Directory.Exists(path))
            {
                SetStatus($"Path does not exist: {path}");
                System.Windows.MessageBox.Show(
                    $"The path does not exist:\n{path}",
                    "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in PathsListBox.Items)
            {
                if (string.Equals(item.ToString(), path, StringComparison.OrdinalIgnoreCase))
                {
                    SetStatus("Path already in list.");
                    return;
                }
            }

            PathsListBox.Items.Add(path);
            PathInputBox.Clear();
            UpdateStartButtonState();
            SetStatus($"Added: {path}");
        }

        private void RemovePathButton_Click(object sender, RoutedEventArgs e)
        {
            if (PathsListBox.SelectedItem is string selected)
            {
                PathsListBox.Items.Remove(selected);
                UpdateStartButtonState();
                SetStatus("Path removed.");
            }
        }

        private void PathsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RemovePathButton.IsEnabled = PathsListBox.SelectedItem != null && !_isScanning;
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
            _lastScanResult = null;

            SetScanningState(true);
            UpdateProgress(0, "Starting scan...");
            StatsBlock.Visibility = Visibility.Collapsed;
            FolderTreeView.Items.Clear();
            FolderTreeView.Visibility = Visibility.Collapsed;
            TreePlaceholder.Visibility = Visibility.Visible;
            ExportHtmlButton.IsEnabled = false;
            ExportPdfButton.IsEnabled = false;

            var settings = BuildScanSettings();

            // Always create a fresh ScanEngine and clean up previous handler
            if (_scanEngine != null && _progressHandler != null)
                _scanEngine.ProgressChanged -= _progressHandler;

            _scanEngine = new ScanEngine();

            _progressHandler = (s, args) =>
            {
                var pct = Math.Min(100, args.PercentComplete);
                var msg = TruncatePath(args.CurrentPath, 60);
                Dispatcher.InvokeAsync(() => UpdateProgress(pct, msg));
            };
            _scanEngine.ProgressChanged += _progressHandler;

            try
            {
                SetStatus($"Scanning {paths.Count} path(s)...");

                ScanResult? aggregatedResult = null;
                var scanStart = DateTime.Now;

                if (paths.Count == 1)
                {
                    aggregatedResult = await _scanEngine.ScanFolderAsync(paths[0], settings);
                    aggregatedResult?.UpdateTotals();
                }
                else
                {
                    aggregatedResult = new ScanResult { ScanStartTime = scanStart };
                    foreach (var path in paths)
                    {
                        var partialResult = await _scanEngine.ScanFolderAsync(path, settings);
                        if (partialResult == null) continue;

                        foreach (var root in partialResult.RootFolders)
                            aggregatedResult.AddRootFolder(root);
                        foreach (var p in partialResult.ScannedPaths)
                            aggregatedResult.AddScannedPath(p);
                    }
                    aggregatedResult.SetScanDuration(DateTime.Now);
                    aggregatedResult.UpdateTotals();
                }

                if (aggregatedResult != null)
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
            _scanEngine?.CancelScan();
            SetStatus("Cancelling scan...");
            CancelScanButton.IsEnabled = false;
        }

        private void OnScanCompleted(ScanResult result)
        {
            UpdateProgress(100, "Scan complete");
            SetStatus($"Scan complete — {result.TotalFolders:N0} folders, {result.TotalFiles:N0} files in {result.ScanDuration.TotalSeconds:F1}s");

            TotalFoldersLabel.Text = result.TotalFolders.ToString("N0");
            TotalFilesLabel.Text = result.TotalFiles.ToString("N0");
            DurationLabel.Text = result.ScanDuration.TotalSeconds >= 60
                ? $"{(int)result.ScanDuration.TotalMinutes}m {result.ScanDuration.Seconds:D2}s"
                : $"{result.ScanDuration.TotalSeconds:F2}s";
            StatsBlock.Visibility = Visibility.Visible;

            PopulateTree(result);

            ExportHtmlButton.IsEnabled = true;
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

        private async void ExportHtmlButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastScanResult == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save HTML Report",
                Filter = "HTML Files (*.html)|*.html",
                FileName = "FolderScan_Report.html",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                ExportHtmlButton.IsEnabled = false;
                SetStatus("Exporting HTML...");
                var exporter = new HtmlExporter();
                await exporter.ExportAsync(_lastScanResult, dialog.FileName);
                SetStatus($"HTML exported: {dialog.FileName}");
                System.Windows.MessageBox.Show(
                    $"HTML report saved to:\n{dialog.FileName}",
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Export failed:\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                SetStatus("HTML export failed.");
            }
            finally
            {
                ExportHtmlButton.IsEnabled = _lastScanResult != null;
            }
        }

        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastScanResult == null) return;

            var roots = _lastScanResult.RootFolders;
            var reportDepth = (int)ReportDepthSlider.Value;
            var pdfOptions = new PdfExportOptions { MaxTreeDepth = reportDepth };

            // If "Preview before export" is checked, open the interactive preview window
            if (PreviewBeforeExportCheckBox.IsChecked == true)
            {
                var preview = new ExportPreviewWindow(_lastScanResult, pdfOptions) { Owner = this };
                preview.ShowDialog();
                return; // ExportPreviewWindow handles SaveFileDialogs and PDF generation
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
                    System.Windows.MessageBox.Show(
                        $"PDF report saved to:\n{dialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
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
                }

                if (exported.Count > 0)
                {
                    SetStatus($"{exported.Count} PDF(s) exported.");
                    System.Windows.MessageBox.Show(
                        $"{exported.Count} PDF report(s) saved:\n" + string.Join("\n", exported),
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
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
            AddPathButton.IsEnabled = !scanning;
            BrowseButton.IsEnabled = !scanning;
            RemovePathButton.IsEnabled = !scanning && PathsListBox.SelectedItem != null;
            PathInputBox.IsEnabled = !scanning;
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
