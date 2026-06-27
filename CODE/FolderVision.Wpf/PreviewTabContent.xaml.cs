using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;  // TreeViewItem, DataTemplate
using System.Windows.Input;
using FolderVision.Models;
using FolderVision.Wpf.Models;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfTextBlock   = System.Windows.Controls.TextBlock;

namespace FolderVision.Wpf
{
    public partial class PreviewTabContent : WpfUserControl
    {
        public PreviewNode? RootNode { get; private set; }

        // Unchecked (empty box) = header included in PDF  |  Checked (×) = header excluded
        public bool IncludeHeader => IncludeHeaderCheckBox.IsChecked != true;

        /// <summary>Editable PDF title shown in the header bar. Defaults to the root path.</summary>
        public string PdfTitle => PdfTitleTextBox.Text;

        public void SetPdfTitle(string rootPath)
        {
            PdfTitleTextBox.Text = rootPath;
        }

        // Cached after first call to Initialize so BuildLazyItem doesn't call FindResource each time
        private DataTemplate? _headerTemplate;

        public PreviewTabContent()
        {
            InitializeComponent();
        }

        // ── Initialization ─────────────────────────────────────────────────────

        public async void Initialize(FolderInfo root, int maxDepth)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            PreviewTree.Visibility    = Visibility.Collapsed;
            PreviewTree.Items.Clear();

            // Build the full PreviewNode tree on a background thread (pure data, no UI)
            int depth = maxDepth > 0 ? maxDepth : int.MaxValue;
            RootNode = await Task.Run(() => BuildNode(root, 0, depth, isRoot: true));

            // Cache the header DataTemplate (keyed — not auto-applied by WPF)
            _headerTemplate ??= (DataTemplate)FindResource("PreviewNodeHeaderTemplate");

            // Add only the root as a lazy TreeViewItem; children are created on demand
            var rootItem = BuildLazyItem(RootNode);
            PreviewTree.Items.Add(rootItem);
            rootItem.IsExpanded = true;   // expand root → builds its direct children lazily

            LoadingOverlay.Visibility = Visibility.Collapsed;
            PreviewTree.Visibility    = Visibility.Visible;
        }

        // ── Lazy TreeViewItem building ─────────────────────────────────────────
        //
        // IMPORTANT: we do NOT use HierarchicalDataTemplate + ItemsSource binding.
        // That approach forces WPF to create containers for all descendants even when
        // nodes are collapsed, freezing the UI for large trees (36k+ nodes).
        // Instead we build TreeViewItem objects manually and populate children only
        // when the user expands a node (same pattern as MainWindow's folder tree).

        private TreeViewItem BuildLazyItem(PreviewNode node)
        {
            var item = new TreeViewItem
            {
                // Header = PreviewNode → DataContext inside HeaderTemplate = this node
                Header         = node,
                HeaderTemplate = _headerTemplate,
                IsExpanded     = false,
                Tag            = node,   // used by OnNodeExpanded to know which node to expand
                ToolTip        = node.IsRoot ? null : node.Source?.FullPath
            };

            if (node.Children.Count > 0)
            {
                // Placeholder keeps the expand arrow visible; replaced on first expand
                item.Items.Add(new TreeViewItem { Header = "…" });
                item.Expanded += OnNodeExpanded;
            }

            return item;
        }

        private void OnNodeExpanded(object sender, RoutedEventArgs e)
        {
            if (sender is not TreeViewItem item || item.Tag is not PreviewNode node) return;

            // Only act if placeholder is still present (avoid rebuilding on re-expand)
            if (item.Items.Count != 1
                || item.Items[0] is not TreeViewItem placeholder
                || placeholder.Header?.ToString() != "…")
                return;

            item.Expanded -= OnNodeExpanded;   // one-shot handler
            item.Items.Clear();

            foreach (var child in node.Children)
                item.Items.Add(BuildLazyItem(child));
        }

        // ── Data-model tree builder (runs on background thread) ───────────────

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
                foreach (var child in folder.SubFolders.OrderBy(f => f.Name))
                    node.Children.Add(BuildNode(child, depth + 1, maxDepth));

            return node;
        }

        // ── Export helper ──────────────────────────────────────────────────────

        /// <summary>Rebuilds a FolderInfo tree from only the checked/renamed nodes.</summary>
        public FolderInfo? BuildFilteredRoot()
        {
            if (RootNode == null || !RootNode.IsIncluded) return null;
            return BuildFiltered(RootNode);
        }

        private static FolderInfo? BuildFiltered(PreviewNode node)
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
                var filtered = BuildFiltered(child);
                if (filtered != null) folder.SubFolders.Add(filtered);
            }

            folder.SubFolderCount = folder.SubFolders.Count;
            return folder;
        }

        // ── Bulk-uncheck children ──────────────────────────────────────────────

        private void UncheckChildren_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is PreviewNode node)
            {
                if (!node.IsIncluded) node.IsIncluded = true;
                foreach (var child in node.Children)
                    if (!child.IsIncluded) child.IsIncluded = true;
            }
        }

        // ── Double-click: invert all descendants ───────────────────────────────

        private void NodeLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) return;
            e.Handled = true;

            if (sender is WpfTextBlock label && label.DataContext is PreviewNode node)
            {
                InvertDescendants(node);
                if (node.IsIncluded && node.Children.Any(c => !c.IsIncluded))
                    node.InvertIncluded();
            }
        }

        private static void InvertDescendants(PreviewNode node)
        {
            foreach (var child in node.Children)
            {
                child.InvertIncluded();
                InvertDescendants(child);
            }
        }
    }
}
