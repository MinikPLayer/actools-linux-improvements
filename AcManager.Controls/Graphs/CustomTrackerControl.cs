using System;
using System.Windows;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf;

namespace AcManager.Controls.Graphs {
    public class CustomTrackerControl : TrackerControl {
        static CustomTrackerControl() {
        }

        public static readonly StyledProperty<ScreenPoint> PositionOverrideProperty = DP.Register<ScreenPoint>(nameof(PositionOverride),
                typeof(CustomTrackerControl), new PropertyMetadata(OnPositionOverrideChanged));

        public ScreenPoint PositionOverride {
            get => GetValue(PositionOverrideProperty) as ScreenPoint? ?? default;
            set => SetValue(PositionOverrideProperty, value);
        }

        private static void OnPositionOverrideChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((CustomTrackerControl)o).UpdatePositionAndLineExtents();
        }

        public static readonly StyledProperty<OxyRect> LineExtentsOverrideProperty = DP.Register<OxyRect>(nameof(LineExtentsOverride),
                typeof(CustomTrackerControl), new PropertyMetadata(OnLineExtentsOverrideChanged));

        public OxyRect LineExtentsOverride {
            get => GetValue(LineExtentsOverrideProperty) as OxyRect? ?? default;
            set => SetValue(LineExtentsOverrideProperty, value);
        }

        private static void OnLineExtentsOverrideChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((CustomTrackerControl)o).UpdatePositionAndLineExtents();
        }

        // private Popup _popup;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            UpdatePositionAndLineExtents();
            // _popup = this.GetTemplateChild("PART_Popup") as Popup;
            base.OnApplyTemplate(e);
        }

        private void UpdatePositionAndLineExtents() {
            var position = PositionOverride;
            var extents = LineExtentsOverride;

            var yAxis = (DataContext as TrackerHitResult)?.YAxis;
            Position = position;
            LineExtents = yAxis?.Position == AxisPosition.Right
                    ? new OxyRect(position.X, position.Y, Math.Max(extents.Right - position.X, 0.001), Math.Max(extents.Bottom - position.Y, 0.001))
                    : new OxyRect(extents.Left, position.Y, Math.Max(position.X - extents.Left, 0.001), Math.Max(extents.Bottom - position.Y, 0.001));

            if (yAxis != null) {
                LineStroke = yAxis.AxislineColor.ToBrush();
            }
        }
    }
}