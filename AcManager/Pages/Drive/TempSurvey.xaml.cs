using System.Windows;
using FirstFloor.ModernUI.Helpers;

namespace AcManager.Pages.Drive {
    public partial class TempSurvey : global::Avalonia.Controls.UserControl {
        public TempSurvey() {
            InitializeComponent();
        }

        private void OnButtonClick(object sender, RoutedEventArgs e) {
            Stored.Get<bool>("surveyHide").Value = !Stored.Get<bool>("surveyHide").Value;
            /*if (Application.Current.MainWindow is MainWindow window) {
                window.ShortSurveyLink.IsShown = false;
            }*/
        }
    }
}