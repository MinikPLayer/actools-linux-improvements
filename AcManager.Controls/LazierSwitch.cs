using System.ComponentModel;
using System.Windows;
using System.Windows.Markup;
using AcTools.Utils;
using FirstFloor.ModernUI;
using FirstFloor.ModernUI.Windows.Controls;

namespace AcManager.Controls {
    [ContentProperty(nameof(NonNull))]
    public class LazierSwitch : BaseSwitch {
        public static readonly StyledProperty<Lazier> ValueProperty = DP.Register<Lazier>(nameof(Value),
                typeof(LazierSwitch), new FrameworkPropertyMetadata(null, OnValuePropertyChanged));

        protected static void OnValuePropertyChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (!(sender is LazierSwitch b)) return;
            var nv = e.NewValue as Lazier;
            b._value = nv;
            (e.OldValue as Lazier)?.UnsubscribeWeak(b.OnValueContentChanged);
            nv?.SubscribeWeak(b.OnValueContentChanged);
            nv?.StartSetting();
            OnChildSelectingPropertyChanged(sender, e);
        }

        private void OnValueContentChanged(object sender, PropertyChangedEventArgs args) {
            if (args.PropertyName == nameof(Lazier.IsSet) || args.PropertyName == nameof(Lazier.GenericValue)) {
                RefreshActiveChild();
            }
        }

        private Lazier _value;

        public Lazier Value {
            get => _value;
            set => SetValue(ValueProperty, value);
        }

        public static readonly StyledProperty<UIElement> NullProperty = DP.Register<UIElement>(nameof(Null),
                typeof(LazierSwitch), new PropertyMetadata(null, (o, e) => {
                    ((LazierSwitch)o)._null = (UIElement)e.NewValue;
                    OnChildRegisteringPropertyChanged(o, e);
                }));

        private UIElement _null;

        public UIElement Null {
            get => _null;
            set => SetValue(NullProperty, value);
        }

        public static readonly StyledProperty<UIElement> NonNullProperty = DP.Register<UIElement>(nameof(NonNull),
                typeof(LazierSwitch), new PropertyMetadata(null, (o, e) => {
                    ((LazierSwitch)o)._nonNull = (UIElement)e.NewValue;
                    OnChildRegisteringPropertyChanged(o, e);
                }));

        private UIElement _nonNull;

        [Avalonia.Metadata.Content] public UIElement NonNull {
            get => _nonNull;
            set => SetValue(NonNullProperty, value);
        }

        public static readonly StyledProperty<UIElement> LoadingProperty = DP.Register<UIElement>(nameof(Loading),
                typeof(LazierSwitch), new PropertyMetadata(null, (o, e) => {
                    ((LazierSwitch)o)._loading = (UIElement)e.NewValue;
                    OnChildRegisteringPropertyChanged(o, e);
                }));

        private UIElement _loading;

        public UIElement Loading {
            get => _loading;
            set => SetValue(LoadingProperty, value);
        }

        protected override UIElement GetChild() {
            var v = _value;
            if (v == null) return _nonNull;
            if (v.IsSet) return v.GenericValue == null ? _null : _nonNull;
            return _loading;
        }
    }
}