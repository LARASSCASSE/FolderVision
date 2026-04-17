using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using FolderVision.Models;
using FolderVision.Wpf.Models;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfStackPanel = System.Windows.Controls.StackPanel;

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

        public void Initialize(FolderInfo root, int maxDepth)
        {
            RootNode = BuildNode(root, 0, maxDepth > 0 ? maxDepth : int.MaxValue, isRoot: true);
            PreviewTree.Items.Clear();
            PreviewTree.Items.Add(RootNode);
            if (PreviewTree.Items[0] is System.Windows.Controls.TreeViewItem tvi) tvi.IsExpanded = true;
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

        // ── Inline editing ─────────────────────────────────────────────────────

        private void NodeLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2) return;
            e.Handled = true;

            if (sender is WpfTextBlock label && label.Parent is WpfStackPanel panel)
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
                    tb.Text = node.DisplayName;
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
    }
}
