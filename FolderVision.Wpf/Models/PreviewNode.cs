using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using FolderVision.Models;

namespace FolderVision.Wpf.Models
{
    public class PreviewNode : INotifyPropertyChanged
    {
        private bool _isIncluded = true;
        private string _displayName = string.Empty;

        public string OriginalName { get; set; } = string.Empty;
        public int Depth { get; set; }
        public bool IsRoot { get; set; }
        public FolderInfo Source { get; set; } = null!;
        public ObservableCollection<PreviewNode> Children { get; set; } = new();

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public bool IsIncluded
        {
            get => _isIncluded;
            set
            {
                if (_isIncluded == value) return;
                _isIncluded = value;
                OnPropertyChanged();
                // Cascade to children
                if (!value)
                    foreach (var child in Children)
                        child.IsIncluded = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
