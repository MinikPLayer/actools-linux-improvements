using System.Windows;

namespace FirstFloor.ModernUI.Presentation {
    public class BindingProxy : Freezable {
        public object Data {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
                DP.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}