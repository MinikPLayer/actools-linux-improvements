using Avalonia.Controls.Primitives;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// WPF’s Thumb lost its dragging state if mouse capture was taken away, here it was a workaround for that.
    /// Avalonia’s thumb handles it properly, so it’s just a plain thumb now (kept for XAML compatibility).
    /// </summary>
    public class ThumbExt : Thumb { }
}
