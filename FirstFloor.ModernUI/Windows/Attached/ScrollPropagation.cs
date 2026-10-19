using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FirstFloor.ModernUI.Windows.Media;

namespace FirstFloor.ModernUI.Windows.Attached {
    /// <summary>
    /// Avalonia’s scroll viewers pass wheel events to the parent when they can’t scroll any further, so there is not much to fix here;
    /// the only thing is that nested list boxes without their own scroll viewers shouldn’t take wheel events.
    /// </summary>
    public static class ScrollPropagation {
        public static bool GetFix(DependencyObject obj) {
            return (bool)obj.GetValue(FixProperty);
        }

        public static void SetFix(DependencyObject obj, bool value) {
            obj.SetValue(FixProperty, value);
        }

        public static readonly AttachedProperty<bool> FixProperty = DP.RegisterAttached<bool>("Fix",
                typeof(ScrollPropagation), new UIPropertyMetadata(OnFixChanged));

        private static void OnFixChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is FrameworkElement element && e.NewValue is bool value) {
                if (value) {
                    element.AddPreviewMouseWheel(OnMouseWheel);
                } else {
                    element.RemovePreviewMouseWheel(OnMouseWheel);
                }
            }
        }

        private static void OnMouseWheel(object sender, PointerWheelEventArgs e) {
            if (sender is ScrollViewer scrollViewer) {
                var hovered = (Mouse.DirectlyOver as FrameworkElement)?.GetParent<ScrollViewer>();
                if (hovered != null && hovered != scrollViewer) {
                    if (e.Delta.Y < 0
                            ? scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight
                            : hovered.VerticalOffset == 0) {
                        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - e.Delta.Y * 48d);
                        e.Handled = true;
                    }
                }
            } else {
                var parentScroll = (sender as FrameworkElement)?.GetParent<ScrollViewer>();
                if (parentScroll != null) {
                    e.Handled = true; // Prevent ListBox from handling it
                    parentScroll.ScrollToVerticalOffset(parentScroll.VerticalOffset - e.Delta.Y * 48d);
                }
            }
        }
    }
}
