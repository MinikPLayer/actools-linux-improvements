using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Media {
    /// <summary>
    /// Provides addition visual tree helper methods.
    /// </summary>
    public static class VisualTreeHelperEx {
        public static IEnumerable<T> GetAllOfType<T>() where T : AvaloniaObject {
            var app = Application.Current;
            if (app == null) yield break;
            foreach (var image in app.Windows.OfType<Window>().SelectMany(x => FindVisualChildren<T>(x))) {
                yield return image;
            }
        }

        [Pure]
        public static bool IsUserVisible(this Control element, Control container) {
            if (element.IsVisible != true) return false;
            var topLeft = element.TranslatePoint(new Point(0.0, 0.0), container);
            if (topLeft == null) return false;
            var bottomRight = new Point(topLeft.Value.X + element.Bounds.Width, topLeft.Value.Y + element.Bounds.Height);
            var rect = new Avalonia.Rect(0.0, 0.0, container.Bounds.Width, container.Bounds.Height);
            return rect.Contains(topLeft.Value) || rect.Contains(bottomRight);
        }

        [Pure]
        public static IEnumerable<T> GetVisibleItemsFromListbox<T>(this ItemsControl listBox) {
            var any = false;
            foreach (var item in listBox.Items.OfType<T>()) {
                var container = listBox.ContainerFromItem(item);
                if (container != null && IsUserVisible(container, listBox)) {
                    any = true;
                    yield return item;
                } else if (any) {
                    break;
                }
            }
        }

        [Pure]
        public static IEnumerable<T> FindVisualChildren<T>([NotNull] this AvaloniaObject depObj) where T : AvaloniaObject {
            if (depObj == null) throw new ArgumentNullException(nameof(depObj));
            return depObj is Visual v ? v.GetVisualDescendants().OfType<T>() : Enumerable.Empty<T>();
        }

        [Pure, CanBeNull]
        public static T FindVisualChild<T>([NotNull] this AvaloniaObject obj) where T : AvaloniaObject {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return FindVisualChildren<T>(obj).FirstOrDefault();
        }

        [Pure]
        public static IEnumerable<T> FindLogicalChildren<T>([NotNull] this AvaloniaObject depObj) where T : AvaloniaObject {
            if (depObj == null) throw new ArgumentNullException(nameof(depObj));
            return depObj is ILogical l ? l.GetLogicalDescendants().OfType<T>() : Enumerable.Empty<T>();
        }

        [Pure, CanBeNull]
        public static T FindLogicalChild<T>([NotNull] this AvaloniaObject obj) where T : AvaloniaObject {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            return FindLogicalChildren<T>(obj).FirstOrDefault();
        }

        /// <summary>
        /// Returns a collection of the visual ancestor elements of specified dependency object.
        /// </summary>
        public static IEnumerable<AvaloniaObject> Ancestors(this AvaloniaObject dependencyObject) {
            var parent = dependencyObject;
            while (true) {
                parent = GetParent(parent);
                if (parent != null) {
                    yield return parent;
                } else {
                    break;
                }
            }
        }

        /// <summary>
        /// Returns a collection of visual elements that contain specified object, and the ancestors of specified object.
        /// </summary>
        public static IEnumerable<AvaloniaObject> AncestorsAndSelf(this AvaloniaObject dependencyObject) {
            if (dependencyObject == null) throw new ArgumentNullException(nameof(dependencyObject));

            var parent = dependencyObject;
            while (true) {
                if (parent != null) {
                    yield return parent;
                } else {
                    break;
                }
                parent = GetParent(parent);
            }
        }

        /// <summary>
        /// Gets the parent for specified dependency object (logical parent first, like in WPF; falls back to visual one).
        /// </summary>
        [Pure, CanBeNull]
        public static AvaloniaObject GetParent([NotNull] this AvaloniaObject dependencyObject) {
            if (dependencyObject == null) throw new ArgumentNullException(nameof(dependencyObject));

            if (dependencyObject is StyledElement se && se.Parent != null) return se.Parent;
            return dependencyObject is Visual v ? v.GetVisualParent() : null;
        }

        [Pure]
        public static IEnumerable<AvaloniaObject> GetParents([NotNull] this AvaloniaObject dependencyObject) {
            if (dependencyObject == null) throw new ArgumentNullException(nameof(dependencyObject));

            var parent = dependencyObject.GetParent();
            while (parent != null) {
                yield return parent;
                parent = parent.GetParent();
            }
        }

        [Pure, CanBeNull]
        public static AvaloniaObject GetParentWhere([NotNull] this AvaloniaObject dependencyObject, [NotNull] Func<AvaloniaObject, bool> predicate) {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return dependencyObject.GetParents().Where(predicate).FirstOrDefault();
        }

        [Pure, CanBeNull]
        public static T GetParent<T>([NotNull] this AvaloniaObject dependencyObject) where T : AvaloniaObject {
            return dependencyObject.GetParents().OfType<T>().FirstOrDefault();
        }

        [Pure, CanBeNull]
        public static T GetFromPoint<T>(this Control reference, Point point) where T : AvaloniaObject {
            return reference.InputHitTest(point) is AvaloniaObject element ? element as T ?? GetParent<T>(element) : null;
        }

        /// <summary>
        /// Works faster if value the same — for example, usual VerifyAccess() won’t be called.
        /// </summary>
        public static void SetVisibility([CanBeNull] this Control element, bool visible) {
            if (element != null && element.IsVisible != visible) {
                element.IsVisible = visible;
            }
        }

        public static void SetVisibility([CanBeNull] this Control element, Visibility visibility) {
            SetVisibility(element, visibility == Visibility.Visible);
        }

        /// <summary>
        /// Not needed in Avalonia, bindings with element names work in templates properly.
        /// </summary>
        public static void ResetElementNameBindings([NotNull] this AvaloniaObject obj) { }
    }
}
