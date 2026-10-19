using FirstFloor.ModernUI.Windows.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using AcManager.Controls.Presentation;
using AcManager.Tools.AcObjectsNew;

namespace AcManager.Controls {
    [ContentProperty(nameof(Content))]
    public class AcObjectBase : Control {
        static AcObjectBase() {
        }

        public AcObjectBase() {
            SetCurrentValue(ToolBarsProperty, new Collection<ToolBar>());
        }

        private Grid _main;
        private AcObjectHeaderSection _iconImage;
        private AcToolBar _toolBar;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            base.OnApplyTemplate(e);

            if (_main != null) {
                _main.RemoveMouseRightButtonUp(OnMainMouseUp);
            }

            if (_toolBar != null) {
                _toolBar.RemovePreviewMouseUp(OnToolBarMouseUp);
            }

            if (_iconImage != null) {
                _iconImage.IconMouseUp -= OnHeaderIconMouseUp;
            }

            _main = this.GetTemplateChild(@"PART_Main") as Grid;
            _toolBar = this.GetTemplateChild(@"PART_ToolBar") as AcToolBar;
            _iconImage = this.GetTemplateChild(@"PART_Header") as AcObjectHeaderSection;

            if (_main != null) {
                _main.AddMouseRightButtonUp(OnMainMouseUp);
            }

            if (_toolBar != null) {
                _toolBar.AddPreviewMouseUp(OnToolBarMouseUp);
            }

            if (_iconImage != null) {
                _iconImage.IconMouseUp += OnHeaderIconMouseUp;
            }
        }

        protected async void OnMainMouseUp(object sender, MouseButtonEventArgs e) {
            if (_toolBar == null || !AppAppearanceManager.Instance.PopupToolBars) return;

            await Task.Delay(1);
            if (e.Handled) return;

            e.Handled = true;
            _toolBar.IsActive = !_toolBar.IsActive;
        }

        protected void OnToolBarMouseUp(object sender, MouseButtonEventArgs e) {
            /*if (e.ChangedButton != MouseButton.Left) return;
            if (_toolBar == null) return;
            _toolBar.IsActive = false;*/
        }

        private void OnHeaderIconMouseUp(object sender, MouseButtonEventArgs e) {
            IconMouseUp?.Invoke(sender, e);
        }

        public event MouseButtonEventHandler IconMouseUp;

        public static readonly StyledProperty<AcObjectNew> AcObjectProperty = DP.Register<AcObjectNew>(nameof(AcObject),
                typeof(AcObjectBase));

        public AcObjectNew AcObject {
            get => (AcObjectNew)GetValue(AcObjectProperty);
            set => SetValue(AcObjectProperty, value);
        }

        public static readonly StyledProperty<bool> IsNameReadOnlyProperty = DP.Register<bool>(nameof(IsNameReadOnly),
                typeof(AcObjectBase), new PropertyMetadata(false));

        public bool IsNameReadOnly {
            get => GetValue(IsNameReadOnlyProperty) as bool? ?? default;
            set => SetValue(IsNameReadOnlyProperty, value);
        }

        public static readonly StyledProperty<bool> ShowIconProperty = DP.Register<bool>(nameof(ShowIcon),
                typeof(AcObjectBase));

        public bool ShowIcon {
            get => GetValue(ShowIconProperty) as bool? ?? default;
            set => SetValue(ShowIconProperty, value);
        }

        public static readonly StyledProperty<Brush> IconBackgroundProperty = DP.Register<Brush>(nameof(IconBackground),
                typeof(AcObjectBase));

        public Brush IconBackground {
            get => (Brush)GetValue(IconBackgroundProperty);
            set => SetValue(IconBackgroundProperty, value);
        }

        public static readonly StyledProperty<string> IconProperty = DP.Register<string>(nameof(Icon),
                typeof(AcObjectBase));

        public string Icon {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly StyledProperty<object> IconContentProperty = DP.Register<object>(nameof(IconContent),
                typeof(AcObjectBase));

        public object IconContent {
            get => GetValue(IconContentProperty);
            set => SetValue(IconContentProperty, value);
        }

        public static readonly StyledProperty<object> ContentProperty = DP.Register<object>(nameof(Content),
                typeof(AcObjectBase));

        [Avalonia.Metadata.Content] public object Content {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static readonly StyledProperty<Collection<ToolBar>> ToolBarsProperty = DP.Register<Collection<ToolBar>>(nameof(ToolBars),
                typeof(AcObjectBase));

        public Collection<ToolBar> ToolBars {
            get => (Collection<ToolBar>)GetValue(ToolBarsProperty);
            set => SetValue(ToolBarsProperty, value);
        }

        public static readonly StyledProperty<bool> ScrollableProperty = DP.Register<bool>(nameof(Scrollable),
                typeof(AcObjectBase), new PropertyMetadata(true));

        public bool Scrollable {
            get => GetValue(ScrollableProperty) as bool? ?? default;
            set => SetValue(ScrollableProperty, value);
        }

        public static readonly StyledProperty<Thickness> HeaderPaddingProperty = DP.Register<Thickness>(nameof(HeaderPadding),
                typeof(AcObjectBase));

        public Thickness HeaderPadding {
            get => GetValue(HeaderPaddingProperty) as Thickness? ?? default;
            set => SetValue(HeaderPaddingProperty, value);
        }
    }
}
