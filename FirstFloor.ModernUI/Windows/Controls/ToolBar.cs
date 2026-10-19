using System;
using System.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Replacement for WPF ToolBar (Avalonia doesn’t have one). Children get their ToolBar styles by the theme.
    /// </summary>
    public class ToolBar : ItemsControl {
        public static readonly string ButtonStyleKey = "ToolBar.ButtonStyle";
        public static readonly string ToggleButtonStyleKey = "ToolBar.ToggleButtonStyle";
        public static readonly string CheckBoxStyleKey = "ToolBar.CheckBoxStyle";
        public static readonly string RadioButtonStyleKey = "ToolBar.RadioButtonStyle";
        public static readonly string MenuStyleKey = "ToolBar.MenuStyle";
        public static readonly string SeparatorStyleKey = "ToolBar.SeparatorStyle";
        public static readonly string TextBoxStyleKey = "ToolBar.TextBoxStyle";
        public static readonly string ComboBoxStyleKey = "ToolBar.ComboBoxStyle";

        protected override Type StyleKeyOverride => typeof(ToolBar);

        public static readonly StyledProperty<Orientation> OrientationProperty = StackPanel.OrientationProperty.AddOwner<ToolBar>();

        public Orientation Orientation {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly AttachedProperty<bool> IsLockedProperty = AvaloniaProperty.RegisterAttached<ToolBar, Control, bool>("IsLocked");
        public static bool GetIsLocked(Control c) => c.GetValue(IsLockedProperty);
        public static void SetIsLocked(Control c, bool v) => c.SetValue(IsLockedProperty, v);

        public static readonly StyledProperty<int> BandProperty = AvaloniaProperty.Register<ToolBar, int>(nameof(Band));
        public int Band {
            get => GetValue(BandProperty);
            set => SetValue(BandProperty, value);
        }

        public static readonly StyledProperty<int> BandIndexProperty = AvaloniaProperty.Register<ToolBar, int>(nameof(BandIndex));
        public int BandIndex {
            get => GetValue(BandIndexProperty);
            set => SetValue(BandIndexProperty, value);
        }
    }

    public class ToolBarTray : ItemsControl {
        protected override Type StyleKeyOverride => typeof(ToolBarTray);

        /// <summary>WPF compatible name for the items.</summary>
        public IList ToolBars => Items;

        public static readonly StyledProperty<Orientation> OrientationProperty = StackPanel.OrientationProperty.AddOwner<ToolBarTray>();

        public Orientation Orientation {
            get => GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly StyledProperty<bool> IsLockedProperty = AvaloniaProperty.Register<ToolBarTray, bool>(nameof(IsLocked));

        public bool IsLocked {
            get => GetValue(IsLockedProperty);
            set => SetValue(IsLockedProperty, value);
        }
    }
}
