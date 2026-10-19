using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace FirstFloor.ModernUI.Windows.Controls {
    public enum AdornerPlacement {
        Inside,
        Outside
    }

    /// <summary>
    /// Shows additional content over the main one, optionally only on hover. The WPF version used adorners, here it’s a simple overlay.
    /// </summary>
    public class AdornedControl : ContentControl {
        public static readonly StyledProperty<bool> IsAdornerVisibleProperty = DP.Register<bool>(nameof(IsAdornerVisible), typeof(AdornedControl), new PropertyMetadata(false));
        public bool IsAdornerVisible { get => GetValue(IsAdornerVisibleProperty); set => SetValue(IsAdornerVisibleProperty, value); }

        public static readonly StyledProperty<FrameworkElement> AdornerContentProperty = DP.Register<FrameworkElement>(nameof(AdornerContent), typeof(AdornedControl));
        public FrameworkElement AdornerContent { get => GetValue(AdornerContentProperty); set => SetValue(AdornerContentProperty, value); }

        public static readonly StyledProperty<AdornerPlacement> HorizontalAdornerPlacementProperty = DP.Register<AdornerPlacement>(nameof(HorizontalAdornerPlacement),
                typeof(AdornedControl), new PropertyMetadata(AdornerPlacement.Inside));
        public AdornerPlacement HorizontalAdornerPlacement { get => GetValue(HorizontalAdornerPlacementProperty); set => SetValue(HorizontalAdornerPlacementProperty, value); }

        public static readonly StyledProperty<AdornerPlacement> VerticalAdornerPlacementProperty = DP.Register<AdornerPlacement>(nameof(VerticalAdornerPlacement),
                typeof(AdornedControl), new PropertyMetadata(AdornerPlacement.Inside));
        public AdornerPlacement VerticalAdornerPlacement { get => GetValue(VerticalAdornerPlacementProperty); set => SetValue(VerticalAdornerPlacementProperty, value); }

        public static readonly StyledProperty<double> AdornerOffsetXProperty = DP.Register<double>(nameof(AdornerOffsetX), typeof(AdornedControl), new PropertyMetadata(0d));
        public double AdornerOffsetX { get => GetValue(AdornerOffsetXProperty); set => SetValue(AdornerOffsetXProperty, value); }

        public static readonly StyledProperty<double> AdornerOffsetYProperty = DP.Register<double>(nameof(AdornerOffsetY), typeof(AdornedControl), new PropertyMetadata(0d));
        public double AdornerOffsetY { get => GetValue(AdornerOffsetYProperty); set => SetValue(AdornerOffsetYProperty, value); }

        public static readonly StyledProperty<int> OrderProperty = DP.Register<int>(nameof(Order), typeof(AdornedControl), new PropertyMetadata(0));
        public int Order { get => GetValue(OrderProperty); set => SetValue(OrderProperty, value); }

        public static readonly StyledProperty<bool> AvoidUsingScrollContentPresenterProperty = DP.Register<bool>(nameof(AvoidUsingScrollContentPresenter),
                typeof(AdornedControl), new PropertyMetadata(false));
        public bool AvoidUsingScrollContentPresenter { get => GetValue(AvoidUsingScrollContentPresenterProperty); set => SetValue(AvoidUsingScrollContentPresenterProperty, value); }

        public static readonly StyledProperty<bool> ShowAdornerOnHoverProperty = DP.Register<bool>(nameof(ShowAdornerOnHover), typeof(AdornedControl), new PropertyMetadata(false));
        public bool ShowAdornerOnHover { get => GetValue(ShowAdornerOnHoverProperty); set => SetValue(ShowAdornerOnHoverProperty, value); }

        public static readonly AttachedProperty<int> ZOrderProperty = DP.RegisterAttached<int>("ZOrder", typeof(AdornedControl), new PropertyMetadata(0));
        public static int GetZOrder(DependencyObject o) => o.GetValue(ZOrderProperty);
        public static void SetZOrder(DependencyObject o, int value) => o.SetValue(ZOrderProperty, value);

        protected override Type StyleKeyOverride => typeof(AdornedControl);
    }
}
