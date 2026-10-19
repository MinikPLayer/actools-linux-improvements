using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using FirstFloor.ModernUI.Presentation;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    public interface IShortDisplayable {
        string ShortDisplayName { get; }
    }

    public class HierarchicalGroup : BetterObservableCollection<object> {
        public HierarchicalGroup() { }

        public HierarchicalGroup(string displayName, List<object> list) : base(list) {
            _displayName = displayName;
        }

        public HierarchicalGroup(string displayName, [NotNull] IEnumerable<object> collection) : base(collection) {
            _displayName = displayName;
        }

        public HierarchicalGroup(List<object> list) : base(list) { }

        public HierarchicalGroup([NotNull] IEnumerable<object> collection) : base(collection) { }

        public HierarchicalGroup(string displayName) {
            _displayName = displayName;
        }

        public IValueConverter HeaderConverter { get; set; }

        private string _displayName;

        public string DisplayName {
            get => _displayName;
            set {
                if (Equals(value, _displayName)) return;
                _displayName = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(DisplayName)));
            }
        }
    }
}
