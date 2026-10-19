using System.Windows;
using System.Windows.Input;

namespace FirstFloor.ModernUI.Windows.Attached {
    public class IsPressedAdvancement {
        public static readonly AttachedProperty<bool> IsPressedProperty = DP.RegisterAttached<bool>("IsPressed",
                typeof(IsPressedAdvancement), new PropertyMetadata(false));

        public static readonly AttachedProperty<bool> AttachIsPressedProperty = DP.RegisterAttached<bool>("AttachIsPressed",
                typeof(IsPressedAdvancement), new PropertyMetadata(false, PropertyChangedCallback));

        public static void PropertyChangedCallback(DependencyObject depObj, DependencyPropertyChangedEventArgs args) {
            var element = (FrameworkElement)depObj;
            if (element == null) return;

            if ((bool)args.NewValue) {
                element.AddMouseDown(OnMouseDown);
                element.AddMouseUp(OnMouseUp);
                element.AddMouseLeave(OnMouseLeave);
            } else {
                element.RemoveMouseDown(OnMouseDown);
                element.RemoveMouseUp(OnMouseUp);
                element.RemoveMouseLeave(OnMouseLeave);
            }
        }

        static void OnMouseLeave(object sender, MouseEventArgs e) {
            var element = (FrameworkElement)sender;
            element?.SetValue(IsPressedProperty, false);
        }

        static void OnMouseUp(object sender, MouseButtonEventArgs e) {
            var element = (FrameworkElement)sender;
            element?.SetValue(IsPressedProperty, false);
        }

        static void OnMouseDown(object sender, MouseButtonEventArgs e) {
            var element = (FrameworkElement)sender;
            element?.SetValue(IsPressedProperty, true);
        }

        public static bool GetIsPressed(UIElement element) {
            return element.GetValue(IsPressedProperty) as bool? == true;
        }

        public static void SetIsPressed(UIElement element, bool val) {
            element.SetValue(IsPressedProperty, val);
        }

        public static bool GetAttachIsPressed(UIElement element) {
            return element.GetValue(AttachIsPressedProperty) as bool? == true;
        }

        public static void SetAttachIsPressed(UIElement element, bool val) {
            element.SetValue(AttachIsPressedProperty, val);
        }
    }
}