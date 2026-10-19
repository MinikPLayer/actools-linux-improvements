using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class FormattedSlider : Slider {
        public FormattedSlider() {
        }

        public static readonly StyledProperty<string> AutoToolTipFormatProperty = DP.Register<string>(nameof(AutoToolTipFormat),
                typeof(FormattedSlider), new PropertyMetadata(null, (o, e) => {
                    ((FormattedSlider)o)._autoToolTipFormat = (string)e.NewValue;
                }));

        private string _autoToolTipFormat;

        [CanBeNull]
        public string AutoToolTipFormat {
            get => _autoToolTipFormat;
            set => SetValue(AutoToolTipFormatProperty, value);
        }

        public static readonly StyledProperty<IValueConverter> AutoToolTipConverterProperty = DP.Register<IValueConverter>(nameof(AutoToolTipConverter),
                typeof(FormattedSlider), new PropertyMetadata(null, (o, e) => {
                    ((FormattedSlider)o)._autoToolTipConverter = (IValueConverter)e.NewValue;
                }));

        private IValueConverter _autoToolTipConverter;

        [CanBeNull]
        public IValueConverter AutoToolTipConverter {
            get => _autoToolTipConverter;
            set => SetValue(AutoToolTipConverterProperty, value);
        }
        // PORT: Avalonia has no auto tooltip for the slider thumb, so the format and converter are kept for the bindings only
    }
}