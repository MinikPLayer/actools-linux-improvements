using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>Mouse wheel scrolls horizontally: for ItemsControl with its own ScrollViewer.</summary>
    public class HorizontalScrollBehavior : Behavior<ItemsControl> {
        public string Name { get; set; }

        private ScrollViewer _scrollViewer;

        public bool IsInverted { get; set; }

        public bool IsEnabled { get; set; } = true;

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            AssociatedObject.Loaded -= OnLoaded;
            _scrollViewer = AssociatedObject.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
            _scrollViewer?.AddHandler(InputElement.PointerWheelChangedEvent, OnPreviewMouseWheel, RoutingStrategies.Tunnel);
        }

        protected override void OnDetaching() {
            _scrollViewer?.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPreviewMouseWheel);
            base.OnDetaching();
        }

        private void OnPreviewMouseWheel(object sender, PointerWheelEventArgs e) {
            if (!e.Handled && IsEnabled && _scrollViewer != null) {
                var delta = e.Delta.Y * 48d;
                _scrollViewer.Offset = _scrollViewer.Offset.WithX(Math.Max(0d, IsInverted ? _scrollViewer.Offset.X + delta : _scrollViewer.Offset.X - delta));
                e.Handled = true;
            }
        }
    }

    public class HorizontalScrollViewerBehavior : Behavior<ScrollViewer> {
        public bool IsInverted { get; set; }

        public bool IsEnabled { get; set; } = true;

        protected override void OnAttached() {
            base.OnAttached();
            AssociatedObject.AddHandler(InputElement.PointerWheelChangedEvent, OnPreviewMouseWheel, RoutingStrategies.Tunnel);
        }

        protected override void OnDetaching() {
            AssociatedObject?.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPreviewMouseWheel);
            base.OnDetaching();
        }

        private void OnPreviewMouseWheel(object sender, PointerWheelEventArgs e) {
            if (!e.Handled && IsEnabled && AssociatedObject != null) {
                var delta = e.Delta.Y * 48d;
                var o = AssociatedObject.Offset;
                AssociatedObject.Offset = o.WithX(Math.Max(0d, IsInverted ? o.X + delta : o.X - delta));
                e.Handled = true;
            }
        }
    }
}
