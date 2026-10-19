using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace FirstFloor.ModernUI.Windows.Attached {
    /// <summary>
    /// DataGrid columns aren’t named elements in Avalonia, so x:Name of a column is stored in this attached property (see PortTools/xaml_convert.py)
    /// and looked up with <see cref="Find{T}"/> from generated `.Names.cs` files.
    /// </summary>
    public static class ColumnNames {
        public static readonly AttachedProperty<string> NameProperty = AvaloniaProperty.RegisterAttached<DataGridColumn, DataGridColumn, string>("Name");

        public static string GetName(DataGridColumn obj) => obj.GetValue(NameProperty);
        public static void SetName(DataGridColumn obj, string value) => obj.SetValue(NameProperty, value);

        public static T Find<T>(ILogical root, string name) where T : DataGridColumn {
            return root.GetLogicalDescendants().OfType<DataGrid>().SelectMany(x => x.Columns)
                    .OfType<T>().FirstOrDefault(x => string.Equals(GetName(x), name, StringComparison.Ordinal));
        }
    }
}
