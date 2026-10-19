using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using AcManager.Tools.AcObjectsNew;
using AcManager.Tools.Helpers;
using AcManager.Tools.Miscellaneous;
using FirstFloor.ModernUI.Commands;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;

namespace AcManager.Controls.Dialogs {
    public partial class CupInformationDialog : global::FirstFloor.ModernUI.Windows.Controls.ModernDialog {
        public class ViewModel : NotifyPropertyChanged {
            public ICupSupportedObject CupObject { get; }
            public string DisplayAlternativeIds { get; }

            public ViewModel(ICupSupportedObject cupObject) {
                CupObject = cupObject;

                var manager = (cupObject as AcObjectNew)?.GetManager();
                DisplayAlternativeIds = cupObject.CupUpdateInformation?.AlternativeIds?.Select(x => manager?.GetObjectById(x ?? "")?.DisplayName ?? x)
                                                 .JoinToReadableString();
            }
        }

        public ViewModel Model => (ViewModel) DataContext;

        public CupInformationDialog(ICupSupportedObject obj) {
            InitializeComponent();
            DataContext = new ViewModel(obj);

            var client = CupClient.Instance;
            if (client != null) {
                Buttons = new[] {
                    CreateCloseDialogButton("Update", true, false, MessageBoxResult.OK, new AsyncCommand(() =>
                            client.InstallUpdateAsync(obj.CupContentType, obj.Id))),
                    CreateCloseDialogButton("Report update as broken", false, true, MessageBoxResult.No,
                            new AsyncCommand(() => client.ReportUpdateAsync(obj.CupContentType, obj.Id))),
                    CloseButton
                };
            }
        }

        private void OnClosing(object sender, Avalonia.Controls.WindowClosingEventArgs e) {}

        private bool _click;

        private void OnHyperlinkMouseDown(object sender, PointerPressedEventArgs e) {
            _click = true;
        }

        private void OnHyperlinkMouseUp(object sender, PointerReleasedEventArgs e) {
            if (_click) {
                WindowsHelper.ViewInBrowser(Model.CupObject.CupUpdateInformation?.InformationUrl);
            }
        }

        private void OnHyperlinkMouseSelection(object sender, SelectionChangedEventArgs e) {
            _click = false;
        }
    }
}
