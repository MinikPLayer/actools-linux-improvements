using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AcManager.Tools;
using AcManager.Tools.AcObjectsNew;
using FirstFloor.ModernUI.Dialogs;

namespace AcManager.Controls {
    public class NotesBlock : Control {
        protected override System.Type StyleKeyOverride => typeof(NotesBlock);

        static NotesBlock() {
        }

        public static readonly StyledProperty<AcObjectNew> AcObjectProperty = DP.Register<AcObjectNew>(nameof(AcObject),
                typeof(NotesBlock));

        public AcObjectNew AcObject {
            get => (AcObjectNew)GetValue(AcObjectProperty);
            set => SetValue(AcObjectProperty, value);
        }

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            base.OnApplyTemplate(e);
            if (this.GetTemplateChild(@"PART_EditButton") is Button button) {
                button.Click += OnEditClick;
            }
        }

        private void Edit() {
            var obj = AcObject;
            if (obj != null) {
                obj.Notes = Prompt.Show(null, $"Notes for {obj.DisplayName}", obj.Notes, ToolsStrings.Common_None, multiline: true,
                        comment: "Notes are for personal, saved on this computer information. You can sort by notes as well, with “notes:word” query.")
                        ?? obj.Notes;
            }
        }

        private void OnEditClick(object sender, RoutedEventArgs args) {
            args.Handled = true;
            Edit();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e) {
            base.OnPointerReleased(e);
            if (!e.Handled && e.InitialPressMouseButton == MouseButton.Left) {
                e.Handled = true;
                Edit();
            }
        }
    }
}