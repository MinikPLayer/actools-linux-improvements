using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AcManager.Controls.QuickSwitches {
    public class QuickSwitchComboBox : ComboBox {
        protected override System.Type StyleKeyOverride => typeof(QuickSwitchComboBox);

        static QuickSwitchComboBox() {
        }

        public static readonly StyledProperty<Geometry> IconDataProperty = DP.Register<Geometry>(nameof(IconData),
                typeof(QuickSwitchComboBox));

        public Geometry IconData {
            get { return (Geometry)GetValue(IconDataProperty); }
            set { SetValue(IconDataProperty, value); }
        }
    }
}