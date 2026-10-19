using System.Windows.Input;

namespace AcManager.Pages.ServerPreset {
    public partial class ServerPresetCombined : global::Avalonia.Controls.UserControl {
        public ServerPresetCombined() {
            InitializeComponent();
            this.AddPreviewMouseWheel(OnMouseWheel);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e) {
            ScrollViewer.ScrollToVerticalOffset(ScrollViewer.VerticalOffset - e.Delta.Y * 120d);
            e.Handled = true;
        }
    }
}
