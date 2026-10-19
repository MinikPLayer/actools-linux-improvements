using System;
using System.IO;
using AcTools.Utils;
using ImageMagick;
using JetBrains.Annotations;

namespace AcTools.Render.Utils {
    /// <summary>
    /// Encodes images as DDS. NVIDIA Texture Tools (mixed-mode C++/CLI library, can’t be used with .NET Core)
    /// are no longer used here, Magick.NET does all the work.
    /// </summary>
    public static class DdsEncoder {
        private static string GetCompression(PreferredDdsFormat format, int width, int height) {
            switch (format) {
                case PreferredDdsFormat.Auto:
                    return width <= 512 && height <= 512 ? "none" : "dxt1";
                case PreferredDdsFormat.AutoTransparency:
                    return width <= 512 && height <= 512 ? "none" : "dxt5";
                case PreferredDdsFormat.DXT1:
                    return "dxt1";
                case PreferredDdsFormat.DXT5:
                    return "dxt5";
                case PreferredDdsFormat.NoCompression:
                case PreferredDdsFormat.NoCompressionTransparency:
                case PreferredDdsFormat.Luminance:
                case PreferredDdsFormat.LuminanceTransparency:
                case PreferredDdsFormat.RGB565:
                case PreferredDdsFormat.RGBA4444:
                    return "none";
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        private static int GetMipmapsCount(int width, int height) {
            var result = 0;
            for (var s = Math.Max(width, height); s > 1; s >>= 1) result++;
            return result;
        }

        public static void SaveAsDds([NotNull] Stream stream, [NotNull] byte[] imageData, PreferredDdsFormat format, [CanBeNull] IProgress<double> progress) {
            try {
                using (var input = new MemoryStream(imageData))
                using (var image = new MagickImage(input)) {
                    var compression = GetCompression(format, (int)image.Width, (int)image.Height);
                    AcToolsLogging.Write($"Preferred format: {format}, compression: {compression}");

                    image.Settings.SetDefine(MagickFormat.Dds, "compression", compression);
                    image.Settings.SetDefine(MagickFormat.Dds, "mipmaps", GetMipmapsCount((int)image.Width, (int)image.Height).ToString());
                    if (compression != "none") {
                        image.Settings.SetDefine(MagickFormat.Dds, "cluster-fit", "true");
                    }

                    image.Write(stream, MagickFormat.Dds);
                }

                progress?.Report(1d);
            } catch (Exception e) {
                AcToolsLogging.NonFatalErrorNotify("Can’t encode DDS texture using Magick.NET", null, e);
            }
        }

        public static void SaveAsDds([NotNull] string filename, [NotNull] byte[] imageData, PreferredDdsFormat format, [CanBeNull] IProgress<double> progress) {
            using (var temporary = FileUtils.RecycleOriginal(filename)) {
                try {
                    using (var stream = File.Create(temporary.Filename)) {
                        SaveAsDds(stream, imageData, format, progress);
                    }
                } catch {
                    FileUtils.TryToDelete(temporary.Filename);
                    throw;
                }
            }
        }
    }
}
