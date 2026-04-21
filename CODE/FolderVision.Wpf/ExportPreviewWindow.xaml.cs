using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfMessageBox = System.Windows.MessageBox;
using FolderVision.Exporters;
using FolderVision.Models;
using FolderVision.Wpf.Models;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfStackPanel = System.Windows.Controls.StackPanel;

namespace FolderVision.Wpf
{
    public partial class ExportPreviewWindow : Window
    {
        private readonly ScanResult _scanResult;
        private readonly PdfExportOptions _pdfOptions;
        private readonly List<PreviewNode> _roots = new();

        public ExportPreviewWindow(ScanResult scanResult, PdfExportOptions pdfOptions)
        {
            _scanResult = scanResult;
            _pdfOptions = pdfOptions;

            InitializeComponent();
            BuildTree();
        }

        // ── Tree construction ──────────────────────────────────────────────────

        private void BuildTree()
        {
            int maxDepth = _pdfOptions.MaxTreeDepth > 0 ? _pdfOptions.MaxTreeDepth : int.MaxValue;

            foreach (var root in _scanResult.RootFolders)
            {
                var node = BuildNode(root, depth: 0, maxDepth: maxDepth, isRoot: true);
                _roots.Add(node);
                PreviewTree.Items.Add(node);
            }

            int total = _roots.Sum(r => CountNodes(r));
            SubtitleLabel.Text =
                $"{total} folder(s) — uncheck to exclude, double-click a name to rename.";
        }

        private static PreviewNode BuildNode(FolderInfo folder, int depth, int maxDepth, bool isRoot = false)
        {
            var node = new PreviewNode
            {
                OriginalName = isRoot ? folder.FullPath : folder.Name,
                DisplayName  = isRoot ? folder.FullPath : folder.Name,
                Depth        = depth,
                IsRoot       = isRoot,
                Source       = folder,
                Children     = new ObservableCollection<PreviewNode>()
            };

            if (depth < maxDepth)
            {
                foreach (var child in folder.SubFolders.OrderBy(f => f.Name))
                    node.Children.Add(BuildNode(child, depth + 1, maxDepth));
            }

            return node;
        }

        private static int CountNodes(PreviewNode node)
            => 1 + node.Children.Sum(c => CountNodes(c));

        // ── Inline editing ────────────────────────────────────────────────────

        private void NodeLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) return;
            e.Handled = true;

            if (sender is WpfTextBlock label &&
                label.Parent is WpfStackPanel panel)
            {
                var editor = panel.Children.OfType<WpfTextBox>().FirstOrDefault();
                if (editor == null) return;

                label.Visibility = Visibility.Collapsed;
                editor.Visibility = Visibility.Visible;
                editor.SelectAll();
                editor.Focus();
            }
        }

        private void NodeEditor_LostFocus(object sender, RoutedEventArgs e)
            => CommitEdit(sender as WpfTextBox);

        private void NodeEditor_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key is Key.Enter or Key.Escape)
            {
                if (e.Key == Key.Escape && sender is WpfTextBox tb && tb.DataContext is PreviewNode node)
                    tb.Text = node.DisplayName; // revert
                CommitEdit(sender as WpfTextBox);
                e.Handled = true;
            }
        }

        private static void CommitEdit(WpfTextBox? editor)
        {
            if (editor == null) return;
            editor.Visibility = Visibility.Collapsed;

            var panel = editor.Parent as WpfStackPanel;
            var label = panel?.Children.OfType<WpfTextBlock>().FirstOrDefault();
            if (label != null) label.Visibility = Visibility.Visible;
        }

        // ── Export ────────────────────────────────────────────────────────────

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var includedRoots = _roots.Where(r => r.IsIncluded).ToList();
            if (includedRoots.Count == 0)
            {
                WpfMessageBox.Show("No folders are selected. Please check at least one folder.",
                    "Nothing to Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExportButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            var exported = new List<string>();
            try
            {
                foreach (var rootNode in includedRoots)
                {
                    var filteredFolder = BuildFilteredFolderInfo(rootNode);
                    if (filteredFolder == null) continue;

                    var singleResult = new ScanResult();
                    singleResult.AddRootFolder(filteredFolder);
                    foreach (var p in _scanResult.ScannedPaths)
                        singleResult.AddScannedPath(p);
                    singleResult.UpdateTotals();

                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Title = includedRoots.Count > 1
                            ? $"Save PDF — {rootNode.DisplayName}"
                            : "Save PDF Report",
                        Filter = "PDF Files (*.pdf)|*.pdf",
                        FileName = BuildPdfFileName(rootNode.DisplayName),
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    };

                    if (dialog.ShowDialog(this) != true) continue;

                    await Task.Run(() =>
                        new PdfExporter(_pdfOptions).ExportAsync(singleResult, dialog.FileName));

                    exported.Add(dialog.FileName);
                }

                if (exported.Count > 0)
                {
                    WpfMessageBox.Show(
                        exported.Count == 1
                            ? $"PDF report saved to:\n{exported[0]}"
                            : $"{exported.Count} PDF report(s) saved:\n" + string.Join("\n", exported),
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                Close();
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Export failed:\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ExportButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
            => Close();

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds a FolderInfo from a PreviewNode tree, using edited DisplayNames
        /// and skipping unchecked nodes. Returns null if the node itself is unchecked.
        /// </summary>
        private static FolderInfo? BuildFilteredFolderInfo(PreviewNode node)
        {
            if (!node.IsIncluded) return null;

            var folder = new FolderInfo
            {
                FullPath     = node.Source.FullPath,
                Name         = node.DisplayName,
                FileCount    = node.Source.FileCount,
                LastModified = node.Source.LastModified,
                SubFolders   = new List<FolderInfo>()
            };

            foreach (var child in node.Children)
            {
                var filtered = BuildFilteredFolderInfo(child);
                if (filtered != null)
                    folder.SubFolders.Add(filtered);
            }

            folder.SubFolderCount = folder.SubFolders.Count;
            return folder;
        }

        private static string BuildPdfFileName(string folderPath)
        {
            var name = Path.GetFileName(folderPath.TrimEnd('\\', '/'));
            if (string.IsNullOrEmpty(name)) name = folderPath.Replace(":", "").Replace("\\", "_").Replace("/", "_");
            var safe = string.Concat(name.Split(Path.GetInvalidFileNameChars()));
            return $"FolderVision_{safe}_{DateTime.Now:yyyyMMdd}.pdf";
        }
    }
}
