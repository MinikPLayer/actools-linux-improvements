using System.Windows;
using System.Windows.Markup;

namespace FirstFloor.ModernUI.Windows.Controls {
    [ContentProperty(nameof(True))]
    public class BooleanSwitch : BaseSwitch {
        public static readonly StyledProperty<bool> ValueProperty = DP.Register<bool>(nameof(Value),
                typeof(BooleanSwitch), new PropertyMetadata(false, (o, e) => {
                    ((BooleanSwitch)o)._value = (bool)e.NewValue;
                    OnChildSelectingPropertyChanged(o, e);
                }));

        private bool _value;

        public bool Value {
            get => _value;
            set => SetValue(ValueProperty, value);
        }

        public static readonly StyledProperty<UIElement> TrueProperty = DP.Register<UIElement>(nameof(True),
                typeof(BooleanSwitch), new PropertyMetadata(null, (o, e) => {
                    ((BooleanSwitch)o)._true = (UIElement)e.NewValue;
                    OnChildRegisteringPropertyChanged(o, e);
                }));

        private UIElement _true;

        public UIElement True {
            get => _true;
            set => SetValue(TrueProperty, value);
        }

        public static readonly StyledProperty<UIElement> FalseProperty = DP.Register<UIElement>(nameof(False),
                typeof(BooleanSwitch), new PropertyMetadata(null, (o, e) => {
                    ((BooleanSwitch)o)._false = (UIElement)e.NewValue;
                    OnChildRegisteringPropertyChanged(o, e);
                }));

        private UIElement _false;

        public UIElement False {
            get => _false;
            set => SetValue(FalseProperty, value);
        }

        protected override UIElement GetChild() {
            var value = _value;
            if (_collapseOnFalse) {
                var targetVisibility = value ? Visibility.Visible : Visibility.Collapsed;
                if (this.Visibility != targetVisibility) {
                    this.Visibility = targetVisibility;
                }
            }
            return value ? _true : _false;
        }

        public static readonly StyledProperty<bool> CollapseOnFalseProperty = DP.Register<bool>(nameof(CollapseOnFalse),
                typeof(BooleanSwitch), new PropertyMetadata(false, (o, e) => {
                    var b = (BooleanSwitch)o;
                    b._collapseOnFalse = (bool)e.NewValue;
                    if (b._collapseOnFalse && !b.Value) {
                        b.Visibility = Visibility.Collapsed;
                    }
                }));

        private bool _collapseOnFalse;

        public bool CollapseOnFalse {
            get => _collapseOnFalse;
            set => SetValue(CollapseOnFalseProperty, value);
        }
    }
}