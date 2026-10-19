using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using FirstFloor.ModernUI.Windows.Attached;
using FirstFloor.ModernUI.Windows.Media;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class ContextMenuButtonEventArgs : HandledEventArgs {
        internal ContextMenuButtonEventArgs() {}

        public object Menu { internal get; set; }
    }

    [ContentProperty(nameof(Menu))]
    public class ContextMenuButton : Control {
        public ContextMenuButton() {
        }

        public static readonly DependencyProperty LookForParentMenuProperty = DP.Register(nameof(LookForParentMenu), typeof(bool),
                typeof(ContextMenuButton), new PropertyMetadata(false, (o, e) => {
                    ((ContextMenuButton)o)._lookForParentMenu = (bool)e.NewValue;
                }));

        private bool _lookForParentMenu;

        public bool LookForParentMenu {
            get => _lookForParentMenu;
            set => SetValue(LookForParentMenuProperty, value);
        }

        public event EventHandler<ContextMenuButtonEventArgs> Click;

        private bool Open(bool near) {
            var args = new ContextMenuButtonEventArgs();
            Click?.Invoke(this, args);
            if (args.Handled) return true;

            var menu = args.Menu ?? Menu ?? (LookForParentMenu ?
                    this.GetParents().OfType<FrameworkElement>().Select(x => x.ContextMenu).FirstOrDefault(x => x != null) : null);


            if ((menu as FrameworkElement)?.DataContext == null) {
                ((FrameworkElement)menu)?.SetBinding(DataContextProperty, new Binding {
                    Source = this,
                    Path = new PropertyPath(nameof(DataContext))
                });
            }

            if (ForceNear.HasValue) {
                near = ForceNear.Value;
            }

            if (menu is Popup popup) {
                if (popup.IsOpen) {
                    popup.IsOpen = false;
                } else {
                    popup.Placement = near ? PlacementMode.Bottom : PlacementMode.Pointer;
                    popup.PlacementTarget = near ? this : Avalonia.VisualTree.VisualExtensions.GetVisualRoot(this) as Avalonia.Visual;
                    popup.IsOpen = true;
                    popup.StaysOpen = false;
                }
                return true;
            }

            if (menu is ContextMenu contextMenu) {
                contextMenu.Placement = near ? PlacementMode.Bottom : PlacementMode.Pointer;
                contextMenu.PlacementTarget = near ? this : null;
                if (near) {
                    contextMenu.Open(this);
                } else {
                    contextMenu.Open();
                }
                return true;
            }

            var command = Command;
            if (command != null) {
                command.Execute(CommandParameter);
                return true;
            }

            return false;
        }

        public static readonly DependencyProperty ForceNearProperty = DP.Register(nameof(ForceNear), typeof(bool?),
                typeof(ContextMenuButton), new PropertyMetadata(null, (o, e) => {
                    ((ContextMenuButton)o)._forceNear = (bool?)e.NewValue;
                }));

        private bool? _forceNear;

        public bool? ForceNear {
            get => _forceNear;
            set => SetValue(ForceNearProperty, value);
        }

        public static readonly DependencyProperty ExtraDelayProperty = DP.Register(nameof(ExtraDelay), typeof(bool),
                typeof(ContextMenuButton), new PropertyMetadata(false, (o, e) => {
                    ((ContextMenuButton)o)._extraDelay = (bool)e.NewValue;
                }));

        private bool _extraDelay;

        public bool ExtraDelay {
            get => _extraDelay;
            set => SetValue(ExtraDelayProperty, value);
        }

        private FrameworkElement _child;
        private FrameworkElement _button;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            if (_button != null) {
                _button.RemovePreviewMouseLeftButtonDown(OnMouseDown);
                _button.RemovePreviewMouseLeftButtonUp(OnButtonClick);
                _button.RemoveMouseRightButtonUp(OnButtonDown);
            }

            base.OnApplyTemplate(e);

            _button = this.GetTemplateChild(@"PART_Button") as FrameworkElement;
            if (_button != null) {
                _button.AddPreviewMouseLeftButtonDown(OnMouseDown);
                _button.AddPreviewMouseLeftButtonUp(OnButtonClick);
                _button.AddMouseRightButtonUp(OnButtonDown);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e) {
            e.Handled = true;
        }

        private void OnButtonDown(object sender, MouseButtonEventArgs e) {
            if (!e.Handled && Command != null) {
                e.Handled = true;
            }
        }

        private async void OnButtonClick(object sender, MouseButtonEventArgs e) {
            if (ExtraDelay) {
                await Task.Delay(1);
            }

            if (!e.Handled && Open(true)) {
                e.Handled = true;
            }
        }

        private async void OnContextMenuClick(object sender, MouseButtonEventArgs e) {
            if (Parent.FindVisualChildren<FrameworkElement>().Any(child => child.ContextMenu != null)) {
                await Task.Delay(1);
            }

            if (!e.Handled && Open(false)) {
                e.Handled = true;
            }
        }

        private FrameworkElement _observedParent;

        protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e) {
            base.OnAttachedToLogicalTree(e);
            Unsubscribe();

            var fe = Parent as FrameworkElement;
            if (fe != null) {
                _observedParent = fe;
                fe.AddPreviewMouseRightButtonUp(OnContextMenuPreviewClick);
                fe.AddMouseRightButtonUp(OnContextMenuClick);

                if (fe is Panel panel && panel.Background == null) {
                    panel.Background = Brushes.Transparent;
                }
            }
        }

        protected override void OnDetachedFromLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e) {
            base.OnDetachedFromLogicalTree(e);
            Unsubscribe();
        }

        private void Unsubscribe() {
            var fe = _observedParent;
            if (fe != null) {
                fe.RemovePreviewMouseRightButtonUp(OnContextMenuPreviewClick);
                fe.RemoveMouseRightButtonUp(OnContextMenuClick);
                _observedParent = null;
            }
        }

        private void OnContextMenuPreviewClick(object sender, MouseButtonEventArgs e) {
            if (Menu is ContextMenu menu) {
                ContextMenuAdvancement.Add(this, menu);
            }
        }

        public static readonly DependencyProperty MenuProperty = DP.Register(nameof(Menu), typeof(FrameworkElement),
                typeof(ContextMenuButton), new PropertyMetadata(OnMenuChanged));

        public FrameworkElement Menu {
            get => (FrameworkElement)GetValue(MenuProperty);
            set => SetValue(MenuProperty, value);
        }

        private static void OnMenuChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ContextMenuButton)o).OnMenuChanged((FrameworkElement)e.OldValue, (FrameworkElement)e.NewValue);
        }

        private void OnMenuChanged(FrameworkElement oldValue, FrameworkElement newValue) {
            if (oldValue != null) {
                if (oldValue is ContextMenu) {
                    oldValue.ClearValue(DataContextProperty);
                } else {
                    LogicalChildren.Remove(oldValue);
                }
            }

            if (newValue != null) {
                if (newValue is ContextMenu) {
                    _child = null;
                    newValue.SetBinding(DataContextProperty, new Binding {
                        Path = new PropertyPath(nameof(DataContext)),
                        Source = this
                    });
                } else {
                    _child = newValue;
                    LogicalChildren.Add(newValue);
                }
            } else {
                _child = null;
            }
        }

        public static readonly DependencyProperty CommandProperty = DP.Register(nameof(Command), typeof(ICommand),
            typeof(ContextMenuButton));

        public ICommand Command {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandParameterProperty = DP.Register(nameof(CommandParameter), typeof(object),
                typeof(ContextMenuButton));

        public object CommandParameter {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        public static readonly DependencyProperty PropagateToChildrenProperty = DP.Register(nameof(PropagateToChildren), typeof(bool),
                typeof(ContextMenuButton));

        public bool PropagateToChildren {
            get => GetValue(PropagateToChildrenProperty) as bool? == true;
            set => SetValue(PropagateToChildrenProperty, value);
        }
    }
}