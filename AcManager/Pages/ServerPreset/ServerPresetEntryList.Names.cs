// Named objects which are not controls (no generated fields for them), see PortTools/gen_names.py
namespace AcManager.Pages.ServerPreset {
    public partial class ServerPresetEntryList {
        private global::Avalonia.Controls.DataGridTemplateColumn _TeamColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn TeamColumn => _TeamColumn_ ?? (_TeamColumn_ = Avalonia.Controls.NameScopeExtensions.FindNameScope(this)?.Find("TeamColumn") as global::Avalonia.Controls.DataGridTemplateColumn);

        private global::Avalonia.Controls.DataGridTemplateColumn _BallastColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn BallastColumn => _BallastColumn_ ?? (_BallastColumn_ = Avalonia.Controls.NameScopeExtensions.FindNameScope(this)?.Find("BallastColumn") as global::Avalonia.Controls.DataGridTemplateColumn);

        private global::Avalonia.Controls.DataGridTemplateColumn _RestrictorColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn RestrictorColumn => _RestrictorColumn_ ?? (_RestrictorColumn_ = Avalonia.Controls.NameScopeExtensions.FindNameScope(this)?.Find("RestrictorColumn") as global::Avalonia.Controls.DataGridTemplateColumn);
    }
}
