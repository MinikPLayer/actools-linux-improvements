using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace FirstFloor.ModernUI.Presentation {
    /// <summary>
    /// WPF allowed <c>&lt;SolidColorBrush x:Key="…" Color="{DynamicResource SomeColor}"/&gt;</c> in resource dictionaries, but in Avalonia such
    /// a brush can’t find the color (it ends up transparent). Instead, <c>cdb:DynamicColorBrush.ColorKey="SomeColor"</c> makes the brush look
    /// the color up in the application resources and follow them: <see cref="Refresh"/> has to be called after the colors have been changed.
    /// </summary>
    public class DynamicColorBrush : AvaloniaObject {
        private static readonly List<WeakReference<SolidColorBrush>> Instances = new List<WeakReference<SolidColorBrush>>();

        public static readonly AttachedProperty<string> ColorKeyProperty = AvaloniaProperty.RegisterAttached<DynamicColorBrush, SolidColorBrush, string>("ColorKey");

        static DynamicColorBrush() {
            ColorKeyProperty.Changed.AddClassHandler<SolidColorBrush>((brush, e) => {
                lock (Instances) {
                    Instances.Add(new WeakReference<SolidColorBrush>(brush));
                }

                Update(brush);
            });
        }

        public static string GetColorKey(SolidColorBrush brush) => brush.GetValue(ColorKeyProperty);

        public static void SetColorKey(SolidColorBrush brush, string value) => brush.SetValue(ColorKeyProperty, value);

        private static void Update(SolidColorBrush brush) {
            var key = GetColorKey(brush);
            var app = Application.Current;
            if (key == null || app == null) return;
            if (app.TryGetResource(key, app.ActualThemeVariant, out var value) && value is Color color) {
                brush.Color = color;
            }
        }

        public static void Refresh() {
            lock (Instances) {
                for (var i = Instances.Count - 1; i >= 0; i--) {
                    if (Instances[i].TryGetTarget(out var brush)) {
                        Update(brush);
                    } else {
                        Instances.RemoveAt(i);
                    }
                }
            }
        }
    }
}
