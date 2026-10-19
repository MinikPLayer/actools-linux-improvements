using System.Windows;
using Avalonia.Controls;

namespace FirstFloor.ModernUI.Windows.Attached {
    /// <summary>
    /// Placeholder until Draggable is ported (see LinuxPortDocs.md).
    /// </summary>
    public static class Draggable {
        public static event System.EventHandler<DraggableMovedEventArgs> DragStarted { add { } remove { } }
        public static event System.EventHandler<DraggableMovedEventArgs> DragEnded { add { } remove { } }

        public static void RegisterDraggable(System.Type type) { }
        public static void RegisterDraggable<T>() { }

        public const string SourceFormat = "X-Draggable-Source";

        public static readonly AttachedProperty<object> DataProperty = DP.RegisterAttached<object>("Data", typeof(Draggable), new PropertyMetadata(null));
        public static object GetData(DependencyObject o) => o.GetValue(DataProperty);
        public static void SetData(DependencyObject o, object value) => o.SetValue(DataProperty, value);

        public static readonly AttachedProperty<bool> KeepSelectionProperty = DP.RegisterAttached<bool>("KeepSelection", typeof(Draggable), new PropertyMetadata(false));
        public static bool GetKeepSelection(DependencyObject o) => o.GetValue(KeepSelectionProperty);
        public static void SetKeepSelection(DependencyObject o, bool value) => o.SetValue(KeepSelectionProperty, value);

        public static readonly AttachedProperty<bool> EnabledProperty = DP.RegisterAttached<bool>("Enabled", typeof(Draggable), new PropertyMetadata(false));
        public static bool GetEnabled(DependencyObject o) => o.GetValue(EnabledProperty);
        public static void SetEnabled(DependencyObject o, bool value) => o.SetValue(EnabledProperty, value);

        public static readonly AttachedProperty<bool> ForceDisabledProperty = DP.RegisterAttached<bool>("ForceDisabled", typeof(Draggable), new PropertyMetadata(false));
        public static bool GetForceDisabled(DependencyObject o) => o.GetValue(ForceDisabledProperty);
        public static void SetForceDisabled(DependencyObject o, bool value) => o.SetValue(ForceDisabledProperty, value);

        public static int GetMouseItemIndex(this ItemsControl itemsControl) => -1;

        public static bool IsCopyAction(this Avalonia.Input.DragEventArgs e) => e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Control);
        public static bool IsSpecificAction(this Avalonia.Input.DragEventArgs e) => e.KeyModifiers.HasFlag(Avalonia.Input.KeyModifiers.Shift);
    }
}
