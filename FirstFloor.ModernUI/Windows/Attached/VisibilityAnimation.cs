using System;
using System.Linq;
using Avalonia.Animation;
using System.Windows;

namespace FirstFloor.ModernUI.Windows.Attached {
    /// <summary>
    /// Fades elements in and out; implemented with Avalonia’s opacity transitions.
    /// </summary>
    public class VisibilityAnimation : DependencyObject {
        public static TimeSpan GetDuration(DependencyObject obj) {
            return obj.GetValue(DurationProperty) as TimeSpan? ?? default;
        }

        public static void SetDuration(DependencyObject obj, TimeSpan value) {
            obj.SetValue(DurationProperty, value);
        }

        public static readonly AttachedProperty<TimeSpan> DurationProperty = DP.RegisterAttached<TimeSpan>("Duration",
                typeof(VisibilityAnimation), new UIPropertyMetadata(TimeSpan.FromSeconds(0.3)));

        public static bool? GetVisible(DependencyObject obj) {
            return (bool?)obj.GetValue(VisibleProperty);
        }

        public static void SetVisible(DependencyObject obj, bool? value) {
            obj.SetValue(VisibleProperty, value);
        }

        public static readonly AttachedProperty<bool?> VisibleProperty = DP.RegisterAttached<bool?>("Visible",
                typeof(VisibilityAnimation), new UIPropertyMetadata(null, OnVisibleChanged));

        private static void OnVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is Avalonia.Controls.Control element) || !(e.NewValue is bool)) return;

            var oldValue = e.OldValue as bool?;
            var newValue = (bool?)e.NewValue == true;
            if (!oldValue.HasValue) {
                if (!newValue) {
                    element.Opacity = 0d;
                    element.IsHitTestVisible = false;
                }
                return;
            }

            if (oldValue == newValue) return;

            var transitions = element.Transitions ?? (element.Transitions = new Transitions());
            if (!transitions.OfType<DoubleTransition>().Any(x => x.Property == Avalonia.Visual.OpacityProperty)) {
                transitions.Add(new DoubleTransition { Property = Avalonia.Visual.OpacityProperty, Duration = GetDuration(element) });
            }

            element.IsHitTestVisible = newValue;
            element.Opacity = newValue ? 1d : 0d;
        }
    }
}
