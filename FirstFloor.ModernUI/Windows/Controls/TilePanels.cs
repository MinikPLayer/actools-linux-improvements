using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Tiles of the same size going left to right (or top to bottom) and wrapping. WPF version of this panel was virtualizing; Avalonia’s
    /// virtualization works differently (see LinuxPortDocs.md), so this one lays out all of its children.
    /// </summary>
    public class VirtualizingTilePanel : Panel {
        public static readonly StyledProperty<Orientation> OrientationProperty = AvaloniaProperty.Register<VirtualizingTilePanel, Orientation>(
                nameof(Orientation), Orientation.Horizontal);

        public static readonly StyledProperty<double> ItemWidthProperty = AvaloniaProperty.Register<VirtualizingTilePanel, double>(
                nameof(ItemWidth), double.PositiveInfinity);

        public static readonly StyledProperty<double> ItemHeightProperty = AvaloniaProperty.Register<VirtualizingTilePanel, double>(
                nameof(ItemHeight), double.PositiveInfinity);

        public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
        public double ItemWidth { get => GetValue(ItemWidthProperty); set => SetValue(ItemWidthProperty, value); }
        public double ItemHeight { get => GetValue(ItemHeightProperty); set => SetValue(ItemHeightProperty, value); }

        static VirtualizingTilePanel() {
            AffectsMeasure<VirtualizingTilePanel>(OrientationProperty, ItemWidthProperty, ItemHeightProperty);
        }

        private Size _tile;
        private int _perLine;

        protected override Size MeasureOverride(Size availableSize) {
            var tw = ItemWidth;
            var th = ItemHeight;
            if (double.IsInfinity(tw) || double.IsInfinity(th)) {
                // item size is not set: tile size is the biggest of desired ones
                var w = 0d;
                var h = 0d;
                foreach (var child in Children) {
                    child.Measure(Size.Infinity);
                    w = Math.Max(w, child.DesiredSize.Width);
                    h = Math.Max(h, child.DesiredSize.Height);
                }
                if (double.IsInfinity(tw)) tw = w;
                if (double.IsInfinity(th)) th = h;
            }

            _tile = new Size(tw, th);
            foreach (var child in Children) {
                child.Measure(_tile);
            }

            if (tw <= 0 || th <= 0 || Children.Count == 0) {
                _perLine = 1;
                return default;
            }

            var horizontal = Orientation == Orientation.Horizontal;
            var limit = horizontal ? availableSize.Width : availableSize.Height;
            _perLine = double.IsInfinity(limit) ? Children.Count : Math.Max(1, (int)Math.Floor(limit / (horizontal ? tw : th)));
            var lines = (int)Math.Ceiling((double)Children.Count / _perLine);
            var inLine = Math.Min(_perLine, Children.Count);
            return horizontal ? new Size(inLine * tw, lines * th) : new Size(lines * tw, inLine * th);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var horizontal = Orientation == Orientation.Horizontal;
            var i = 0;
            foreach (var child in Children) {
                var line = i / _perLine;
                var pos = i % _perLine;
                var x = horizontal ? pos * _tile.Width : line * _tile.Width;
                var y = horizontal ? line * _tile.Height : pos * _tile.Height;
                child.Arrange(new Rect(x, y, _tile.Width, _tile.Height));
                i++;
            }

            return finalSize;
        }
    }

    /// <summary>Fixed number of columns, tile height is a part of tile width.</summary>
    public class VirtualizingColumnsPanel : Panel {
        public static readonly StyledProperty<int> ColumnsProperty = AvaloniaProperty.Register<VirtualizingColumnsPanel, int>(nameof(Columns), 2);
        public static readonly StyledProperty<double> ItemHeightModifierProperty = AvaloniaProperty.Register<VirtualizingColumnsPanel, double>(
                nameof(ItemHeightModifier), 0.8);

        public int Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }
        public double ItemHeightModifier { get => GetValue(ItemHeightModifierProperty); set => SetValue(ItemHeightModifierProperty, value); }

        static VirtualizingColumnsPanel() {
            AffectsMeasure<VirtualizingColumnsPanel>(ColumnsProperty, ItemHeightModifierProperty);
        }

        private Size _tile;

        protected override Size MeasureOverride(Size availableSize) {
            var columns = Math.Max(1, Columns);
            var width = double.IsInfinity(availableSize.Width) ? 100d * columns : availableSize.Width;
            _tile = new Size(Math.Floor(width / columns), Math.Floor(width / columns) * ItemHeightModifier);
            foreach (var child in Children) {
                child.Measure(_tile);
            }

            var rows = (int)Math.Ceiling((double)Children.Count / columns);
            return new Size(columns * _tile.Width, rows * _tile.Height);
        }

        protected override Size ArrangeOverride(Size finalSize) {
            var columns = Math.Max(1, Columns);
            var i = 0;
            foreach (var child in Children) {
                child.Arrange(new Rect(i % columns * _tile.Width, i / columns * _tile.Height, _tile.Width, _tile.Height));
                i++;
            }

            return finalSize;
        }
    }

    public class VirtualizingWrapPanel : WrapPanel {
    }
}
