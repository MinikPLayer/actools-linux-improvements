using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace AcManager.Controls {
    [ContentProperty(nameof(Children))]
    public class MessageBlock : ContentControl {
        static MessageBlock() {
        }

        public MessageBlock() {
            Children = new UIElementCollection(this, this);
        }

        public static readonly StyledProperty<UIElementCollection> ChildrenProperty = DP.Register<UIElementCollection>(nameof(Children),
                typeof(MessageBlock));

        [Avalonia.Metadata.Content] public UIElementCollection Children {
            get => (UIElementCollection)GetValue(ChildrenProperty);
            set => SetValue(ChildrenProperty, value);
        }

        private Panel _panel;
        private Button _closeButton;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            if (_closeButton != null) {
                _closeButton.Click -= OnCloseButtonClick;
            }

            base.OnApplyTemplate(e);

            _panel = this.GetTemplateChild(@"PART_Panel") as Panel;
            if (_panel != null) {
                var newChildren = _panel.Children;
                if (!ReferenceEquals(Children, newChildren)) {
                    var children = Children.OfType<UIElement>().ToList();
                    Children.Clear();

                    foreach (var child in children) {
                        newChildren.Add(child);
                    }

                    Children = newChildren;
                }
            }

            _closeButton = this.GetTemplateChild(@"PART_CloseButton") as Button;
            if (_closeButton != null) {
                _closeButton.Click += OnCloseButtonClick;
            }
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e) {
            if (CloseKey != null) {
                ValuesStorage.Set("msgblock:" + CloseKey, true);
                SetClosed(true);
            }
        }

        public new static readonly StyledProperty<Visibility> VisibilityProperty = DP.Register<Visibility>(nameof(Visibility),
                typeof(MessageBlock), new PropertyMetadata(Visibility.Visible, (o, e) => {
                    var b = (MessageBlock)o;
                    b._visibility = (Visibility)e.NewValue;
                    b.UpdateVisibility();
                }));

        private Visibility _visibility = Visibility.Visible;

        public new Visibility Visibility {
            get => _visibility;
            set => SetValue(VisibilityProperty, value);
        }

        public static readonly StyledProperty<string> CloseKeyProperty = DP.Register<string>(nameof(CloseKey),
                typeof(MessageBlock), new PropertyMetadata(null, (o, e) => {
                    var b = (MessageBlock)o;
                    b._closeKey = (string)e.NewValue;
                    b.OnCloseKeyChanged();
                }));

        private string _closeKey;

        [CanBeNull]
        public string CloseKey {
            get => _closeKey;
            set => SetValue(CloseKeyProperty, value);
        }

        private void OnCloseKeyChanged() {
            SetClosed(ValuesStorage.Get("msgblock:" + CloseKey, false));
        }

        private void SetClosed(bool value) {
            SetValue(ClosedPropertyKey.DependencyProperty, value);
            UpdateVisibility();
        }

        private void UpdateVisibility() {
            base.Visibility = Closed ? Visibility.Collapsed : Visibility;
        }

        public static readonly DependencyPropertyKey ClosedPropertyKey = DP.RegisterReadOnly(nameof(Closed), typeof(bool),
                typeof(MessageBlock), new PropertyMetadata(false));

        public static readonly DependencyProperty ClosedProperty = ClosedPropertyKey.DependencyProperty;

        public bool Closed => GetValue(ClosedProperty) as bool? == true;

        public static readonly StyledProperty<object> CloseButtonContentProperty = DP.Register<object>(nameof(CloseButtonContent),
                typeof(MessageBlock), new PropertyMetadata(null));

        public object CloseButtonContent {
            get => GetValue(CloseButtonContentProperty);
            set => SetValue(CloseButtonContentProperty, value);
        }

        public static readonly StyledProperty<string> CloseButtonContentStringFormatProperty = DP.Register<string>(nameof(CloseButtonContentStringFormat),
                typeof(MessageBlock));

        public string CloseButtonContentStringFormat {
            get => (string)GetValue(CloseButtonContentStringFormatProperty);
            set => SetValue(CloseButtonContentStringFormatProperty, value);
        }
    }
}