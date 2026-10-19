using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AcTools.Utils;

namespace AcManager.Controls {
    public class RatingBar : Control {
        static RatingBar() {
        }

        public static readonly StyledProperty<Brush> ActiveBrushProperty = DP.Register<Brush>(nameof(ActiveBrush),
                typeof(RatingBar));

        public Brush ActiveBrush {
            get => (Brush)GetValue(ActiveBrushProperty);
            set => SetValue(ActiveBrushProperty, value);
        }

        public static readonly StyledProperty<PathData> DataProperty = DP.Register<PathData>(nameof(Data),
                typeof(RatingBar));

        public PathData Data {
            get => (PathData)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly StyledProperty<double> RatingProperty = DP.Register<double>(nameof(Rating),
                typeof(RatingBar), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double Rating {
            get => GetValue(RatingProperty) as double? ?? default;
            set => SetValue(RatingProperty, value);
        }

        public static readonly StyledProperty<bool> IsReadOnlyProperty = DP.Register<bool>(nameof(IsReadOnly),
                typeof(RatingBar));

        public bool IsReadOnly {
            get => GetValue(IsReadOnlyProperty) as bool? ?? default;
            set => SetValue(IsReadOnlyProperty, value);
        }

        private void Update(MouseEventArgs e) {
            if (IsReadOnly) return;
            Rating = (5d * e.GetPosition(this).X / this.ActualWidth).Round(0.5);
            Focus();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e) {
            base.OnPointerPressed(e);
            Update(e);
        }

        protected override void OnPointerMoved(PointerEventArgs e) {
            base.OnPointerMoved(e);
            if (e.LeftButton == MouseButtonState.Pressed) {
                Update(e);
            }
        }
    }
}