using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using FolderVision.Models;
using FolderVision.Wpf.Models;

namespace FolderVision.Wpf
{
    public partial class DuplicatesTabContent : System.Windows.Controls.UserControl
    {
        private readonly List<PreviewNode> _groupNodes = new();

        public DuplicatesTabContent()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Populate the tree from duplicate groups.
        /// Key = folder name, Value = sorted list of full paths.
        /// </summary>
        public void Initialize(Dictionary<string, List<string>> duplicateGroups)
        {
            _groupNodes.Clear();
            DuplicatesTree.Items.Clear();

            var count = duplicateGroups.Count;
            GroupCountLabel.Text = $"{count} duplicate group{(count > 1 ? "s" : "")}";

            // Sort: largest groups first, then alphabetically
            foreach (var kvp in duplicateGroups
                .OrderByDescending(k => k.Value.Count)
                .ThenBy(k => k.Key, System.StringComparer.OrdinalIgnoreCase))
            {
                var groupName = kvp.Key;
                var paths     = kvp.Value.OrderBy(p => p).ToList();
                var label     = paths.Count == 2 ? "duplicate"
                              : paths.Count == 3 ? "triple"
                              : $"{paths.Count} occurrences";

                var groupNode = new PreviewNode
                {
                    OriginalName = groupName,
                    DisplayName  = $"{groupName}   ({label})",
                    IsRoot       = true,   // drives the red-purple label style
                    IsExpanded   = true,
                    Source       = new FolderInfo { Name = groupName, FullPath = groupName },
                    Children     = new ObservableCollection<PreviewNode>()
                };

                foreach (var path in paths)
                {
                    groupNode.Children.Add(new PreviewNode
                    {
                        OriginalName = path,
                        DisplayName  = path,
                        IsRoot       = false,
                        IsExpanded   = false,
                        Source       = new FolderInfo { Name = path, FullPath = path }
                    });
                }

                _groupNodes.Add(groupNode);
                DuplicatesTree.Items.Add(groupNode);
            }
        }

        /// <summary>
        /// Returns only the checked groups with their checked paths.
        /// Groups or paths that the user unchecked (×) are excluded.
        /// </summary>
        public Dictionary<string, List<string>> GetSelectedGroups()
        {
            var result = new Dictionary<string, List<string>>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var groupNode in _groupNodes)
            {
                if (!groupNode.IsIncluded) continue;
                var paths = groupNode.Children
                    .Where(c => c.IsIncluded)
                    .Select(c => c.OriginalName)
                    .ToList();
                if (paths.Count >= 2)
                    result[groupNode.OriginalName] = paths;
            }
            return result;
        }

        // Bulk-uncheck children button (same logic as PreviewTabContent)
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
    }
}
