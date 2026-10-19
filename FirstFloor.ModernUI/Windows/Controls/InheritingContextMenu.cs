using System;
using Avalonia.Controls;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Context menu which inherits data context of the placement target (Avalonia already does that for context menus of controls).
    /// </summary>
    public class InheritingContextMenu : ContextMenu {
        protected override Type StyleKeyOverride => typeof(ContextMenu);
    }
}
