using System.Windows;
using System.Windows.Input;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class ModernToggleButton : ModernButton {
        static ModernToggleButton() {
            InputElement.PointerPressedEvent.AddClassHandler<ModernToggleButton>((x, e) => { if (e is PointerEventArgs p && p.IsButton(MouseButton.Right)) x.OnPreviewMouseRightButtonDown(p); }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        }

        public static readonly StyledProperty<bool> IsCheckedProperty = DP.Register<bool>(nameof(IsChecked),
                typeof(ModernToggleButton), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsChecked {
            get => GetValue(IsCheckedProperty) as bool? == true;
            set => SetValue(IsCheckedProperty, value);
        }

        protected override void OnClick() {
            IsChecked = !IsChecked;
        }

        public static readonly StyledProperty<ICommand> MoreCommandProperty = DP.Register<ICommand>(nameof(MoreCommand),
            typeof(ModernToggleButton));

        public ICommand MoreCommand {
            get => (ICommand)GetValue(MoreCommandProperty);
            set => SetValue(MoreCommandProperty, value);
        }

        private ContextMenuButton _contextMenuButton;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            if (_contextMenuButton != null) {
                _contextMenuButton.Click -= OnContextMenuButtonClick;
                _contextMenuButton.RemovePreviewMouseDown(OnContextMenuButtonPreviewClick);
            }

            base.OnApplyTemplate(e);
            _contextMenuButton = this.GetTemplateChild("PART_MoreButton") as ContextMenuButton;

            if (_contextMenuButton != null) {
                _contextMenuButton.Click += OnContextMenuButtonClick;
                _contextMenuButton.AddPreviewMouseDown(OnContextMenuButtonPreviewClick);
            }
        }

        private void OnContextMenuButtonPreviewClick(object sender, MouseButtonEventArgs e) {
            var moreCommand = MoreCommand;
            if (moreCommand != null) {
                MoreCommand?.Execute(null);
                e.Handled = true;
            }
        }

        private void OnContextMenuButtonClick(object sender, ContextMenuButtonEventArgs e) {
            var moreCommand = MoreCommand;
            if (moreCommand != null) {
                MoreCommand?.Execute(null);
                e.Handled = true;
            }
        }

        protected void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e) {
            var moreCommand = MoreCommand;
            if (moreCommand != null) {
                MoreCommand?.Execute(null);
                e.Handled = true;
            }
        }
    }
}