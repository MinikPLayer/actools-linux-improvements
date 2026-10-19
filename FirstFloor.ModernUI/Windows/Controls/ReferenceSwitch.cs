using System.Windows;
using System.Windows.Markup;

namespace FirstFloor.ModernUI.Windows.Controls {
    [ContentProperty(nameof(NonNull))]
    public class ReferenceSwitch : BaseSwitch {
        public static readonly StyledProperty<object> ValueProperty = DP.Register<object>(nameof(Value),
                typeof(ReferenceSwitch), new PropertyMetadata(null, (o, e) => {
                    ((ReferenceSwitch)o)._value = e.NewValue;
                    OnChildSelectingPropertyChanged(o, e);
                }));

        private object _value;

        public object Value {
            get => _value;
            set => SetValue(ValueProperty, value);
        }

        public static readonly StyledProperty<UIElement> NullProperty = DP.Register<UIElement>(nameof(Null),
                typeof(ReferenceSwitch), new PropertyMetadata(null, (o, e) => {
                    ((ReferenceSwitch)o)._null = (UIElement)e.NewValue;
                    OnChildRegisteringPropertyChanged(o, e);
                }));

        private UIElement _null;

        public UIElement Null {
            get => _null;
            set => SetValue(NullProperty, value);
        }

        public static readonly StyledProperty<UIElement> NonNullProperty = DP.Register<UIElement>(nameof(NonNull),
                typeof(ReferenceSwitch), new PropertyMetadata(null, (o, e) => {
                    ((ReferenceSwitch)o)._nonNull = (UIElement)e.NewValue;
                    OnChildRegisteringPropertyChanged(o, e);
                }));

        private UIElement _nonNull;

        [Avalonia.Metadata.Content] public UIElement NonNull {
            get => _nonNull;
            set => SetValue(NonNullProperty, value);
        }

        protected override UIElement GetChild() {
            return _value == null ? _null : _nonNull;
        }
    }
}