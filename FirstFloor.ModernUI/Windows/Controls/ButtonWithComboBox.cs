using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace FirstFloor.ModernUI.Windows.Controls {
    [ContentProperty(@"MenuItems")]
    public class ButtonWithComboBox : ContentControl {
        public ButtonWithComboBox() {
            MenuItems = new Collection<DependencyObject>();
            this.AddPreviewMouseRightButtonUp(OnRightClick);
        }

        private void OnRightClick(object sender, MouseButtonEventArgs args) {
            if (_item != null) {
                args.Handled = true;
                _item.IsSubmenuOpen = true;
            }
        }

        private MenuItem _item;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            if (_item != null) {
                _item.KeyDown -= OnMenuKeyDown;
            }

            base.OnApplyTemplate(e);

            _item = this.GetTemplateChild("PART_MenuItem") as MenuItem;
            if (_item != null) {
                _item.KeyDown += OnMenuKeyDown;
            }
        }

        private void OnMenuKeyDown(object sender, KeyEventArgs args) {
            if (_item == null) return;
            if (_item.IsSubmenuOpen != true && (args.Key == Key.Enter || args.Key == Key.Space)) {
                _item.IsSubmenuOpen = true;
            }
        }

        public IList MenuItems {
            get => (Collection<DependencyObject>)GetValue(MenuItemsProperty);
            set => SetValue(MenuItemsProperty, value);
        }

        public static readonly DependencyProperty MenuItemsProperty = DP.Register("MenuItems", typeof(IList),
                typeof(ButtonWithComboBox));

        public object ButtonToolTip {
            get => GetValue(ButtonToolTipProperty);
            set => SetValue(ButtonToolTipProperty, value);
        }

        public static readonly DependencyProperty ButtonToolTipProperty = DP.Register("ButtonToolTip", typeof(object),
                typeof(ButtonWithComboBox));

        public static readonly DependencyProperty CommandProperty = DP.Register(nameof(Command), typeof(ICommand),
                typeof(ButtonWithComboBox));

        public ICommand Command {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty = DP.Register(nameof(CommandParameter), typeof(object),
                typeof(ButtonWithComboBox));

        public object CommandParameter {
            get => (object)GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }
    }
}
