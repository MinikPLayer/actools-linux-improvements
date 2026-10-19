using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AcManager.Controls {
    public class FavouriteButton : ToggleButton {
        static FavouriteButton() {
        }

        public static readonly StyledProperty<Brush> ActiveBrushProperty = DP.Register<Brush>(nameof(ActiveBrush),
                typeof(FavouriteButton));

        public Brush ActiveBrush {
            get => (Brush)GetValue(ActiveBrushProperty);
            set => SetValue(ActiveBrushProperty, value);
        }

        public static readonly StyledProperty<PathData> DataProperty = DP.Register<PathData>(nameof(Data),
                typeof(FavouriteButton));

        public PathData Data {
            get => (PathData)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}