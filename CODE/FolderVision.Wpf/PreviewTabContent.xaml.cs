using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FolderVision.Models;
using FolderVision.Wpf.Models;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace FolderVision.Wpf
{
    public partial class PreviewTabContent : WpfUserControl
    {
        public PreviewNode? RootNode { get; private set; }

        // Unchecked (empty box) = header included in PDF  |  Checked (×) = header excluded
        public bool IncludeHeader => IncludeHeaderCheckBox.IsChecked != true;

        public PreviewTabContent()
        {
            InitializeComponent();
        }

        public async void Initialize(FolderInfo root, int maxDepth)
        {
            // Show spinner while building the node tree on a background thread
            LoadingOverlay.Visibility = Visibility.Visible;
            PreviewTree.Visibility    = Visibility.Collapsed;
            PreviewTree.Items.Clear();

            int depth = maxDepth > 0 ? maxDepth : int.MaxValue;
            RootNode = await Task.Run(() => BuildNode(root, 0, depth, isRoot: true));

            PreviewTree.Items.Add(RootNode);
            LoadingOverlay.Visibility = Visibility.Collapsed;
            PreviewTree.Visibility    = Visibility.Visible;
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
                foreach (var child in folder.SubFolders.OrderBy(f => f.Name))
                    node.Children.Add(BuildNode(child, depth + 1, maxDepth));

            return node;
        }

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
                SubFolders   = new System.Collections.Generic.List<FolderInfo>()
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
                // "Décocher" = remove the × = set IsIncluded back to true
                // Also uncheck the parent itself
                if (!node.IsIncluded) node.IsIncluded = true;
                // Then uncheck all children that are currently checked (IsIncluded=false)
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
                // If parent is still checked but some children are now unchecked → uncheck parent too
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
