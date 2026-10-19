// Named objects which are not controls (no generated fields for them), see PortTools/gen_names.py
namespace AcManager.Pages.AcSettings {
    public partial class AcSettingsPython_detailed {
        private global::Avalonia.Controls.DataGridTemplateColumn _PositionColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn PositionColumn => _PositionColumn_ ?? (_PositionColumn_ = FirstFloor.ModernUI.Windows.Attached.ColumnNames.Find<global::Avalonia.Controls.DataGridTemplateColumn>(this, "PositionColumn"));

        private global::Avalonia.Controls.DataGridTemplateColumn _ScaleColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn ScaleColumn => _ScaleColumn_ ?? (_ScaleColumn_ = FirstFloor.ModernUI.Windows.Attached.ColumnNames.Find<global::Avalonia.Controls.DataGridTemplateColumn>(this, "ScaleColumn"));

        private global::Avalonia.Controls.DataGridTemplateColumn _BlockedColumn_;
        internal global::Avalonia.Controls.DataGridTemplateColumn BlockedColumn => _BlockedColumn_ ?? (_BlockedColumn_ = FirstFloor.ModernUI.Windows.Attached.ColumnNames.Find<global::Avalonia.Controls.DataGridTemplateColumn>(this, "BlockedColumn"));
    }
}
