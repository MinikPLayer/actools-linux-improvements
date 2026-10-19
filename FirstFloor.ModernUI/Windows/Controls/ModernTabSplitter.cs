using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class ModernTabSplitter : GridSplitter {
        public ModernTabSplitter() {
            DragCompleted += OnDragCompleted;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            SetMaxWidth();
            SetWidth(LoadWidth());
        }

        public static readonly RoutedEvent<MovedEventArgs> MovedEvent = (RoutedEvent<MovedEventArgs>)EventManager.RegisterRoutedEvent(nameof(Moved), RoutingStrategy.Bubble,
                typeof(EventHandler<MovedEventArgs>), typeof(ModernTabSplitter));

        public event EventHandler<MovedEventArgs> Moved {
            add => AddHandler(MovedEvent, value);
            remove => RemoveHandler(MovedEvent, value);
        }

        public class MovedEventArgs : RoutedEventArgs {
            public double NewWidth { get; }

            public MovedEventArgs(RoutedEvent routedEvent, double newWidth) : base(routedEvent) {
                NewWidth = newWidth;
            }
        }

        private FrameworkElement _observedParent;

        protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e) {
            base.OnAttachedToLogicalTree(e);

            if (_observedParent != null) {
                _observedParent.SizeChanged -= OnParentSizeChanged;
            }

            _observedParent = Parent as FrameworkElement;
            if (_observedParent != null) {
                _observedParent.SizeChanged += OnParentSizeChanged;
            }

            if (IsLoaded) {
                SetMaxWidth();
                SetWidth(LoadWidth());
            }
        }

        protected override void OnDetachedFromLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e) {
            base.OnDetachedFromLogicalTree(e);
            if (_observedParent != null) {
                _observedParent.SizeChanged -= OnParentSizeChanged;
                _observedParent = null;
            }
        }

        private void OnParentSizeChanged(object sender, SizeChangedEventArgs sizeChangedEventArgs) {
            if (IsLoaded) {
                SetMaxWidth();
            }
        }

        private void SetWidth(double width) {
            var column = GetTargetColumn();
            if (column != null) {
                column.Width = new GridLength(width, GridUnitType.Pixel);
                RaiseEvent(new MovedEventArgs(MovedEvent, width));
            }
        }

        [CanBeNull]
        private ColumnDefinition GetTargetColumn() {
            return AdjustRightColumn ?
                    (Parent as Grid)?.ColumnDefinitions.LastOrDefault() :
                    (Parent as Grid)?.ColumnDefinitions.FirstOrDefault();
        }

        private void SetMaxWidth() {
            var target = GetTargetColumn();
            if (target == null) return;

            var grid = (Grid)Parent;
            var maxWidthAllowed = Math.Max(grid.ActualWidth - grid.ColumnDefinitions.Where(x =>
                    !ReferenceEquals(x, target)).Sum(x => x.Width.IsAbsolute ? x.ActualWidth : x.MinWidth), 100);

            target.MaxWidth = maxWidthAllowed;
            if (target.ActualWidth > maxWidthAllowed - 10) {
                target.Width = new GridLength(maxWidthAllowed - 10, GridUnitType.Pixel);
            }
        }

        public double? GetWidth() {
            return GetTargetColumn()?.ActualWidth;
        }

        private double LoadWidth() {
            return ValuesStorage.Get(SaveKeyValue, InitialWidth);
        }

        private void SaveWidth(double value) {
            ValuesStorage.Set(SaveKeyValue, value);
        }

        private void OnDragCompleted(object sender, VectorEventArgs e) {
            if (SaveKey != null) {
                var value = GetWidth();
                if (value.HasValue) {
                    SaveWidth(value.Value);
                }
            }
        }

        public static readonly StyledProperty<bool> AdjustRightColumnProperty = DP.Register<bool>(nameof(AdjustRightColumn),
                typeof(ModernTabSplitter), new PropertyMetadata(false, (o, e) => {
                    ((ModernTabSplitter)o)._adjustRightColumn = (bool)e.NewValue;
                }));

        private bool _adjustRightColumn;

        public bool AdjustRightColumn {
            get => _adjustRightColumn;
            set => SetValue(AdjustRightColumnProperty, value);
        }

        public static readonly StyledProperty<double> InitialWidthProperty = DP.Register<double>(nameof(InitialWidth),
                typeof(ModernTabSplitter), new PropertyMetadata(200d));

        public double InitialWidth {
            get => GetValue(InitialWidthProperty) as double? ?? 200d;
            set => SetValue(InitialWidthProperty, value);
        }

        private string SaveKeyValue => $"ModernTabSplitter:{SaveKey}";

        public static readonly StyledProperty<string> SaveKeyProperty = DP.Register<string>(nameof(SaveKey),
                typeof(ModernTabSplitter), new PropertyMetadata(@"ModernTab", OnSaveKeyChanged));

        public string SaveKey {
            get => (string)GetValue(SaveKeyProperty);
            set => SetValue(SaveKeyProperty, value);
        }

        private static void OnSaveKeyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            //((ModernTabSplitter)o).OnSaveKeyChanged();
        }

        /*private void OnSaveKeyChanged() {
            if (!IsLoaded) return;
            SetWidth(LoadWidth());
        }*/
    }
}