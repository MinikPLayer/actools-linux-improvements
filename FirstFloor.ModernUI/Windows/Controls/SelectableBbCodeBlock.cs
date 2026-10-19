using System.ComponentModel;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// BbCode block with selectable text.
    /// </summary>
    [Localizable(false)]
    public class SelectableBbCodeBlock : BbCodeBlock {
        public SelectableBbCodeBlock() {
            Focusable = true;
        }
    }
}
