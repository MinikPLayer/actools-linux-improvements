using System.Windows;

namespace FirstFloor.ModernUI.Windows.Attached {
    public static class HyperlinkHelper {
        public static bool GetIsHighlighted(DependencyObject obj) {
            return (bool)obj.GetValue(IsHighlightedProperty);
        }

        public static void SetIsHighlighted(DependencyObject obj, bool value) {
            obj.SetValue(IsHighlightedProperty, value);
        }

        public static readonly AttachedProperty<bool> IsHighlightedProperty = DP.RegisterAttached<bool>("IsHighlighted",
                typeof(HyperlinkHelper), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));
    }
}