using System.Windows;
using AcManager.Tools.Miscellaneous;
using FirstFloor.ModernUI;

namespace AcManager.Controls.QuickSwitches {
    public class ShadersPatchPresetsControl : QuickSwitchPresetsControl {
        protected override System.Type StyleKeyOverride => typeof(ShadersPatchPresetsControl);

        static ShadersPatchPresetsControl() {
        }

        private PatchSettingsModel _model;

        public ShadersPatchPresetsControl() {
            Loaded += OnLoaded;
            this.OnActualUnload(() => _model?.Dispose());
        }

        private void OnLoaded(object sender, RoutedEventArgs args) {
            if (_model != null) return;
            _model = PatchSettingsModel.Create();
            UserPresetable = _model;
        }
    }
}