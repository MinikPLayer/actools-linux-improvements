using System.Windows;
using System.Windows.Controls;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <inheritdoc />
    /// <summary>
    /// Represents a control that indicates that an operation is ongoing.
    /// </summary>
    public class ModernProgressRing : Control {
        /// <summary>
        /// Identifies the IsActive property.
        /// </summary>
        public static readonly StyledProperty<bool> IsActiveProperty = DP.Register<bool>("IsActive", typeof(ModernProgressRing),
                new PropertyMetadata(false, OnIsActiveChanged));

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:FirstFloor.ModernUI.Windows.Controls.ModernProgressRing" /> class.
        /// </summary>
        public ModernProgressRing() {
        }

        private static void OnIsActiveChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ModernProgressRing)o).Classes.Set(":active", (bool)e.NewValue);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the <see cref="ModernProgressRing"/> is showing progress.
        /// </summary>
        public bool IsActive {
            get => GetValue(IsActiveProperty) as bool? == true;
            set => SetValue(IsActiveProperty, value);
        }

        #region For glowing
        public static readonly StyledProperty<double> DensityMultiplierProperty = DP.Register<double>(nameof(DensityMultiplier),
                typeof(ModernProgressRing), new FrameworkPropertyMetadata(1d));

        public double DensityMultiplier {
            get => GetValue(DensityMultiplierProperty) as double? ?? 0d;
            set => SetValue(DensityMultiplierProperty, value);
        }
        #endregion
    }
}
