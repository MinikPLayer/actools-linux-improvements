using System;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>
    /// Used in WPF to prevent transparent windows, which are slow; not needed in Avalonia.
    /// </summary>
    public static class DisableTransparencyHelper {
        public static void Disable() { }
    }
}
