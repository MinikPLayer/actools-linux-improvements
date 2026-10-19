using System;
using System.Globalization;
using Avalonia.Controls.Documents;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Text block which displays a dimmed placeholder when text is empty.
    /// </summary>
    public class PlaceholderTextBlock : TextBlock {
        public static readonly DependencyProperty PlaceholderProperty = DP.Register(nameof(Placeholder), typeof(string),
                typeof(PlaceholderTextBlock), new PropertyMetadata(OnPlaceholderChanged));

        public string Placeholder {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        private static void OnPlaceholderChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((PlaceholderTextBlock)o).Update();
        }

        protected override Type StyleKeyOverride => typeof(TextBlock);

        protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == TextProperty) {
                Update();
            }
        }

        private static readonly ValueConverter Converter = new ValueConverter();

        private class ValueConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                if (!(value is ISolidColorBrush brush)) return Brushes.Transparent;
                var color = brush.Color;
                return new SolidColorBrush(Color.FromArgb((byte)(color.A / 2.7), color.R, color.G, color.B));
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                throw new NotSupportedException();
            }
        }

        public static Inline GetPlaceholder(Control target, string text) {
            var inline = new Run { Text = text };
            inline.Bind(TextElement.ForegroundProperty, new Avalonia.Data.Binding {
                Path = nameof(Foreground),
                Source = target,
                Converter = Converter
            });
            return inline;
        }

        private bool _placeholderShown;

        private void Update() {
            if (string.IsNullOrEmpty(Text)) {
                var placeholder = Placeholder;
                if (!string.IsNullOrEmpty(placeholder)) {
                    Inlines?.Clear();
                    Inlines?.Add(GetPlaceholder(this, placeholder));
                    _placeholderShown = true;
                }
            } else if (_placeholderShown) {
                _placeholderShown = false;
            }
        }
    }
}
