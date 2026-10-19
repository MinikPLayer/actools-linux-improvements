using System.IO;
using System.Windows;
using Avalonia.Media.Imaging;
using FirstFloor.ModernUI.Helpers;

namespace FirstFloor.ModernUI.Windows.Media {
    public static class BrokenIcon {
        private static Bitmap _imageSource;

        public static Bitmap ImageSource => _imageSource ?? (_imageSource = Load());

        public static int Width => 14;

        public static int Height => 16;

        public static Size Size { get; } = new Size(14, 16);

        private static Bitmap Load() {
            using (var stream = new MemoryStream(BinaryResources.BrokenImage)) {
                return new Bitmap(stream);
            }
        }
    }
}
