using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;

namespace AcManager.Controls.UserControls {
    [ContentProperty(nameof(Content))]
    public partial class ModernPopup : Avalonia.Controls.Primitives.Popup {
        public ModernPopup() {
            InitializeComponent();
            Root.DataContext = this;
            Placement = PlacementMode.Bottom;
        }

        /// <summary>WPF’s StaysOpen is the opposite of light dismiss.</summary>
        public static readonly StyledProperty<bool> StaysOpenProperty = Avalonia.AvaloniaProperty.Register<ModernPopup, bool>(nameof(StaysOpen), true);

        public bool StaysOpen {
            get => GetValue(StaysOpenProperty);
            set => SetValue(StaysOpenProperty, value);
        }

        protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == StaysOpenProperty) {
                IsLightDismissEnabled = !(bool)change.NewValue;
            }
        }

        public static readonly StyledProperty<Thickness> PaddingProperty = DP.Register<Thickness>(nameof(Padding),
                typeof(ModernPopup), new PropertyMetadata(new Thickness(4d)));

        public Thickness Padding {
            get => GetValue(PaddingProperty) as Thickness? ?? default;
            set => SetValue(PaddingProperty, value);
        }

        public static readonly StyledProperty<object> ContentProperty = DP.Register<object>(nameof(Content),
                typeof(ModernPopup), new PropertyMetadata(OnContentChanged));

        public object Content {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        private static void OnContentChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernPopup)o).OnContentChanged(e.OldValue as FrameworkElement, e.NewValue as FrameworkElement);
        }

        private void OnContentChanged(FrameworkElement oldValue, FrameworkElement newValue) {
            if (oldValue != null) {
                oldValue.ClearValue(Avalonia.StyledElement.DataContextProperty);
            }

            if (newValue != null && newValue.DataContext == null) {
                newValue.SetBinding(Avalonia.StyledElement.DataContextProperty, new Binding {
                    Path = new PropertyPath(nameof(DataContext)),
                    Source = this
                });
            }
        }
    }
}
