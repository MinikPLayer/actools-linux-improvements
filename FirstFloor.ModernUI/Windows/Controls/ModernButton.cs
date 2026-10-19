using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class ModernButton : Button {
        public static readonly DependencyProperty EllipseDiameterProperty = DP.Register("EllipseDiameter", typeof(double), typeof(ModernButton),
                new PropertyMetadata(22D));

        public static readonly DependencyProperty EllipseStrokeThicknessProperty = DP.Register("EllipseStrokeThickness", typeof(double),
                typeof(ModernButton), new PropertyMetadata(1D));

        public static readonly DependencyProperty IconDataProperty = DP.Register("IconData", typeof(Geometry), typeof(ModernButton));

        public static readonly DependencyProperty IconHeightProperty = DP.Register("IconHeight", typeof(double), typeof(ModernButton),
                new PropertyMetadata(12D));

        public static readonly DependencyProperty IconWidthProperty = DP.Register("IconWidth", typeof(double), typeof(ModernButton),
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
