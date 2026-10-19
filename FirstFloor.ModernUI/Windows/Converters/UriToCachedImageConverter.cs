using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Serialization;

namespace FirstFloor.ModernUI.Windows.Converters {
    /// <summary>
    /// Loads images by URIs (files and application resources). Web images aren’t supported yet.
    /// </summary>
    [ValueConversion(typeof(object), typeof(IImage))]
    public class UriToCachedImageConverter : IValueConverter {
        public const double OneTrueDpi = 96d;

        public static IImage Convert(object value, bool considerOneTrueDpi = false, int decodeWidth = -1, int decodeHeight = -1) {
            if (value is IImage image) return image;

            var source = value as Uri;
            if (source == null) {
                var path = value?.ToString();
                if (string.IsNullOrEmpty(path)) {
                    return null;
                }

                try {
                    source = new Uri(path, UriKind.RelativeOrAbsolute);
                } catch (Exception) {
                    Logging.Warning("Invalid URI format: " + path);
                    return null;
                }
            }

            try {
                using (var stream = OpenStream(source)) {
                    if (stream == null) return null;
                    if (decodeWidth > 0) {
                        return Bitmap.DecodeToWidth(stream, decodeWidth);
                    }

                    if (decodeHeight > 0) {
                        return Bitmap.DecodeToHeight(stream, decodeHeight);
                    }

                    return new Bitmap(stream);
                }
            } catch (Exception) {
                return null;
            }
        }

        private static Stream OpenStream(Uri source) {
            if (!source.IsAbsoluteUri) {
                var local = source.OriginalString;
                return File.Exists(local) ? File.OpenRead(local) : null;
            }

            switch (source.Scheme) {
                case "file":
                    return File.Exists(source.LocalPath) ? File.OpenRead(source.LocalPath) : null;
                case "avares":
                    return AssetLoader.Open(source);
                case "pack":
                    return AssetLoader.Open(ComponentLoader.ToAvaloniaUri(source));
                default:
                    return null;
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (parameter is string p) {
                var i = p.IndexOf('×');
                return i != -1
                        ? Convert(value, false, i == 0 ? -1 : p.Substring(0, i).As<int>(), i == p.Length - 1 ? -1 : p.Substring(i + 1).As<int>())
                        : Convert(value, p == "oneTrueDpi");
            }

            return Convert(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}
