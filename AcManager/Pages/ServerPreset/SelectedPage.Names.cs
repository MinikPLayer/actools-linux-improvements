// Named objects which are not controls (no generated fields for them), see PortTools/gen_names.py
namespace AcManager.Pages.ServerPreset {
    public partial class SelectedPage {
        private global::FirstFloor.ModernUI.Presentation.Link _RunningLogLink_;
        private global::FirstFloor.ModernUI.Presentation.Link RunningLogLink => _RunningLogLink_ ?? (_RunningLogLink_ = FirstFloor.ModernUI.Helpers.NamedObjects.Find(this, "RunningLogLink") as global::FirstFloor.ModernUI.Presentation.Link);

        private global::FirstFloor.ModernUI.Presentation.Link _RunningStatusLink_;
        private global::FirstFloor.ModernUI.Presentation.Link RunningStatusLink => _RunningStatusLink_ ?? (_RunningStatusLink_ = FirstFloor.ModernUI.Helpers.NamedObjects.Find(this, "RunningStatusLink") as global::FirstFloor.ModernUI.Presentation.Link);
    }
}
