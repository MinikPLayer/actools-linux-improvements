namespace AcManager.Pages.Selected {
    public partial class PopupAuthor : global::Avalonia.Controls.UserControl {
        public PopupAuthor(ISelectedAcObjectViewModel model) {
            InitializeComponent();
            MainGrid.DataContext = model;
        }
    }
}
