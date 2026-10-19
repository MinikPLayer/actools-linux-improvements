using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FirstFloor.ModernUI.Serialization;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Viewbox which exposes its scale (to make borders and such keep their size, see <see cref="ThicknessConverter"/>).
    /// </summary>
    public class BorderyViewbox : Decorator {
        public static readonly StyledProperty<Stretch> StretchProperty = AvaloniaProperty.Register<BorderyViewbox, Stretch>(nameof(Stretch), Stretch.Uniform);

        public Stretch Stretch {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty = AvaloniaProperty.Register<BorderyViewbox, StretchDirection>(nameof(StretchDirection), StretchDirection.Both);

        public StretchDirection StretchDirection {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        public static readonly StyledProperty<Size> ScaleProperty = AvaloniaProperty.Register<BorderyViewbox, Size>(nameof(Scale));

        public Size Scale {
            get => GetValue(ScaleProperty);
            private set => SetValue(ScaleProperty, value);
        }

        static BorderyViewbox() {
            AffectsMeasure<BorderyViewbox>(StretchProperty, StretchDirectionProperty);
        }

        protected override Size MeasureOverride(Size constraint) {
            var child = Child;
            if (child == null) return default;

            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var childSize = child.DesiredSize;
            var scale = ComputeScaleFactor(constraint, childSize, Stretch, StretchDirection);
            Scale = scale;
            return new Size(scale.Width * childSize.Width, scale.Height * childSize.Height);
        }

        protected override Size ArrangeOverride(Size arrangeSize) {
            var child = Child;
            if (child == null) return arrangeSize;

            var childSize = child.DesiredSize;
            var scale = ComputeScaleFactor(arrangeSize, childSize, Stretch, StretchDirection);
            Scale = scale;

            child.RenderTransformOrigin = RelativePoint.TopLeft;
            child.RenderTransform = new ScaleTransform(scale.Width, scale.Height);
            child.Arrange(new Rect(new Point(), childSize));
            return new Size(scale.Width * childSize.Width, scale.Height * childSize.Height);
        }

        private static Size ComputeScaleFactor(Size availableSize, Size contentSize, Stretch stretch, StretchDirection stretchDirection) {
            var scaleX = 1.0;
            var scaleY = 1.0;

            var isConstrainedWidth = !double.IsPositiveInfinity(availableSize.Width);
            var isConstrainedHeight = !double.IsPositiveInfinity(availableSize.Height);

            if ((stretch == Stretch.Uniform || stretch == Stretch.UniformToFill || stretch == Stretch.Fill)
                    && (isConstrainedWidth || isConstrainedHeight)) {
                scaleX = Equals(0d, contentSize.Width) ? 0.0 : availableSize.Width / contentSize.Width;
                scaleY = Equals(0d, contentSize.Height) ? 0.0 : availableSize.Height / contentSize.Height;

                if (!isConstrainedWidth) {
                    scaleX = scaleY;
                } else if (!isConstrainedHeight) {
                    scaleY = scaleX;
                } else {
                    switch (stretch) {
                        case Stretch.Uniform:
                            var minscale = scaleX < scaleY ? scaleX : scaleY;
                            scaleX = scaleY = minscale;
                            break;

                        case Stretch.UniformToFill:
                            var maxscale = scaleX > scaleY ? scaleX : scaleY;
                            scaleX = scaleY = maxscale;
                            break;
                    }
                }

                switch (stretchDirection) {
                    case StretchDirection.UpOnly:
                        if (scaleX < 1.0) scaleX = 1.0;
                        if (scaleY < 1.0) scaleY = 1.0;
                        break;

                    case StretchDirection.DownOnly:
                        if (scaleX > 1.0) scaleX = 1.0;
                        if (scaleY > 1.0) scaleY = 1.0;
                        break;
                }
            }

            return new Size(scaleX, scaleY);
        }

        #region Converter-related stuff
        private class ThicknessConverterInner : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                if (!(value is Size)) return parameter;

                var scale = (Size)value;
                var thickness = parameter as Thickness? ?? new Thickness(1d);

                return new Thickness(thickness.Left / scale.Width, thickness.Top / scale.Height,
                        thickness.Right / scale.Width, thickness.Bottom / scale.Height);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                if (!(value is Size)) return parameter;

                var scale = (Size)value;
                var thickness = parameter as Thickness? ?? new Thickness(1d);

                return new Thickness(thickness.Left * scale.Width, thickness.Top * scale.Height,
                        thickness.Right * scale.Width, thickness.Bottom * scale.Height);
            }
        }

        public static IValueConverter ThicknessConverter { get; } = new ThicknessConverterInner();

        private class InvertConverterInner : IValueConverter {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
                return parameter.As(1d) / value.As(1d);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
                return parameter.As(1d) * value.As(1d);
            }
        }

        public static IValueConverter InvertConverter { get; } = new InvertConverterInner();
        #endregion
    }
}
