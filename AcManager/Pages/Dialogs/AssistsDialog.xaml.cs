using AcManager.Controls.ViewModels;

namespace AcManager.Pages.Dialogs {
    public partial class AssistsDialog : global::FirstFloor.ModernUI.Windows.Controls.ModernDialog {
        public AssistsDialog(AssistsViewModel viewModel) {
            DataContext = viewModel;
            InitializeComponent();
            Buttons = new[] { CloseButton };
        }
    }
}
