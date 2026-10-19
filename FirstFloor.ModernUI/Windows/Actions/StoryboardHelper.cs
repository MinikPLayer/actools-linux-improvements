using System;
using System.Threading.Tasks;
using System.Windows;

namespace FirstFloor.ModernUI.Windows.Actions {
    public static class StoryboardHelper {
        public static bool GetContinuousFiring(DependencyObject obj) {
            return (bool)obj.GetValue(ContinuousFiringProperty);
        }

        public static void SetContinuousFiring(DependencyObject obj, bool value) {
            obj.SetValue(ContinuousFiringProperty, value);
        }

        public static readonly AttachedProperty<bool> ContinuousFiringProperty = DP.RegisterAttached<bool>("ContinuousFiring",
                typeof(StoryboardHelper), new UIPropertyMetadata(OnContinuousFiringChanged));

        private static async void OnContinuousFiringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(e.NewValue is bool v)) return;

            if (v && !GetContinuousFiringActive(d)) {
                SetContinuousFiringActive(d, true);
                while (GetContinuousFiring(d)) {
                    SetContinuousFired(d, true);
                    await Task.Yield();
                    SetContinuousFired(d, false);
                    var delay = GetContinuousFiringInterval(d);
                    await Task.Delay(delay.TotalMilliseconds < 100 ? TimeSpan.FromMilliseconds(100) : delay);
                }
                SetContinuousFiringActive(d, false);
            }
        }

        public static bool GetContinuousFiringActive(DependencyObject obj) {
            return (bool)obj.GetValue(ContinuousFiringActiveProperty);
        }

        public static void SetContinuousFiringActive(DependencyObject obj, bool value) {
            obj.SetValue(ContinuousFiringActiveProperty, value);
        }

        public static readonly AttachedProperty<bool> ContinuousFiringActiveProperty = DP.RegisterAttached<bool>("ContinuousFiringActive",
                typeof(StoryboardHelper));

        public static TimeSpan GetContinuousFiringInterval(DependencyObject obj) {
            return (TimeSpan)obj.GetValue(ContinuousFiringIntervalProperty);
        }

        public static void SetContinuousFiringInterval(DependencyObject obj, TimeSpan value) {
            obj.SetValue(ContinuousFiringIntervalProperty, value);
        }

        public static readonly AttachedProperty<TimeSpan> ContinuousFiringIntervalProperty = DP.RegisterAttached<TimeSpan>("ContinuousFiringInterval",
                typeof(StoryboardHelper));

        public static bool GetContinuousFired(DependencyObject obj) {
            return (bool)obj.GetValue(ContinuousFiredProperty);
        }

        public static void SetContinuousFired(DependencyObject obj, bool value) {
            obj.SetValue(ContinuousFiredProperty, value);
        }

        public static readonly AttachedProperty<bool> ContinuousFiredProperty = DP.RegisterAttached<bool>("ContinuousFired",
                typeof(StoryboardHelper));
    }
}