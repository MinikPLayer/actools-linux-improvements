using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FirstFloor.ModernUI.Windows.Attached {
    public static class ListBoxHelper {
        public static bool GetProperMultiSelectionMode(DependencyObject obj) {
            return obj.GetValue(ProperMultiSelectionModeProperty) as bool? == true;
        }

        public static void SetProperMultiSelectionMode(DependencyObject obj, bool value) {
            obj.SetValue(ProperMultiSelectionModeProperty, value);
        }

        public static readonly AttachedProperty<bool> ProperMultiSelectionModeProperty = DP.RegisterAttached<bool>("ProperMultiSelectionMode",
                typeof(ListBoxHelper), new UIPropertyMetadata(OnProperMultiSelectionModeChanged));

        private static void OnProperMultiSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is ListBox element) || !(e.NewValue is bool)) return;
            var newValue = (bool)e.NewValue;
            if (newValue) {
                element.SelectionMode = SelectionMode.Multiple;
                element.AddPreviewMouseDown(OnListBoxMouseDown);
                element.AddMouseUp(OnListBoxMouseUp);
                element.AddMouseLeave(OnListBoxMouseUp);
                element.AddPreviewKeyDown(OnListBoxKeyDown);
            } else {
                element.SelectionMode = SelectionMode.Single;
                element.RemovePreviewMouseDown(OnListBoxMouseDown);
                element.RemoveMouseUp(OnListBoxMouseUp);
                element.RemoveMouseLeave(OnListBoxMouseUp);
                element.RemovePreviewKeyDown(OnListBoxKeyDown);
            }
        }

        private static void OnListBoxMouseDown(object sender, MouseButtonEventArgs e) {
            var listBox = (ListBox)sender;
            listBox.SelectionMode = Keyboard.Modifiers == ModifierKeys.None ? SelectionMode.Multiple : SelectionMode.Multiple;
        }

        private static async void OnListBoxMouseUp(object sender, EventArgs e) {
            var listBox = (ListBox)sender;
            await Task.Delay(1);
            listBox.SelectionMode = SelectionMode.Multiple;
        }

        private static void OnListBoxKeyDown(object sender, KeyEventArgs e) {
            if (Keyboard.Modifiers != ModifierKeys.Control) return;

            switch (e.Key) {
                case Key.A:
                    ((ListBox)sender).SelectAll();
                    break;
                case Key.D:
                    ((ListBox)sender).SelectedItems.Clear();
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }
    }
}