using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using FolderVision.Models;

namespace FolderVision.Wpf.Models
{
    public class PreviewNode : INotifyPropertyChanged
    {
        private bool _isIncluded = true;
        private bool _isExpanded = true;
        private string _displayName = string.Empty;
        private ObservableCollection<PreviewNode> _children = new();

        public string OriginalName { get; set; } = string.Empty;
        public int Depth { get; set; }
        public bool IsRoot { get; set; }
        public FolderInfo Source { get; set; } = null!;

        public ObservableCollection<PreviewNode> Children
        {
            get => _children;
            set
            {
                // Unsubscribe old
                if (_children != null)
                {
                    _children.CollectionChanged -= Children_CollectionChanged;
                    foreach (var c in _children) c.PropertyChanged -= Child_PropertyChanged;
                }
                _children = value;
                if (_children != null)
                {
                    _children.CollectionChanged += Children_CollectionChanged;
                    foreach (var c in _children) c.PropertyChanged += Child_PropertyChanged;
                }
                OnPropertyChanged();
                OnPropertyChanged(nameof(ShowUncheckButton));
            }
        }

        private void Children_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (PreviewNode c in e.NewItems) c.PropertyChanged += Child_PropertyChanged;
            if (e.OldItems != null)
                foreach (PreviewNode c in e.OldItems) c.PropertyChanged -= Child_PropertyChanged;
            OnPropertyChanged(nameof(ShowUncheckButton));
        }

        private void Child_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsIncluded))
                OnPropertyChanged(nameof(ShowUncheckButton));
        }

        /// <summary>
        /// True when >5 children AND at least 5 of them are checked (× = IsIncluded=false).
        /// "Coché" in the UI = checkbox shows × = IsIncluded is false.
        /// </summary>
        public bool ShowUncheckButton =>
            _children.Count > 5 && _children.Count(c => !c.IsIncluded) >= 5;

        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; OnPropertyChanged(); }
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { _isExpanded = value; OnPropertyChanged(); }
        }

        public bool IsIncluded
        {
            get => _isIncluded;
            set
            {
                if (_isIncluded == value) return;
                _isIncluded = value;
                OnPropertyChanged();
                if (!value)
                {
                    IsExpanded = false;
                    foreach (var child in _children)
                        child.IsIncluded = false;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
