using System.Windows;
using System.Windows.Controls;
using FirstFloor.ModernUI.Presentation;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Chooses a container theme by item type. Avalonia can’t select themes per item, so ItemContainerTheme is selected
    /// by the owner (see ModernMenu) using SelectTheme().
    /// </summary>
    public class ModernSubMenuItemContainerStyleSelector {
        public Avalonia.Styling.ControlTheme SelectTheme(object item) => item is LinkInputEmpty ?
                LinkInputEmptyStyle : item is LinkInput ? LinkInputStyle : item is Link ? LinkStyle : null;

        public Avalonia.Styling.ControlTheme LinkInputStyle { get; set; }

        public Avalonia.Styling.ControlTheme LinkInputEmptyStyle { get; set; }

        public Avalonia.Styling.ControlTheme LinkStyle { get; set; }
    }
}
