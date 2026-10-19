using System.Windows;
using System.Windows.Media;

namespace AcManager.Controls.QuickSwitches {
    public class QuickSwitchPresetsControl : UserPresetsControl {
        static QuickSwitchPresetsControl() {
        }

        public static readonly StyledProperty<Geometry> IconDataProperty = DP.Register<Geometry>(nameof(IconData),
                typeof(QuickSwitchPresetsControl));

        public Geometry IconData {
            get => (Geometry)GetValue(IconDataProperty);
            set => SetValue(IconDataProperty, value);
        }
    }
}