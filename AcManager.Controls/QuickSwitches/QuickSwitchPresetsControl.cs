using System.Windows;
using System.Windows.Media;

namespace AcManager.Controls.QuickSwitches {
    public class QuickSwitchPresetsControl : UserPresetsControl {
        protected override System.Type StyleKeyOverride => typeof(QuickSwitchPresetsControl);

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