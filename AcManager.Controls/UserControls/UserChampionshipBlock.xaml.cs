using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using AcManager.Controls.Dialogs;
using AcManager.Tools.Objects;

namespace AcManager.Controls.UserControls {
    public partial class UserChampionshipBlock : global::Avalonia.Controls.UserControl {
        public UserChampionshipBlock() {
            InitializeComponent();
        }

        public object ButtonPlaceholder {
            get { return ButtonPresenter.Content; }
            set { ButtonPresenter.Content = value; }
        }

        public UserChampionshipObject KunosCareerObject => DataContext as UserChampionshipObject;

        private void Information_OnMouseLeftButtonDown(object sender, PointerPressedEventArgs e) {
            var career = KunosCareerObject;
            if (career == null) return;
            
            new UserChampionshipIntro(career).ShowDialog();
        }

        private void OnDataContextChanged(object sender, System.EventArgs e) {
            var career = KunosCareerObject;
            if (career == null) return;

            InformationIcon.Data = this.TryFindResource(@"InformationIconData") as Geometry;
        }
    }
}
