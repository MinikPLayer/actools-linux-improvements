using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FirstFloor.ModernUI.Helpers;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Text field with a hex color and a button with popup. WPF version used pixel shaders for palettes, here it’s Avalonia’s ColorView
    /// (see LinuxPortDocs.md).
    /// </summary>
    public class ColorPicker : TemplatedControl {
        public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<ColorPicker, Color>(nameof(Color),
                Color.FromArgb(0, 0, 0, 0), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<string> DisplayColorProperty = AvaloniaProperty.Register<ColorPicker, string>(nameof(DisplayColor),
                defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

        public static readonly StyledProperty<Color> OverlayColorProperty = AvaloniaProperty.Register<ColorPicker, Color>(nameof(OverlayColor), Colors.White);

        public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
        public string DisplayColor { get => GetValue(DisplayColorProperty); set => SetValue(DisplayColorProperty, value); }
        public Color OverlayColor { get => GetValue(OverlayColorProperty); private set => SetValue(OverlayColorProperty, value); }

        private bool _skip;

        static ColorPicker() {
            ColorProperty.Changed.AddClassHandler<ColorPicker>((o, e) => o.OnColorChanged((Color)e.NewValue));
            DisplayColorProperty.Changed.AddClassHandler<ColorPicker>((o, e) => o.OnDisplayColorChanged((string)e.NewValue));
        }

        public ColorPicker() {
            OnColorChanged(Color);
        }

        private void OnColorChanged(Color newValue) {
            if (_skip) return;
            if (DisplayColor?.ToColor() != newValue) {
                DisplayColor = newValue.ToHexString();
                OverlayColor = newValue.IsBright() ? Colors.Black : Colors.White;
            }
        }

        private void OnDisplayColorChanged(string newValue) {
            var color = newValue.ToColor();
            if (color.HasValue) {
                _skip = true;
                Color = color.Value;
                _skip = false;
            }
        }

        private class InnerColorToBrushConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                return value is Color c ? new SolidColorBrush(c).ToImmutable() : null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                throw new NotSupportedException();
            }
        }

        public static IValueConverter ColorToBrushConverter { get; } = new InnerColorToBrushConverter();

        private class InnerColorToStringConverter : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                return (value as Color?)?.ToHexString();
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                throw new NotSupportedException();
            }
        }

        public static IValueConverter ColorToStringConverter { get; } = new InnerColorToStringConverter();
    }
}
