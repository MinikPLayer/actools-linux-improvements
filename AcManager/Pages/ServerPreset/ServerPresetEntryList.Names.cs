// Named objects which are not controls (no generated fields for them), see PortTools/gen_names.py
namespace AcManager.Pages.ServerPreset {
    public partial class ServerPresetEntryList {
        private global::Avalonia.Controls.DataGridTemplateColumn _TeamColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn TeamColumn => _TeamColumn_ ?? (_TeamColumn_ = FirstFloor.ModernUI.Windows.Attached.ColumnNames.Find<global::Avalonia.Controls.DataGridTemplateColumn>(this, "TeamColumn"));

        private global::Avalonia.Controls.DataGridTemplateColumn _BallastColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn BallastColumn => _BallastColumn_ ?? (_BallastColumn_ = FirstFloor.ModernUI.Windows.Attached.ColumnNames.Find<global::Avalonia.Controls.DataGridTemplateColumn>(this, "BallastColumn"));

        private global::Avalonia.Controls.DataGridTemplateColumn _RestrictorColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn RestrictorColumn => _RestrictorColumn_ ?? (_RestrictorColumn_ = FirstFloor.ModernUI.Windows.Attached.ColumnNames.Find<global::Avalonia.Controls.DataGridTemplateColumn>(this, "RestrictorColumn"));
    }
}
