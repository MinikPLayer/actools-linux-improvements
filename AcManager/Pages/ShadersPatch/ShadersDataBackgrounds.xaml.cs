using System.Windows.Controls;
using System.Windows.Input;
using AcManager.Controls.Dialogs;
using AcManager.Tools.Data;

namespace AcManager.Pages.ShadersPatch {
    public partial class ShadersDataBackgrounds : UserControl {
        public ShadersDataBackgrounds() {
            InitializeComponent();
        }

        private bool _isDoubleClick;

        private void OnGridDoubleClick(object sender, PointerPressedEventArgs e) {
            _isDoubleClick = e.ClickCount == 2;
        }

        private void OnGridMouseUp(object sender, PointerReleasedEventArgs e) {
            if (_isDoubleClick && Grid.SelectedItem is PatchBackgroundDataEntry entry) {
                e.Handled = true;
                new ImageViewer(entry.GetApiUrl()) {
                    ConsiderPreferredFullscreen = true
                }.ShowDialog();
            }
        }
    }
}
