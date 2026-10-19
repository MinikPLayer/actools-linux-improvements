using System.Windows;

namespace FirstFloor.ModernUI.Windows.Attached {
    public static class ExpanderHelper {
        public static bool GetIsContentVisible(DependencyObject obj) {
            return (bool)obj.GetValue(IsContentVisibleProperty);
        }

        public static void SetIsContentVisible(DependencyObject obj, bool value) {
            obj.SetValue(IsContentVisibleProperty, value);
        }

        public static readonly AttachedProperty<bool> IsContentVisibleProperty = DP.RegisterAttached<bool>("IsContentVisible",
                typeof(ExpanderHelper), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
    }
}