using System;
using Avalonia;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Windows.Navigation;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// A lighweight control for displaying small amounts of rich formatted BbCode content.
    /// </summary>
    /// <remarks>
    /// Simplified version for now: supports basic formatting tags, no images and emoji, links are shown but not clickable (see LinuxPortDocs.md).
    /// </remarks>
    [Localizable(false)]
    public class BbCodeBlock : Avalonia.Controls.Decorator {
        public static ResourceDictionary IconsDictionary { get; set; }

        public enum AllowBbCodes {
            None,
            Limited,
            All
        }

        public static ICommand LinkCommand { get; } = new AsyncCommand<string>(async p => {
            var uri = new Uri(p, UriKind.RelativeOrAbsolute);
            if (DefaultLinkNavigator.Commands != null) {
                if (DefaultLinkNavigator.Commands.TryGetValue(uri, out var command)) {
                    await ExecuteCommandAsync(command, null);
                    return;
                }

                if (uri.IsAbsoluteUri) {
                    var original = uri.AbsoluteUri;
                    var index = original.IndexOf('?');
                    if (index != -1) {
                        var subUri = new Uri(original.Substring(0, index), UriKind.Absolute);
                        if (DefaultLinkNavigator.Commands.TryGetValue(subUri, out command)) {
                            await ExecuteCommandAsync(command, uri.GetQueryParam("param"));
                            return;
                        }
                    }
                }
            }

            DefaultLinkNavigator?.Navigate(uri, Application.Current?.MainWindow);

            Task ExecuteCommandAsync(ICommand command, string param) {
                if (!command.CanExecute(param)) return Task.Delay(0);

                if (command is AsyncCommand asyncCommand) {
                    return asyncCommand.ExecuteAsync();
                }

                if (command is AsyncCommand<string> asyncCommandT) {
                    return asyncCommandT.ExecuteAsync(param);
                }

                command.Execute(param);
                return Task.Delay(0);
            }
        });

        [CanBeNull]
        public static IEmojiProvider OptionEmojiProvider;

        [CanBeNull]
        public static string OptionEmojiCacheDirectory;

        [CanBeNull]
        public static string OptionImageCacheDirectory;

        [CanBeNull]
        public static Func<string, bool> OptionVerifyLocalImage;

        [CanBeNull]
        public static ICommand OptionFileNavigateCommand;

        public static string Encode(string value) {
            return value?.Replace("[", "\\[");
        }

        public static string Decode(string value) {
            return value?.Replace("\\[", "[");
        }

        public static string EncodeAttribute(string value) {
            return value == null ? null : "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        public static event EventHandler<BbCodeImageEventArgs> ImageClicked;

        internal static void OnImageClicked(BbCodeImageEventArgs args) {
            ImageClicked?.Invoke(null, args);
        }

        public static readonly ILinkNavigator DefaultLinkNavigator = new DefaultLinkNavigator();

        public static void AddLinkCommand(Uri key, ICommand value) {
            DefaultLinkNavigator.Commands.Remove(key);
            DefaultLinkNavigator.Commands.Add(key, value);
        }

        #region Properties
        public static readonly StyledProperty<string> TextProperty = DP.Register<string>(nameof(Text), typeof(BbCodeBlock),
                new PropertyMetadata(OnBbCodeChanged));

        [Avalonia.Metadata.Content]
        public string Text {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly StyledProperty<ILinkNavigator> LinkNavigatorProperty = DP.Register<ILinkNavigator>(nameof(LinkNavigator),
                typeof(BbCodeBlock), new PropertyMetadata(DefaultLinkNavigator, OnBbCodeChanged));

        [CanBeNull]
        public ILinkNavigator LinkNavigator {
            get => (ILinkNavigator)GetValue(LinkNavigatorProperty);
            set => SetValue(LinkNavigatorProperty, value);
        }

        public static readonly StyledProperty<EmojiSupport> ModeProperty = DP.Register<EmojiSupport>(nameof(Mode),
                typeof(BbCodeBlock), new PropertyMetadata(EmojiSupport.Extended, OnBbCodeChanged));

        public EmojiSupport Mode {
            get => (EmojiSupport)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        public static readonly StyledProperty<bool> HighlightUrlsProperty = DP.Register<bool>(nameof(HighlightUrls),
                typeof(BbCodeBlock), new PropertyMetadata(true, OnBbCodeChanged));

        public bool HighlightUrls {
            get => (bool)GetValue(HighlightUrlsProperty);
            set => SetValue(HighlightUrlsProperty, value);
        }

        public static readonly StyledProperty<bool> AllowImagesProperty = DP.Register<bool>(nameof(AllowImages),
                typeof(BbCodeBlock), new PropertyMetadata(true));

        public bool AllowImages {
            get => (bool)GetValue(AllowImagesProperty);
            set => SetValue(AllowImagesProperty, value);
        }

        public static readonly StyledProperty<string> PlaceholderProperty = DP.Register<string>(nameof(Placeholder),
                typeof(BbCodeBlock), new PropertyMetadata(OnBbCodeChanged));

        public string Placeholder {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly StyledProperty<AllowBbCodes> AllowedBbCodesProperty = DP.Register<AllowBbCodes>(nameof(AllowedBbCodes),
                typeof(BbCodeBlock), new PropertyMetadata(AllowBbCodes.All, OnBbCodeChanged));

        public AllowBbCodes AllowedBbCodes {
            get => (AllowBbCodes)GetValue(AllowedBbCodesProperty);
            set => SetValue(AllowedBbCodesProperty, value);
        }

        // text-related properties of the inner text block

        public static readonly StyledProperty<IBrush> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<BbCodeBlock>();
        public static readonly StyledProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<BbCodeBlock>();
        public static readonly StyledProperty<FontFamily> FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner<BbCodeBlock>();
        public static readonly StyledProperty<FontWeight> FontWeightProperty = TextElement.FontWeightProperty.AddOwner<BbCodeBlock>();
        public static readonly StyledProperty<FontStyle> FontStyleProperty = TextElement.FontStyleProperty.AddOwner<BbCodeBlock>();
        public static readonly StyledProperty<TextWrapping> TextWrappingProperty = TextBlock.TextWrappingProperty.AddOwner<BbCodeBlock>();
        public static readonly StyledProperty<TextAlignment> TextAlignmentProperty = TextBlock.TextAlignmentProperty.AddOwner<BbCodeBlock>();
        public static readonly StyledProperty<TextTrimming> TextTrimmingProperty = TextBlock.TextTrimmingProperty.AddOwner<BbCodeBlock>();
        public static readonly StyledProperty<double> LineHeightProperty = TextBlock.LineHeightProperty.AddOwner<BbCodeBlock>();

        public IBrush Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }
        public double FontSize { get => GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
        public FontFamily FontFamily { get => GetValue(FontFamilyProperty); set => SetValue(FontFamilyProperty, value); }
        public FontWeight FontWeight { get => GetValue(FontWeightProperty); set => SetValue(FontWeightProperty, value); }
        public FontStyle FontStyle { get => GetValue(FontStyleProperty); set => SetValue(FontStyleProperty, value); }
        public TextWrapping TextWrapping { get => GetValue(TextWrappingProperty); set => SetValue(TextWrappingProperty, value); }
        public TextAlignment TextAlignment { get => GetValue(TextAlignmentProperty); set => SetValue(TextAlignmentProperty, value); }
        public TextTrimming TextTrimming { get => GetValue(TextTrimmingProperty); set => SetValue(TextTrimmingProperty, value); }
        public double LineHeight { get => GetValue(LineHeightProperty); set => SetValue(LineHeightProperty, value); }
        #endregion

        protected readonly TextBlock TextBlock;

        public BbCodeBlock() {
            TextBlock = new TextBlock();
            TextBlock[!TextBlock.TextWrappingProperty] = this[!TextWrappingProperty];
            TextBlock[!TextBlock.TextAlignmentProperty] = this[!TextAlignmentProperty];
            TextBlock[!TextBlock.TextTrimmingProperty] = this[!TextTrimmingProperty];
            TextBlock[!TextBlock.LineHeightProperty] = this[!LineHeightProperty];
            Child = TextBlock;
        }

        /// <summary>Parses BB codes into a single inline (to be used in other text blocks).</summary>
        public static Inline ParseEmoji([CanBeNull] string text, AllowBbCodes allowBbCodes = AllowBbCodes.All, bool highlightUrls = false,
                [CanBeNull] object source = null, [CanBeNull] FirstFloor.ModernUI.Windows.Controls.BbCode.IBbCodeLinkParser linkParser = null, [CanBeNull] ILinkNavigator linkNavigator = null) {
            var span = new Span();
            if (!string.IsNullOrEmpty(text)) {
                SimpleBbCodeParser.Fill(span.Inlines, text);
            }

            return span;
        }

        private static void OnBbCodeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((BbCodeBlock)o).Update();
        }

        protected virtual void Update() {
            var text = Text;
            var inlines = TextBlock.Inlines;
            inlines.Clear();

            if (string.IsNullOrEmpty(text)) {
                var placeholder = Placeholder;
                if (!string.IsNullOrEmpty(placeholder)) {
                    inlines.Add(PlaceholderTextBlock.GetPlaceholder(TextBlock, placeholder));
                }

                return;
            }

            if (Mode == EmojiSupport.WithoutBbCodes) {
                inlines.Add(new Run { Text = text });
                return;
            }

            SimpleBbCodeParser.Fill(inlines, text);
        }
    }

    /// <summary>
    /// Basic BBCode support: [b], [i], [u], [s], [color], [size], [mono], [url], [url=…], [br], \[ escapes.
    /// </summary>
    internal static class SimpleBbCodeParser {
        private class Frame {
            public string Tag;
            public string Argument;
            public int Start;
        }

        public static void Fill(InlineCollection inlines, string text) {
            var stack = new List<Frame>();
            var plain = new StringBuilder();
            var runs = new List<(string, List<Frame>)>();

            void Flush() {
                if (plain.Length == 0) return;
                runs.Add((plain.ToString(), stack.Select(x => new Frame { Tag = x.Tag, Argument = x.Argument }).ToList()));
                plain.Clear();
            }

            for (var i = 0; i < text.Length; i++) {
                var c = text[i];
                if (c == '\\' && i + 1 < text.Length && text[i + 1] == '[') {
                    plain.Append('[');
                    i++;
                    continue;
                }

                if (c == '[') {
                    var end = text.IndexOf(']', i);
                    if (end > i) {
                        var body = text.Substring(i + 1, end - i - 1);
                        var closing = body.StartsWith("/");
                        if (closing) body = body.Substring(1);
                        var eq = body.IndexOf('=');
                        var name = (eq == -1 ? body : body.Substring(0, eq)).Trim().ToLowerInvariant();
                        var arg = eq == -1 ? null : body.Substring(eq + 1).Trim().Trim('"');

                        switch (name) {
                            case "b":
                            case "i":
                            case "u":
                            case "s":
                            case "color":
                            case "size":
                            case "mono":
                            case "url":
                            case "bg":
                            case "code":
                                Flush();
                                if (closing) {
                                    var index = stack.FindLastIndex(x => x.Tag == name);
                                    if (index != -1) stack.RemoveRange(index, stack.Count - index);
                                } else {
                                    stack.Add(new Frame { Tag = name, Argument = arg });
                                }
                                i = end;
                                continue;
                            case "br":
                                plain.Append('\n');
                                i = end;
                                continue;
                        }
                    }
                }

                plain.Append(c);
            }

            Flush();

            foreach (var (value, frames) in runs) {
                var run = new Run { Text = value };
                foreach (var frame in frames) {
                    switch (frame.Tag) {
                        case "b":
                            run.FontWeight = FontWeight.Bold;
                            break;
                        case "i":
                            run.FontStyle = FontStyle.Italic;
                            break;
                        case "u":
                            run.TextDecorations = TextDecorations.Underline;
                            break;
                        case "s":
                            run.TextDecorations = TextDecorations.Strikethrough;
                            break;
                        case "mono":
                        case "code":
                            run.FontFamily = new FontFamily("Consolas, Menlo, DejaVu Sans Mono, monospace");
                            break;
                        case "size":
                            if (double.TryParse(frame.Argument, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var size)) {
                                run.FontSize = size;
                            }
                            break;
                        case "color":
                            if (frame.Argument != null && Color.TryParse(frame.Argument, out var color)) {
                                run.Foreground = new SolidColorBrush(color);
                            }
                            break;
                        case "url":
                            run.TextDecorations = TextDecorations.Underline;
                            break;
                    }
                }
                inlines.Add(run);
            }
        }
    }
}
