using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using FirstFloor.ModernUI.Helpers;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// A read-only text box which looks like a text block, text is copied to clipboard by click.
    /// </summary>
    public class SelectableTextBlock : BetterTextBox {
        public SelectableTextBlock() {
            IsReadOnly = true;
        }

        protected override System.Type StyleKeyOverride => typeof(SelectableTextBlock);

        private bool _tooltipSet;

        protected override void OnPointerEntered(PointerEventArgs e) {
            if (!_tooltipSet) {
                _tooltipSet = true;
                if (this.ToolTip == null) {
                    var toolTipText = new TextBlock();
                    toolTipText.SetBinding(TextBlock.TextProperty, new Avalonia.Data.Binding(nameof(Text)) {
                        Source = this,
                        StringFormat = "{0} (click to copy)"
                    });
                    this.ToolTip = toolTipText;
                }
            }
            base.OnPointerEntered(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e) {
            if (e.IsButton(MouseButton.Left) && e.ClickCount == 1 && SelectionEnd == SelectionStart) {
                ClipboardHelper.SetText(Text);
            }

            base.OnPointerReleased(e);
        }
    }
}
