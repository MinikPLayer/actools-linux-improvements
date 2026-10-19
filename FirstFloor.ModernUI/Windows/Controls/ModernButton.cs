using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class ModernButton : Button {
        public static readonly StyledProperty<double> EllipseDiameterProperty = DP.Register<double>("EllipseDiameter", typeof(ModernButton),
                new PropertyMetadata(22D));

        public static readonly StyledProperty<double> EllipseStrokeThicknessProperty = DP.Register<double>("EllipseStrokeThickness",
                typeof(ModernButton), new PropertyMetadata(1D));

        public static readonly StyledProperty<Geometry> IconDataProperty = DP.Register<Geometry>("IconData", typeof(ModernButton));

        public static readonly StyledProperty<double> IconHeightProperty = DP.Register<double>("IconHeight", typeof(ModernButton),
                new PropertyMetadata(12D));

        public static readonly StyledProperty<double> IconWidthProperty = DP.Register<double>("IconWidth", typeof(ModernButton),
                new PropertyMetadata(12D));

        public double EllipseDiameter {
            get => GetValue(EllipseDiameterProperty) as double? ?? default;
            set => SetValue(EllipseDiameterProperty, value);
        }

        public double EllipseStrokeThickness {
            get => GetValue(EllipseStrokeThicknessProperty) as double? ?? default;
            set => SetValue(EllipseStrokeThicknessProperty, value);
        }

        public Geometry IconData {
            get => (Geometry)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }

        public double IconHeight {
            get => GetValue(IconHeightProperty) as double? ?? default;
            set => SetValue(IconHeightProperty, value);
        }

        public double IconWidth {
            get => GetValue(IconWidthProperty) as double? ?? default;
            set => SetValue(IconWidthProperty, value);
        }
    }
}
