using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AcManager.Controls {
    public class FavouriteButton : ToggleButton {
        protected override System.Type StyleKeyOverride => typeof(FavouriteButton);

        static FavouriteButton() {
        }

        public static readonly StyledProperty<Brush> ActiveBrushProperty = DP.Register<Brush>(nameof(ActiveBrush),
                typeof(FavouriteButton));

        public Brush ActiveBrush {
            get => (Brush)GetValue(ActiveBrushProperty);
            set => SetValue(ActiveBrushProperty, value);
        }

        public static readonly StyledProperty<Avalonia.Media.Geometry> DataProperty = DP.Register<Avalonia.Media.Geometry>(nameof(Data),
                typeof(FavouriteButton));

        public Avalonia.Media.Geometry Data {
            get => (Avalonia.Media.Geometry)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}