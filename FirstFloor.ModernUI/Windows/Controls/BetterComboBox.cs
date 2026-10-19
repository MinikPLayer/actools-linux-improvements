using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using System.Windows.Input;
using Avalonia.Input;
using FirstFloor.ModernUI.Windows.Attached;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class BetterComboBox : ComboBox {
        static BetterComboBox() {
            DP.OverrideMetadata(IsEditableProperty, typeof(BetterComboBox), new FrameworkPropertyMetadata(true));
        }

        public BetterComboBox() {
            AddHandler(PointerWheelChangedEvent, OnMouseWheel, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            Loaded += OnLoaded;
            SelectionChanged += (sender, e) => OnSelectionChanged(e);
        }

        public static readonly StyledProperty<bool> IsReadOnlyProperty = TextBox.IsReadOnlyProperty.AddOwner<BetterComboBox>();

        public bool IsReadOnly {
            get => GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        public static readonly StyledProperty<int> MaxLengthProperty = DP.Register<int>(nameof(MaxLength),
                typeof(BetterComboBox));

        public int MaxLength {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        public static readonly StyledProperty<TextWrapping> TextWrappingProperty = DP.Register<TextWrapping>(nameof(TextWrapping),
                typeof(BetterComboBox), new FrameworkPropertyMetadata(TextWrapping.NoWrap));

        public TextWrapping TextWrapping {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public static readonly StyledProperty<bool> AcceptsReturnProperty = DP.Register<bool>(nameof(AcceptsReturn),
                typeof(BetterComboBox));

        public bool AcceptsReturn {
            get => (bool)GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        public static readonly StyledProperty<bool> AcceptsTabProperty = DP.Register<bool>(nameof(AcceptsTab),
                typeof(BetterComboBox));

        public bool AcceptsTab {
            get => (bool)GetValue(AcceptsTabProperty);
            set => SetValue(AcceptsTabProperty, value);
        }

        public static readonly StyledProperty<string> PlaceholderProperty = DP.Register<string>(nameof(Placeholder),
                typeof(BetterComboBox));

        public string Placeholder {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        private void OnMouseWheel(object sender, PointerWheelEventArgs e) {
            if (IsDropDownOpen) return;
            e.Handled = true;
            var scroll = Windows.Media.VisualTreeHelperEx.GetParent<ScrollViewer>(this);
            scroll?.ScrollToVerticalOffset(scroll.VerticalOffset - e.Delta.Y * 48d);
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape:
                    e.Handled = this.RemoveFocus();
                    break;

                case Key.Enter:
                    if (!AcceptsReturn) {
                        e.Handled = this.MoveFocus();
                    }
                    break;

                case Key.Tab:
                    if (!AcceptsTab && Keyboard.Modifiers == ModifierKeys.Shift && this.MoveFocus(FocusNavigationDirection.Previous)) {
                        e.Handled = true;
                    }
                    break;
            }

            if (!e.Handled) {
                base.OnKeyDown(e);
            }
        }

        public new static readonly StyledProperty<object> SelectedItemProperty = DP.Register<object>(nameof(SelectedItem),
                typeof(BetterComboBox), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        [CanBeNull]
        public new object SelectedItem {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((BetterComboBox)o).OnSelectedItemChanged(e.NewValue);
        }

        private void OnSelectedItemChanged(object newValue) {
            SetValue(SelectingItemsControl.SelectedItemProperty, newValue ?? NullValue);
        }

        public static readonly StyledProperty<bool> HoldSelectedIfEmptyProperty = DP.Register<bool>(nameof(HoldSelectedIfEmpty),
                typeof(BetterComboBox), new PropertyMetadata(false, (o, e) =>
                        ((BetterComboBox)o)._holdSelectedIfEmpty = (bool)e.NewValue));

        private bool _holdSelectedIfEmpty;

        public bool HoldSelectedIfEmpty {
            get => _holdSelectedIfEmpty;
            set => SetValue(HoldSelectedIfEmptyProperty, value);
        }

        public static bool IgnoreUnloadedChanges = false;
        private bool _updateSelectedItemLater;

        private bool CanUpdateSelected() {
            return !HoldSelectedIfEmpty || ItemsSource?.OfType<object>().Any() == true;
        }

        protected virtual void OnSelectionChanged(SelectionChangedEventArgs e) {
            if (!CanUpdateSelected()) return;
            if (!IgnoreUnloadedChanges || IsLoaded) {
                SelectedItem = ReferenceEquals(base.SelectedItem, NullValue) ? null : base.SelectedItem;
            } else {
                _updateSelectedItemLater = true;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (_updateSelectedItemLater) {
                _updateSelectedItemLater = false;
                if (CanUpdateSelected()) {
                    SelectedItem = ReferenceEquals(base.SelectedItem, NullValue) ? null : base.SelectedItem;
                }
            }
        }

        private class NullValueClass {
            public override string ToString() {
                return @"null";
            }
        }

        public static readonly object NullValue = new NullValueClass();

        private class NullPrefixedCollection : BetterObservableCollection<object> {
            private readonly IEnumerable _collection;

            public NullPrefixedCollection(IEnumerable collection)
                    // ReSharper disable once PossibleMultipleEnumeration
                    : base(new[] { NullValue }.Concat(collection.OfType<object>())) {
                if (collection is INotifyCollectionChanged o) {
                    // ReSharper disable once PossibleMultipleEnumeration
                    _collection = collection;
                    o.CollectionChanged += OnCollectionChanged;
                }
            }

            private void Rebuild() {
                ReplaceEverythingBy(new[] { NullValue }.Concat(_collection.OfType<object>()));
            }

            private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
                switch (e.Action) {
                    case NotifyCollectionChangedAction.Add:
                        if (e.NewItems?.Count == 1 && e.NewStartingIndex + 1 > 0) {
                            if (e.NewStartingIndex + 1 >= Count) {
                                Add(e.NewItems[0]);
                            } else {
                                Insert(e.NewStartingIndex + 1, e.NewItems[0]);
                            }
                        } else {
                            Rebuild();
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        if (e.OldItems != null) {
                            foreach (var oldItem in e.OldItems) {
                                Remove(oldItem);
                            }
                        } else {
                            Rebuild();
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (e.OldItems?.Count == 1 && e.NewItems?.Count == 1) {
                            var i = IndexOf(e.OldItems[0]);
                            if (i != -1) {
                                this[i] = e.NewItems[0];
                            } else {
                                Rebuild();
                            }
                        } else {
                            Rebuild();
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        Move(e.OldStartingIndex + 1, e.NewStartingIndex + 1);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        Rebuild();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void UpdateItemsSource() {
            var items = ItemsSource;
            var nullable = Nullable;
            SetValue(ItemsControl.ItemsSourceProperty,
                    nullable ? items == null ? (IEnumerable)new object[] { null } : new NullPrefixedCollection(items) : ItemsSource);
            if (nullable && base.SelectedItem == null) {
                base.SelectedItem = NullValue;
            }
        }

        public new static readonly StyledProperty<IEnumerable> ItemsSourceProperty = DP.Register<IEnumerable>(nameof(ItemsSource),
                typeof(BetterComboBox), new PropertyMetadata(OnItemsSourceChanged));

        public new IEnumerable ItemsSource {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((BetterComboBox)o).UpdateItemsSource();
        }

        public static readonly StyledProperty<bool> NullableProperty = DP.Register<bool>(nameof(Nullable),
                typeof(BetterComboBox), new PropertyMetadata(OnNullableChanged));

        public bool Nullable {
            get => GetValue(NullableProperty) as bool? == true;
            set => SetValue(NullableProperty, value);
        }

        private static void OnNullableChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((BetterComboBox)o).UpdateItemsSource();
        }
    }
}