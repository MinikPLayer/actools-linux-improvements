using System;
using System.Windows;

namespace FirstFloor.ModernUI.Windows.Attached {
    /// <summary>
    /// Obsolete in the original code as well (BetterTextBox has a placeholder). Kept for compatibility, shows nothing.
    /// </summary>
    [Obsolete]
    public static class WatermarkService {
        public static readonly AttachedProperty<object> WatermarkProperty = DP.RegisterAttached<object>("Watermark", typeof(WatermarkService),
                new FrameworkPropertyMetadata(null));

        public static object GetWatermark(DependencyObject d) => d.GetValue(WatermarkProperty);

        public static void SetWatermark(DependencyObject d, object value) => d.SetValue(WatermarkProperty, value);
    }
}
