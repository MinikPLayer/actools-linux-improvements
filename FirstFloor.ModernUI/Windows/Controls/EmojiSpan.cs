using System.Windows;
using System.Windows.Documents;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// For copying.
    /// </summary>
    public class EmojiSpan : Span {
        public EmojiSpan() { }

        public EmojiSpan(string alt) {
            BaselineAlignment = BaselineAlignment.Center;
            Text = alt;
        }

        public string Text {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly StyledProperty<string> TextProperty = DP.Register<string>("Text", typeof(EmojiSpan), new PropertyMetadata("☺"));
    }
}