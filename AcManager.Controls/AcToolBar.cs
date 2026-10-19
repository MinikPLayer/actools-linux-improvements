using FirstFloor.ModernUI.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace AcManager.Controls {
    [ContentProperty(nameof(ToolBars))]
    public class AcToolBar : Control {
        protected override System.Type StyleKeyOverride => typeof(AcToolBar);

        public static readonly StyledProperty<Avalonia.Layout.VerticalAlignment> VerticalContentAlignmentProperty =
                Avalonia.Controls.ContentControl.VerticalContentAlignmentProperty.AddOwner<AcToolBar>();

        public Avalonia.Layout.VerticalAlignment VerticalContentAlignment {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }

        public AcToolBar() {
            SetCurrentValue(ToolBarsProperty, new Collection<ToolBar>());
        }

        private ToolBarTray ToolBarTray => this.GetTemplateChild(@"PART_ToolBarTray") as ToolBarTray;

        public static readonly StyledProperty<Collection<ToolBar>> ToolBarsProperty = DP.Register<Collection<ToolBar>>(nameof(ToolBars),
                                                                                             typeof(AcToolBar));

        [Avalonia.Metadata.Content] public Collection<ToolBar> ToolBars {
            get => (Collection<ToolBar>)GetValue(ToolBarsProperty);
            set => SetValue(ToolBarsProperty, value);
        }

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            base.OnApplyTemplate(e);

            var tray = ToolBarTray;
            if (tray == null) return;
            foreach (var toolBar in ToolBars) {
                tray.ToolBars.Add(toolBar);
            }
        }

        public static readonly StyledProperty<bool> FitWidthProperty = DP.Register<bool>(nameof(FitWidth),
                typeof(AcToolBar), new PropertyMetadata(false));

        public bool FitWidth {
            get => GetValue(FitWidthProperty) as bool? == true;
            set => SetValue(FitWidthProperty, value);
        }

        #region Toggling attributes
        public static readonly StyledProperty<bool> IsTogglableProperty = DP.Register<bool>("IsTogglable",
            typeof(AcToolBar), new PropertyMetadata());

        public bool IsTogglable {
            get => GetValue(IsTogglableProperty) as bool? == true;
            set => SetValue(IsTogglableProperty, value);
        }

        public static readonly StyledProperty<bool> IsActiveProperty = DP.Register<bool>("IsActive",
            typeof(AcToolBar), new PropertyMetadata(false));

        public bool IsActive {
            get => GetValue(IsActiveProperty) as bool? == true;
            set => SetValue(IsActiveProperty, value);
        }
        #endregion
    }
}
