using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Avalonia.Data.Converters;

namespace FirstFloor.ModernUI.Windows.Converters {
    /// <summary>
    /// In WPF, converters produce Visibility. Avalonia has IsVisible instead, so converters check the target type
    /// and return bool if they are used for a boolean property.
    /// </summary>
    public static class VisibilityResult {
        public static object Adapt(object result, Type targetType) {
            if (result is Visibility v && targetType != typeof(Visibility) && targetType != typeof(Visibility?)) {
                return v == Visibility.Visible;
            }

            return result;
        }
    }

    /// <summary>
    /// Converts Visibility to bool (for properties of type Visibility bound to IsVisible).
    /// </summary>
    public class VisibilityToBoolConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            switch (value) {
                case Visibility v:
                    return v == Visibility.Visible;
                case bool b:
                    return b;
                case null:
                    return false;
                default:
                    return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public static class VisibilityConverters {
        public static VisibilityToBoolConverter ToBool { get; } = new VisibilityToBoolConverter();
    }

    /// <summary>
    /// Used to convert DataTriggers: value of binding is compared to the expected value (parameter), the result is used
    /// by style selectors (see T class).
    /// </summary>
    public class StateConverter : IValueConverter {
        public static StateConverter Instance { get; } = new StateConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (parameter == null) return value == null;
            if (value == null) return false;
            if (value.Equals(parameter)) return true;

            var expected = parameter.ToString();
            switch (value) {
                case bool b:
                    return bool.TryParse(expected, out var eb) && eb == b;
                case string s:
                    return s == expected;
                case Enum e:
                    return e.ToString() == expected;
                case IConvertible c when double.TryParse(expected, NumberStyles.Any, CultureInfo.InvariantCulture, out var d):
                    try {
                        return System.Convert.ToDouble(c, CultureInfo.InvariantCulture) == d;
                    } catch {
                        return false;
                    }
                default:
                    return value.ToString() == expected;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
