using System;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using ImageMagick;
using JetBrains.Annotations;

namespace AcManager.Tools.Helpers {
    /// <summary>
    /// Texture effects which used to be WPF pixel shaders (GrayscaleEffect, InvertEffect, OverlayColorEffect, InvertKeepColorEffect
    /// and AsTransparencyMask), now applied directly to pixels. Pixel is a position in an RGBA byte array.
    /// </summary>
    public static class TextureEffects {
        [CanBeNull]
        public static Action<byte[], int> Parse([CanBeNull] string textureEffect) {
            if (textureEffect == null) return null;

            var s = textureEffect.Split(':');
            switch (s[0].ToLowerInvariant()) {
                case "grayscale":
                    return (p, i) => {
                        var gray = (byte)(p[i] * 0.3 + p[i + 1] * 0.59 + p[i + 2] * 0.11);
                        p[i] = p[i + 1] = p[i + 2] = gray;
                    };
                case "invert":
                    return (p, i) => {
                        p[i] = (byte)(255 - p[i]);
                        p[i + 1] = (byte)(255 - p[i + 1]);
                        p[i + 2] = (byte)(255 - p[i + 2]);
                    };
                case "color":
                case "colour":
                    var overlay = s.Length > 1 ? s[1].ToColor() ?? Colors.White : Colors.White;
                    var k = overlay.A / 255d;
                    return (p, i) => {
                        p[i] = (byte)(p[i] + (overlay.R - p[i]) * k);
                        p[i + 1] = (byte)(p[i + 1] + (overlay.G - p[i + 1]) * k);
                        p[i + 2] = (byte)(p[i + 2] + (overlay.B - p[i + 2]) * k);
                    };
                case "invertkeepcolor":
                case "invertkeepcolors":
                case "invertkeepcolour":
                case "invertkeepcolours":
                    return (p, i) => {
                        double r = p[i] / 255d, g = p[i + 1] / 255d, b = p[i + 2] / 255d;
                        var x = Math.Max(r, Math.Max(g, b));
                        var m = Math.Min(r, Math.Min(g, b));
                        if (x == 0d) {
                            p[i] = p[i + 1] = p[i + 2] = p[i + 3];
                        } else {
                            var f = 1 + (1 / x - 2) * m / x;
                            p[i] = (byte)Math.Min(255d, r * f * 255d);
                            p[i + 1] = (byte)Math.Min(255d, g * f * 255d);
                            p[i + 2] = (byte)Math.Min(255d, b * f * 255d);
                        }
                    };
                default:
                    return null;
            }
        }

        /// <summary>
        /// Takes red channel as an alpha mask and fills it with a color (an old AsTransparencyMask effect).
        /// </summary>
        [NotNull]
        public static Action<byte[], int> AsTransparencyMask(Avalonia.Media.Color color) {
            return (p, i) => {
                var mask = p[i] / 255d;
                p[i] = color.R;
                p[i + 1] = color.G;
                p[i + 2] = color.B;
                p[i + 3] = (byte)(color.A * mask);
            };
        }

        [NotNull]
        public static byte[] Apply([NotNull] MagickImage image, [NotNull] Action<byte[], int> effect) {
            var width = image.Width;
            var height = image.Height;
            var pixels = image.GetPixels().ToByteArray(PixelMapping.RGBA);
            for (var i = 0; i < pixels.Length; i += 4) {
                effect(pixels, i);
            }

            using (var result = new MagickImage(MagickColors.Transparent, width, height)) {
                result.GetPixels().SetPixels(pixels);
                result.Format = MagickFormat.Png;
                return result.ToByteArray();
            }
        }

        [NotNull]
        public static byte[] Apply([NotNull] byte[] data, [NotNull] Action<byte[], int> effect) {
            using (var image = new MagickImage(data)) {
                return Apply(image, effect);
            }
        }

        [NotNull]
        public static byte[] ApplyToFile([NotNull] string filename, [NotNull] Action<byte[], int> effect) {
            using (var image = new MagickImage(filename)) {
                return Apply(image, effect);
            }
        }
    }
}
