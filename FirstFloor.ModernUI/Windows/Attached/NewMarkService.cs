using System.Windows;

namespace FirstFloor.ModernUI.Windows.Attached {
    /// <summary>
    /// Adds a small “new” mark to elements (visuals aren’t ported yet).
    /// </summary>
    public static class NewMarkService {
        public static readonly AttachedProperty<bool> NewProperty = DP.RegisterAttached<bool>("New", typeof(NewMarkService), new FrameworkPropertyMetadata(false));

        public static bool GetNew(DependencyObject d) => d.GetValue(NewProperty);

        public static void SetNew(DependencyObject d, bool value) => d.SetValue(NewProperty, value);
    }
}
