using System;
using System.Windows;
using System.Windows.Media.Effects;

namespace FirstFloor.ModernUI.Effects {
    public class ColorPickerSaturationPaletteEffect : ShaderEffect {
        private static readonly PixelShader PixelShaderCompiled = new PixelShader {
            UriSource = new Uri("/FirstFloor.ModernUI;component/Effects/Shaders/ColorPickerSaturationPaletteEffect.ps", UriKind.Relative)
        };

        public ColorPickerSaturationPaletteEffect() {
            PixelShader = PixelShaderCompiled;
            UpdateShaderValue(SaturationProperty);
        }

        public static readonly StyledProperty<double> SaturationProperty = DP.Register<double>("Saturation",
                typeof(ColorPickerSaturationPaletteEffect), new UIPropertyMetadata(0d, PixelShaderConstantCallback(0), CoerceSaturationFactor));

        public double Saturation {
            get => GetValue(SaturationProperty) as double? ?? 0d;
            set => SetValue(SaturationProperty, value);
        }

        private static object CoerceSaturationFactor(DependencyObject d, object value) {
            return Math.Min(Math.Max((double)value / 100d, 0d), 1d);
        }
    }
}