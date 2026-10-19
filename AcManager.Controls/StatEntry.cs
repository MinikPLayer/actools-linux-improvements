using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AcManager.Controls {
    public class StatEntry : Control {
        public static readonly StyledProperty<string> TitleProperty = DP.Register<string>(nameof(Title),
                typeof(StatEntry));

        public string Title {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly StyledProperty<string> ValueProperty = DP.Register<string>(nameof(Value),
                typeof(StatEntry));

        public string Value {
            get => (string)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly StyledProperty<string> ValueUnitsProperty = DP.Register<string>(nameof(ValueUnits),
                typeof(StatEntry));

        public string ValueUnits {
            get => (string)GetValue(ValueUnitsProperty);
            set => SetValue(ValueUnitsProperty, value);
        }

        public static readonly StyledProperty<string> CarIdProperty = DP.Register<string>(nameof(CarId),
                typeof(StatEntry));

        public string CarId {
            get => (string)GetValue(CarIdProperty);
            set => SetValue(CarIdProperty, value);
        }

        public static readonly StyledProperty<string> TrackIdProperty = DP.Register<string>(nameof(TrackId),
                typeof(StatEntry));

        public string TrackId {
            get => (string)GetValue(TrackIdProperty);
            set => SetValue(TrackIdProperty, value);
        }

        public static readonly StyledProperty<Geometry> IconProperty = DP.Register<Geometry>(nameof(Icon),
                typeof(StatEntry));

        public Geometry Icon {
            get => (Geometry)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }
    }
}