using System;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    [Localizable(false)]
    public static class DependencyObjectHelper {
        [NotNull]
        public static T RequireChild<T>([CanBeNull] this Avalonia.AvaloniaObject parent, [CanBeNull] string childName) where T : Avalonia.Controls.Control {
            return parent.FindChild<T>(childName) ?? throw new ArgumentException($"Child with type {typeof(T).Name} named “{childName}” not found");
        }

        [CanBeNull]
        public static T FindChild<T>([CanBeNull] this Avalonia.AvaloniaObject parent, [CanBeNull] string childName) where T : Avalonia.Controls.Control {
            if (!(parent is Avalonia.Visual visual)) return null;
            return visual.GetVisualDescendants().OfType<T>().FirstOrDefault(t => string.IsNullOrEmpty(childName) || t.Name == childName);
        }
    }
}
