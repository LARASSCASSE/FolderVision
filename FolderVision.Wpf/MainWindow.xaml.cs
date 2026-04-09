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

        public MainWindow()
        {
            InitializeComponent();
            PathsListBox.SelectionChanged += PathsListBox_SelectionChanged;
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
            {
                PathInputBox.Text = dialog.SelectedPath;
            }
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
                    "Invalid Path",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            // Avoid duplicates
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
            RemovePathButton.IsEnabled = PathsListBox.SelectedItem != null;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SETTINGS
        // ─────────────────────────────────────────────────────────────────────

        private void ThreadsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ThreadCountLabel != null)
                ThreadCountLabel.Text = ((int)e.NewValue).ToString();
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
                    "No Paths",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _isScanning = true;
            _lastScanResult = null;

            // Reset UI
            SetScanningState(true);
            UpdateProgress(0, "Starting scan...");
            StatsBlock.Visibility = Visibility.Collapsed;
            FolderTreeView.Items.Clear();
            FolderTreeView.Visibility = Visibility.Collapsed;
            TreePlaceholder.Visibility = Visibility.Visible;
            ExportHtmlButton.IsEnabled = false;
            ExportPdfButton.IsEnabled = false;

            var settings = BuildScanSettings(paths);
            _scanEngine = new ScanEngine();

            _scanEngine.ProgressChanged += (s, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateProgress(args.PercentComplete, TruncatePath(args.CurrentPath, 60));
                });
            };

            try
            {
                SetStatus($"Scanning {paths.Count} path(s)...");
                ScanResult? aggregatedResult = null;

                if (paths.Count == 1)
                {
                    aggregatedResult = await _scanEngine.ScanFolderAsync(paths[0], settings);
                }
                else
                {
                    // Multiple paths: scan each sequentially and merge
                    aggregatedResult = new ScanResult { ScanStartTime = DateTime.Now };
                    foreach (var path in paths)
                    {
                        var partialResult = await _scanEngine.ScanFolderAsync(path, settings);
                        foreach (var root in partialResult.RootFolders)
                            aggregatedResult.AddRootFolder(root);
                        foreach (var p in partialResult.ScannedPaths)
                            aggregatedResult.AddScannedPath(p);
                    }
                    aggregatedResult.SetScanDuration(DateTime.Now);
                    aggregatedResult.UpdateTotals();
                }

                _lastScanResult = aggregatedResult;
                OnScanCompleted(aggregatedResult);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Scan failed:\n{ex.Message}",
                    "Scan Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

            // Stats block
            TotalFoldersLabel.Text = result.TotalFolders.ToString("N0");
            TotalFilesLabel.Text = result.TotalFiles.ToString("N0");
            DurationLabel.Text = $"{result.ScanDuration.TotalSeconds:F2}s";
            StatsBlock.Visibility = Visibility.Visible;

            // Populate tree
            PopulateTree(result);

            // Enable export buttons
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
            {
                var rootItem = BuildTreeItem(rootFolder, isRoot: true);
                FolderTreeView.Items.Add(rootItem);
            }

            if (FolderTreeView.Items.Count > 0)
            {
                TreePlaceholder.Visibility = Visibility.Collapsed;
                FolderTreeView.Visibility = Visibility.Visible;

                // Expand top-level items
                foreach (TreeViewItem item in FolderTreeView.Items)
                    item.IsExpanded = true;
            }
        }

        private TreeViewItem BuildTreeItem(FolderInfo folder, bool isRoot = false)
        {
            var displayName = isRoot ? folder.FullPath : folder.Name;
            var header = $"📁 {displayName}  ({folder.SubFolders.Count} folders | {folder.FileCount} files)";

            var item = new TreeViewItem
            {
                Header = header,
                IsExpanded = false,
                ToolTip = folder.FullPath
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
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Export failed:\n{ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                SetStatus("HTML export failed.");
            }
            finally
            {
                ExportHtmlButton.IsEnabled = true;
            }
        }

        private async void ExportPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (_lastScanResult == null) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save PDF Report",
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = "FolderScan_Report.pdf",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                ExportPdfButton.IsEnabled = false;
                SetStatus("Exporting PDF...");
                var exporter = new PdfExporter();
                await exporter.ExportAsync(_lastScanResult, dialog.FileName);
                SetStatus($"PDF exported: {dialog.FileName}");
                System.Windows.MessageBox.Show(
                    $"PDF report saved to:\n{dialog.FileName}",
                    "Export Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Export failed:\n{ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                SetStatus("PDF export failed.");
            }
            finally
            {
                ExportPdfButton.IsEnabled = true;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private ScanSettings BuildScanSettings(List<string> paths)
        {
            var settings = new ScanSettings
            {
                MaxThreads = (int)ThreadsSlider.Value,
                SkipHiddenFolders = SkipHiddenCheckBox.IsChecked == true,
                SkipSystemFolders = SkipSystemCheckBox.IsChecked == true,
                MaxDepth = 500,
                GlobalTimeout = TimeSpan.FromHours(5),
                DirectoryTimeout = TimeSpan.FromMinutes(2),
                NetworkDriveTimeout = TimeSpan.FromMinutes(5),
                LoggingOptions = new LoggingOptions { MinLevel = LogLevel.None }
            };

            foreach (var p in paths)
                settings.PathsToScan.Add(p);

            return settings;
        }

        private List<string> GetAddedPaths()
        {
            var list = new List<string>();
            foreach (var item in PathsListBox.Items)
            {
                if (item is string s)
                    list.Add(s);
            }
            return list;
        }

        private void UpdateStartButtonState()
        {
            StartScanButton.IsEnabled = PathsListBox.Items.Count > 0 && !_isScanning;
        }

        private void SetScanningState(bool scanning)
        {
            _isScanning = scanning;
            StartScanButton.IsEnabled = !scanning && PathsListBox.Items.Count > 0;
            CancelScanButton.IsEnabled = scanning;
            AddPathButton.IsEnabled = !scanning;
            BrowseButton.IsEnabled = !scanning;
            RemovePathButton.IsEnabled = !scanning && PathsListBox.SelectedItem != null;
            PathInputBox.IsEnabled = !scanning;
            ThreadsSlider.IsEnabled = !scanning;
            SkipHiddenCheckBox.IsEnabled = !scanning;
            SkipSystemCheckBox.IsEnabled = !scanning;
        }

        private void UpdateProgress(int percent, string message)
        {
            ScanProgressBar.Value = percent;
            ProgressLabel.Text = message;
        }

        private void SetStatus(string message)
        {
            StatusBarLabel.Text = message;
        }

        private static string TruncatePath(string path, int maxLength)
        {
            if (string.IsNullOrEmpty(path) || path.Length <= maxLength)
                return path;
            return "..." + path[^(maxLength - 3)..];
        }
    }
}
