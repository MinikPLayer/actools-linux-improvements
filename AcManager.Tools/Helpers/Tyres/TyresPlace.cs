using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AcManager.Tools.Helpers.Tyres {
    public class TyresPlace : Border {
        protected override System.Type StyleKeyOverride => typeof(TyresPlace);

        public static readonly StyledProperty<Brush> HighlightColorProperty = DP.Register<Brush>(nameof(HighlightColor),
                typeof(TyresPlace));

        public Brush HighlightColor {
            get => (Brush)GetValue(HighlightColorProperty);
            set => SetValue(HighlightColorProperty, value);
        }

        public static readonly StyledProperty<TyresAppropriateLevel> LevelProperty = DP.Register<TyresAppropriateLevel>(nameof(Level),
                typeof(TyresPlace));

        public TyresAppropriateLevel Level {
            get => GetValue(LevelProperty) as TyresAppropriateLevel? ?? TyresAppropriateLevel.F;
            set => SetValue(LevelProperty, value);
        }
    }
}