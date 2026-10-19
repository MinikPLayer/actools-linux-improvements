using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data;

namespace FirstFloor.ModernUI.Windows.Converters {
    /// <summary>Replacement for WPF’s PriorityBinding: the first value which is set.</summary>
    public class FirstNotNullConverter : Avalonia.Data.Converters.IMultiValueConverter {
        public static readonly FirstNotNullConverter Instance = new FirstNotNullConverter();

        public object Convert(IList<object> values, System.Type targetType, object parameter, CultureInfo culture) {
            foreach (var v in values) {
                if (v != null && !(v is UnsetValueType) && !(v is BindingNotification) && !(v is string s && s.Length == 0)) return v;
            }
            return null;
        }
    }
}
