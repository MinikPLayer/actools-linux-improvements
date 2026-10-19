using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Selectable text which trims the middle of a path to fit the width. The text is shown by a template (see Themes/PathTrimmingTextBlock.axaml).
    /// </summary>
    public class PathTrimmingTextBlock : TemplatedControl {
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<PathTrimmingTextBlock, string>(nameof(Text));
        public static readonly StyledProperty<string> PlaceholderProperty = AvaloniaProperty.Register<PathTrimmingTextBlock, string>(nameof(Placeholder));
        public static readonly StyledProperty<string> DisplayTextProperty = AvaloniaProperty.Register<PathTrimmingTextBlock, string>(nameof(DisplayText));
        public static readonly StyledProperty<bool> IsPlaceholderProperty = AvaloniaProperty.Register<PathTrimmingTextBlock, bool>(nameof(IsPlaceholder));

        public string Text { get => GetValue(TextProperty); set => SetValue(TextProperty, value); }
        public string Placeholder { get => GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
        public string DisplayText { get => GetValue(DisplayTextProperty); private set => SetValue(DisplayTextProperty, value); }
        public bool IsPlaceholder { get => GetValue(IsPlaceholderProperty); private set => SetValue(IsPlaceholderProperty, value); }

        static PathTrimmingTextBlock() {
            TextProperty.Changed.AddClassHandler<PathTrimmingTextBlock>((o, e) => o.Update());
            PlaceholderProperty.Changed.AddClassHandler<PathTrimmingTextBlock>((o, e) => o.Update());
            BoundsProperty.Changed.AddClassHandler<PathTrimmingTextBlock>((o, e) => o.Update());
            FontSizeProperty.Changed.AddClassHandler<PathTrimmingTextBlock>((o, e) => o.Update());
        }

        private void Update() {
            var text = Text;
            if (string.IsNullOrEmpty(text)) {
                DisplayText = Placeholder;
                IsPlaceholder = true;
                ToolTip.SetTip(this, null);
            } else {
                var trimmed = GetTrimmedPath(Bounds.Width);
                DisplayText = trimmed;
                IsPlaceholder = false;
                ToolTip.SetTip(this, trimmed == text ? null : text);
            }
        }

        private static List<string> Split(string s, out bool url) {
            url = false;

            var p = 0;
            var r = new List<string>(10);
            for (var i = 0; i < s.Length; i++) {
                var c = s[i];
                if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar) {
                    var l = i - p;
                    if (l > 0) {
                        var piece = s.Substring(p, i - p);
                        if (p == 0 && (piece == "http:" || piece == "https:") && i < s.Length - 1 && s[i + 1] == '/') {
                            r.Add(piece + '/');
                            url = true;
                            ++i;
                        } else {
                            r.Add(piece);
                        }
                    }

                    p = i + 1;
                }
            }

            if (p < s.Length - 1) {
                r.Add(s.Substring(p));
            }

            return r;
        }

        private string GetTrimmedPath(double width) {
            if (Text == null) return "";

            width -= 20d;

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            var fontSize = FontSize;
            var foreground = Foreground;

            bool TestWidth(string text) {
                return new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
                        typeface, fontSize, foreground).Width < width;
            }

            if (TestWidth(Text)) return Text;

            var pieces = Split(Text, out bool url);
            if (pieces.Count == 0) return "";

            var c = url ? '/' : Path.DirectorySeparatorChar;
            if (pieces.Count == 1) return Text;

            if (url) {
                var last = pieces[pieces.Count - 1];
                for (var i = 1; i < pieces.Count - 1; i++) {
                    var candidate = string.Join(c.ToString(),
                            pieces.Take(pieces.Count - i - 1).Concat(new[]{ "…", last }));
                    if (TestWidth(candidate)) return candidate;
                }
                return $@"…{c}{last}";
            }

            for (var i = 1; i < pieces.Count - 1; i++) {
                var candidate = string.Join(c.ToString(),
                        new[] { pieces[0], "…" }.Concat(pieces.Skip(i + 1)));
                if (TestWidth(candidate)) return candidate;
            }

            return $@"…{c}{pieces[pieces.Count - 1]}";
        }
    }
}