using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Media {
    public static partial class VisualExtension {
        /// <summary>
        /// WPF version could ask the system for mouse position at any moment. Avalonia can’t, so the last known position
        /// (in screen pixels) is tracked by a handler which gets all pointer events.
        /// </summary>
        private static class MouseTracker {
            public static Point Position;

            static MouseTracker() {
                InputElement.PointerMovedEvent.AddClassHandler<InputElement>(OnPointerEvent, handledEventsToo: true);
                InputElement.PointerPressedEvent.AddClassHandler<InputElement>(OnPointerEvent, handledEventsToo: true);
                InputElement.PointerReleasedEvent.AddClassHandler<InputElement>(OnPointerEvent, handledEventsToo: true);
            }

            private static void OnPointerEvent(InputElement sender, PointerEventArgs e) {
                if (sender is Visual visual && visual.GetVisualRoot() != null) {
                    var screen = visual.PointToScreen(e.GetPosition(visual));
                    Position = new Point(screen.X, screen.Y);
                }
            }
        }

        /// <summary>
        /// Call once on start, so tracking would work from the beginning.
        /// </summary>
        public static void StartTrackingMouse() {
            GetMousePosition();
        }

        public static Point GetMousePosition() {
            return MouseTracker.Position;
        }

        public static Point GetMousePosition(this Visual relativeTo) {
            return relativeTo.GetMousePosition(GetMousePosition());
        }

        public static Point GetMousePosition(this Visual relativeTo, Point screenPoint) {
            return relativeTo.PointToClient(new PixelPoint((int)Math.Round(screenPoint.X), (int)Math.Round(screenPoint.Y)));
        }

        public static bool IsMouseOverElement(this Visual target) {
            return target.IsMouseOverElement(GetMousePosition());
        }

        public static bool IsMouseOverElement(this Visual target, Point screenPoint) {
            return new Rect(target.Bounds.Size).Contains(target.GetMousePosition(screenPoint));
        }

        [CanBeNull]
        public static Visual GetItemVisual(this ItemsControl list, object item) {
            var index = list.Items.IndexOf(item);
            return index < 0 ? null : list.ContainerFromIndex(index);
        }

        [CanBeNull]
        public static Visual GetIndexVisual(this ItemsControl list, int index) {
            return index < 0 || index >= list.Items.Count ? null : list.ContainerFromIndex(index);
        }

        public static bool IsMouseOverIndex(this ItemsControl list, int index) {
            return list.GetIndexVisual(index)?.IsMouseOverElement() == true;
        }

        public static bool IsMouseOverIndex(this ItemsControl list, int index, Point point) {
            return list.GetIndexVisual(index)?.IsMouseOverElement(point) == true;
        }

        public static bool IsMouseOverItem(this ItemsControl list, object item) {
            return list.GetItemVisual(item)?.IsMouseOverElement() == true;
        }

        [CanBeNull]
        public static object GetMouseItem(this ItemsControl list) {
            var screenPoint = GetMousePosition();

            for (var i = 0; i < list.Items.Count; i++) {
                if (list.IsMouseOverIndex(i, screenPoint)) {
                    return list.Items[i];
                }
            }

            return null;
        }

        public static int GetMouseItemIndex(this ItemsControl list) {
            var screenPoint = GetMousePosition();

            for (var i = 0; i < list.Items.Count; i++) {
                if (list.IsMouseOverIndex(i, screenPoint)) {
                    return i;
                }
            }

            return -1;
        }

        public static double DistanceTo(this Point a, Point b) {
            return Math.Sqrt(Math.Pow(a.X - b.X, 2d) + Math.Pow(a.Y - b.Y, 2d));
        }
    }
}
