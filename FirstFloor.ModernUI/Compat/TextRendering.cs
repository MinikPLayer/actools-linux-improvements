using Avalonia;

namespace System.Windows.Media {
    public enum BitmapScalingMode {
        Unspecified = 0,
        LowQuality = 1,
        HighQuality = 2,
        NearestNeighbor = 3,
        Linear = 1,
        Fant = 2
    }

    public enum EdgeMode {
        Unspecified = 0,
        Aliased = 1
    }

    public enum ClearTypeHint {
        Auto = 0,
        Enabled = 1
    }

    public enum TextFormattingMode {
        Ideal = 0,
        Display = 1
    }

    public enum TextRenderingMode {
        Auto = 0,
        Aliased = 1,
        Grayscale = 2,
        ClearType = 3
    }

    public enum TextHintingMode {
        Auto = 0,
        Fixed = 1,
        Animated = 2
    }

    /// <summary>
    /// WPF’s attached text rendering options. Avalonia (Skia) has its own text rendering, so these values are only stored.
    /// </summary>
    public static class TextOptions {
        public static readonly AttachedProperty<TextFormattingMode> TextFormattingModeProperty = DP.RegisterAttached<TextFormattingMode>("TextFormattingMode",
                typeof(TextOptions), new PropertyMetadata(TextFormattingMode.Ideal));

        public static readonly AttachedProperty<TextRenderingMode> TextRenderingModeProperty = DP.RegisterAttached<TextRenderingMode>("TextRenderingMode",
                typeof(TextOptions), new PropertyMetadata(TextRenderingMode.Auto));

        public static readonly AttachedProperty<TextHintingMode> TextHintingModeProperty = DP.RegisterAttached<TextHintingMode>("TextHintingMode",
                typeof(TextOptions), new PropertyMetadata(TextHintingMode.Auto));

        public static void SetTextFormattingMode(AvaloniaObject o, TextFormattingMode value) => o.SetValue(TextFormattingModeProperty, value);
        public static TextFormattingMode GetTextFormattingMode(AvaloniaObject o) => o.GetValue(TextFormattingModeProperty);
        public static void SetTextRenderingMode(AvaloniaObject o, TextRenderingMode value) => o.SetValue(TextRenderingModeProperty, value);
        public static TextRenderingMode GetTextRenderingMode(AvaloniaObject o) => o.GetValue(TextRenderingModeProperty);
        public static void SetTextHintingMode(AvaloniaObject o, TextHintingMode value) => o.SetValue(TextHintingModeProperty, value);
        public static TextHintingMode GetTextHintingMode(AvaloniaObject o) => o.GetValue(TextHintingModeProperty);
    }

    public static class RenderOptions {
        private static Avalonia.Media.Imaging.BitmapInterpolationMode Convert(BitmapScalingMode mode) {
            switch (mode) {
                case BitmapScalingMode.NearestNeighbor: return Avalonia.Media.Imaging.BitmapInterpolationMode.None;
                case BitmapScalingMode.LowQuality: return Avalonia.Media.Imaging.BitmapInterpolationMode.LowQuality;
                case BitmapScalingMode.HighQuality: return Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality;
                default: return Avalonia.Media.Imaging.BitmapInterpolationMode.Unspecified;
            }
        }

        public static void SetBitmapScalingMode(Visual o, BitmapScalingMode mode) {
            Avalonia.Media.RenderOptions.SetBitmapInterpolationMode(o, Convert(mode));
        }

        public static void SetEdgeMode(Visual o, EdgeMode mode) {
            Avalonia.Media.RenderOptions.SetEdgeMode(o, mode == EdgeMode.Aliased ? Avalonia.Media.EdgeMode.Aliased : Avalonia.Media.EdgeMode.Unspecified);
        }

        public static void SetClearTypeHint(Visual o, ClearTypeHint hint) { }
    }

    public static class FontStretches {
        public static Avalonia.Media.FontStretch Normal => Avalonia.Media.FontStretch.Normal;
        public static Avalonia.Media.FontStretch Condensed => Avalonia.Media.FontStretch.Condensed;
    }
}
