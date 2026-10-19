using System;
using System.Windows;
using System.Windows.Controls;
using AcManager.Controls.Converters;
using AcManager.Tools.Helpers;
using AcTools.Utils;
using FirstFloor.ModernUI.Windows.Controls;

namespace AcManager.Controls {
    public class TemperatureBlock : TextBlock {
        static TemperatureBlock() {
        }

        public static readonly StyledProperty<string> SpaceProperty = DP.Register<string>(nameof(Space),
                typeof(TemperatureBlock), new FrameworkPropertyMetadata("\u202A", OnModeChanged));

        public string Space {
            get => (string)GetValue(SpaceProperty);
            set => SetValue(SpaceProperty, value);
        }

        public static readonly StyledProperty<string> PrefixProperty = DP.Register<string>(nameof(Prefix),
                typeof(TemperatureBlock), new FrameworkPropertyMetadata("", OnModeChanged));

        public string Prefix {
            get => (string)GetValue(PrefixProperty);
            set => SetValue(PrefixProperty, value);
        }

        public static readonly StyledProperty<double> RoundingProperty = DP.Register<double>(nameof(Rounding),
                typeof(TemperatureBlock), new FrameworkPropertyMetadata(1d));

        public double Rounding {
            get => GetValue(RoundingProperty) as double? ?? 0d;
            set => SetValue(RoundingProperty, value);
        }

        public new static readonly StyledProperty<double> TextProperty = DP.Register<double>(nameof(Text),
                typeof(TemperatureBlock), new PropertyMetadata(0d, (o, e) => {
                    ((TemperatureBlock)o)._text = (double)e.NewValue;
                    ((TemperatureBlock)o).Update();
                }));

        private double _text;

        public new double Text {
            get => _text;
            set => SetValue(TextProperty, value);
        }

        public static readonly StyledProperty<TemperatureUnitMode> ModeProperty = DP.Register<TemperatureUnitMode>(nameof(Mode),
                typeof(TemperatureBlock), new PropertyMetadata(OnModeChanged));

        public TemperatureUnitMode Mode {
            get => GetValue(ModeProperty) as TemperatureUnitMode? ?? default;
            set => SetValue(ModeProperty, value);
        }

        private static void OnModeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((TemperatureBlock)o).Update();
        }

        private void Update() {
            var space = Space;
            var rounding = Rounding;
            switch (Mode) {
                case TemperatureUnitMode.Celsius:
                    base.Text = $"{Prefix}{Text.Round(rounding)}{space}{ControlsStrings.Common_CelsiusPostfix}";
                    break;
                case TemperatureUnitMode.Fahrenheit:
                    base.Text = $"{Prefix}{TemperatureToFahrenheitConverter.ToFahrenheit(Text).Round(rounding)}{space}{ControlsStrings.Common_FahrenheitPostfix}";
                    break;
                case TemperatureUnitMode.Both:
                    base.Text = $"{Prefix}{Text.Round(rounding)}{space}{ControlsStrings.Common_CelsiusPostfix}, {TemperatureToFahrenheitConverter.ToFahrenheit(Text).Round(rounding)}{space}{ControlsStrings.Common_FahrenheitPostfix}";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class TemperatureValueLabel : ValueLabel {
        static TemperatureValueLabel() {
        }

        public static readonly StyledProperty<TemperatureUnitMode> ModeProperty = DP.Register<TemperatureUnitMode>(nameof(Mode),
                typeof(TemperatureValueLabel));

        public TemperatureUnitMode Mode {
            get => GetValue(ModeProperty) as TemperatureUnitMode? ?? default;
            set => SetValue(ModeProperty, value);
        }

        public static readonly StyledProperty<string> FahrenheitPostfixProperty = DP.Register<string>(nameof(FahrenheitPostfix),
                typeof(TemperatureValueLabel));

        public string FahrenheitPostfix {
            get => (string)GetValue(FahrenheitPostfixProperty);
            set => SetValue(FahrenheitPostfixProperty, value);
        }
    }

    public class DoubleTemperatureValueLabel : DoubleValueLabel {
        static DoubleTemperatureValueLabel() {
        }

        public static readonly StyledProperty<TemperatureUnitMode> ModeProperty = DP.Register<TemperatureUnitMode>(nameof(Mode),
                typeof(DoubleTemperatureValueLabel));

        public TemperatureUnitMode Mode {
            get => GetValue(ModeProperty) as TemperatureUnitMode? ?? default;
            set => SetValue(ModeProperty, value);
        }

        public static readonly StyledProperty<string> FahrenheitPostfixProperty = DP.Register<string>(nameof(FahrenheitPostfix),
                typeof(DoubleTemperatureValueLabel));

        public string FahrenheitPostfix {
            get => (string)GetValue(FahrenheitPostfixProperty);
            set => SetValue(FahrenheitPostfixProperty, value);
        }
    }
}
