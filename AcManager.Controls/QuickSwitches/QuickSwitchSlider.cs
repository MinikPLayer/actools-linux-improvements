using System.Windows;
using System.Windows.Media;
using FirstFloor.ModernUI.Windows.Controls;

namespace AcManager.Controls.QuickSwitches {
    public class QuickSwitchSlider : RoundSlider {
        static QuickSwitchSlider() {
        }
        
        public static readonly StyledProperty<Geometry> IconDataProperty = DP.Register<Geometry>(nameof(IconData),
                typeof(QuickSwitchSlider));

        public Geometry IconData {
            get { return (Geometry)GetValue(IconDataProperty); }
            set { SetValue(IconDataProperty, value); }
        }

        public static readonly StyledProperty<string> DisplayValueProperty = DP.Register<string>(nameof(DisplayValue),
                typeof(QuickSwitchSlider));

        public string DisplayValue {
            get { return (string)GetValue(DisplayValueProperty); }
            set { SetValue(DisplayValueProperty, value); }
        }
    }
}