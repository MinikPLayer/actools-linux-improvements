using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FirstFloor.ModernUI.Windows.Attached {
    /// <summary>
    /// Executes the DoubleClickEvent.Command of an element on double click (replaces MouseBinding with LeftDoubleClick of WPF).
    /// </summary>
    public static class DoubleClickEvent {
        public static bool GetEnabled(DependencyObject obj) {
            return obj.GetValue(EnabledProperty) as bool? == true;
        }

        public static void SetEnabled(DependencyObject obj, bool value) {
            obj.SetValue(EnabledProperty, value);
        }

        public static readonly AttachedProperty<bool> EnabledProperty = DP.RegisterAttached<bool>("Enabled",
                typeof(DoubleClickEvent), new UIPropertyMetadata(OnEnabledChanged));

        public static ICommand GetCommand(DependencyObject obj) {
            return obj.GetValue(CommandProperty) as ICommand;
        }

        public static void SetCommand(DependencyObject obj, ICommand value) {
            obj.SetValue(CommandProperty, value);
        }

        public static readonly AttachedProperty<ICommand> CommandProperty = DP.RegisterAttached<ICommand>("Command",
                typeof(DoubleClickEvent), new UIPropertyMetadata(null));

        private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is FrameworkElement element) || !(e.NewValue is bool)) return;
            var newValue = (bool)e.NewValue;
            if (newValue) {
                element.AddPreviewMouseLeftButtonDown(OnClick);
            } else {
                element.RemovePreviewMouseLeftButtonDown(OnClick);
            }
        }

        private static void OnClick(object sender, MouseButtonEventArgs e) {
            if (e.ClickCount == 2 && sender is FrameworkElement element) {
                GetCommand(element)?.Execute(null);
            }
        }
    }
}
