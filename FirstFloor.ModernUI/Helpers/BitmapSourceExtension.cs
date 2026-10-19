using System;
using System.Drawing.Imaging;
using System.IO;
using Avalonia.Media.Imaging;
using ImageMagick;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Helpers {
    public static class BitmapSourceExtension {
        public static Bitmap Resize(this Bitmap bitmap, int width, int height, BitmapInterpolationMode mode = BitmapInterpolationMode.HighQuality) {
            var target = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
            using (var context = target.CreateDrawingContext()) {
                using (context.PushRenderOptions(new Avalonia.Media.RenderOptions { BitmapInterpolationMode = mode })) {
                    context.DrawImage(bitmap, new Rect(0, 0, bitmap.Size.Width, bitmap.Size.Height), new Rect(0, 0, width, height));
                }
            }

            return target;
        }

        [NotNull]
        public static ImageFormat GetImageFormat([CanBeNull] this string filename) {
            switch (Path.GetExtension(filename)?.ToLowerInvariant()) {
                case ".png":
                    return ImageFormat.Png;
                case ".bmp":
                    return ImageFormat.Bmp;
                case ".gif":
                    return ImageFormat.Gif;
                case ".jpg":
                case ".jpeg":
                    return ImageFormat.Jpeg;
                case ".tif":
                case ".tiff":
                    return ImageFormat.Tiff;
                default:
                    return ImageFormat.Png;
            }
        }

        private static MagickFormat GetMagickFormat(ImageFormat format) {
            if (Equals(format, ImageFormat.Jpeg)) return MagickFormat.Jpeg;
            if (Equals(format, ImageFormat.Bmp)) return MagickFormat.Bmp;
            if (Equals(format, ImageFormat.Tiff)) return MagickFormat.Tiff;
            if (Equals(format, ImageFormat.Gif)) return MagickFormat.Gif;
            return MagickFormat.Png;
        }

        public static void SaveTo([NotNull] this Bitmap bitmap, [NotNull] string filename, [CanBeNull] ImageFormat format = null) {
            File.WriteAllBytes(filename, bitmap.ToBytes(format ?? filename.GetImageFormat()));
        }

        [NotNull]
        public static byte[] ToBytes([NotNull] this Bitmap bitmap, [NotNull] ImageFormat format) {
            using (var s = new MemoryStream()) {
                bitmap.Save(s);
                var png = s.ToArray();
                if (Equals(format, ImageFormat.Png)) return png;

                // Avalonia saves only PNG, other formats are converted
                using (var image = new MagickImage(png)) {
                    image.Format = GetMagickFormat(format);
                    return image.ToByteArray();
                }
            }
        }
    }
}
