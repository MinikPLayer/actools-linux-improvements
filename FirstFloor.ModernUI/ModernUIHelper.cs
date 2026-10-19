using System;

namespace FirstFloor.ModernUI {
    /// <summary>
    /// Provides various common helper methods.
    /// </summary>
    public static class ModernUiHelper {
        /// <summary>
        /// Determines whether the current code is executed in a design time environment such as Visual Studio or Blend.
        /// </summary>
        public static bool IsInDesignMode => Avalonia.Controls.Design.IsDesignMode;

        /// <summary>
        /// Gets the DPI awereness of the current process. Avalonia is always per-monitor DPI aware where platform supports that.
        /// </summary>
        public static ProcessDpiAwareness GetDpiAwereness() {
            return ProcessDpiAwareness.PerMonitorDpiAware;
        }

        /// <summary>
        /// Nothing to do here: Avalonia takes care of DPI awareness.
        /// </summary>
        public static bool TrySetPerMonitorDpiAware() {
            return true;
        }
    }
}
