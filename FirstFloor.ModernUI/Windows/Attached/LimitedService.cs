using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace FirstFloor.ModernUI.Windows.Attached {
    public static class ComboBoxAdvancement {
        public static IValueConverter GetDisplayConverter(DependencyObject obj) {
            return (IValueConverter)obj.GetValue(DisplayConverterProperty);
        }

        public static void SetDisplayConverter(DependencyObject obj, IValueConverter value) {
            obj.SetValue(DisplayConverterProperty, value);
        }

        public static readonly AttachedProperty<IValueConverter> DisplayConverterProperty = DP.RegisterAttached<IValueConverter>("DisplayConverter",
                typeof(ComboBoxAdvancement), new UIPropertyMetadata(OnDisplayConverterChanged));

        private static void OnDisplayConverterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is ComboBox element) || !(e.NewValue is IValueConverter newValue)) return;
            element.ItemTemplate = new FuncDataTemplate<object>((item, scope) => new TextBlock {
                [!TextBlock.TextProperty] = new Avalonia.Data.Binding { Converter = newValue }
            });
        }
    }

    /// <summary>
    /// Marks controls which are available only for supporters (the original one added an adorner with a lock icon and dimmed content).
    /// </summary>
    public static class LimitedService {
        public static bool OptionNonLimited = false;

        public static readonly AttachedProperty<bool> LimitedProperty = DP.RegisterAttached<bool>("Limited", typeof(LimitedService),
                new FrameworkPropertyMetadata(false, OnLimitedChanged));

        public static bool GetLimited(DependencyObject d) {
            return d.GetValue(LimitedProperty) as bool? == true;
        }

        public static void SetLimited(DependencyObject d, bool value) {
            d.SetValue(LimitedProperty, value);
        }

        private static void OnLimitedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (OptionNonLimited || !(d is Avalonia.Controls.Control c)) return;
            // TODO: lock adorner
            c.Opacity = e.NewValue as bool? == true ? 0.5 : 1d;
        }
    }
}
