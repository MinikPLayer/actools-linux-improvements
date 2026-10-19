using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AcManager.Tools.AcObjectsNew;

namespace AcManager.Controls {
    public class AcObjectHeaderSection : Control {
        static AcObjectHeaderSection() {
        }

        public static readonly StyledProperty<AcCommonObject> AcObjectProperty = DP.Register<AcCommonObject>(nameof(AcObject),
                typeof(AcObjectHeaderSection));

        public AcCommonObject AcObject {
            get => (AcCommonObject)GetValue(AcObjectProperty);
            set => SetValue(AcObjectProperty, value);
        }

        public static readonly StyledProperty<bool> ShowIconProperty = DP.Register<bool>(nameof(ShowIcon),
                typeof(AcObjectHeaderSection));

        public bool ShowIcon {
            get => GetValue(ShowIconProperty) as bool? == true;
            set => SetValue(ShowIconProperty, value);
        }

        public static readonly StyledProperty<Brush> IconBackgroundProperty = DP.Register<Brush>(nameof(IconBackground),
                typeof(AcObjectHeaderSection));

        public Brush IconBackground {
            get => (Brush)GetValue(IconBackgroundProperty);
            set => SetValue(IconBackgroundProperty, value);
        }

        public static readonly StyledProperty<string> IconProperty = DP.Register<string>(nameof(Icon),
                typeof(AcObjectHeaderSection));

        public string Icon {
            get => (string)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly StyledProperty<object> IconContentProperty = DP.Register<object>(nameof(IconContent),
                typeof(AcObjectHeaderSection));

        public object IconContent {
            get => GetValue(IconContentProperty);
            set => SetValue(IconContentProperty, value);
        }

        private UIElement _iconImage;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            base.OnApplyTemplate(e);

            if (_iconImage != null) {
                _iconImage.RemoveMouseUp(OnIconMouseUp);
            }

            _iconImage = this.GetTemplateChild(@"PART_IconImage") as UIElement;

            if (_iconImage != null) {
                _iconImage.AddMouseUp(OnIconMouseUp);
            }
        }

        private void OnIconMouseUp(object sender, MouseButtonEventArgs e) {
            IconMouseUp?.Invoke(sender, e);
        }

        public event MouseButtonEventHandler IconMouseUp;
    }
}
