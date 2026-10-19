using System;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>
    /// Debug helper for the port: dumps visual trees of all windows to the log (enabled by CM_DUMP_TREE environment variable).
    /// </summary>
    public static class VisualTreeDumper {
        public static void DumpAfter(TimeSpan delay) {
            System.Threading.Tasks.Task.Run(async () => {
                await System.Threading.Tasks.Task.Delay(delay);
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    var windows = (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Windows;
                    if (windows == null) return;
                    foreach (var w in windows.ToList()) {
                        var sb = new StringBuilder();
                        Dump(w, 0, sb);
                        System.IO.File.AppendAllText("/tmp/cm_tree.txt", "Tree of " + w.GetType().Name + ":\n" + sb + "\n");
                    }
                });
            });
        }

        private static void Dump(Visual v, int depth, StringBuilder sb) {
            if (depth > 40) return;
            sb.Append(' ', depth * 2).Append(v.GetType().Name);
            if (v is Control c && !string.IsNullOrEmpty(c.Name)) sb.Append('#').Append(c.Name);
            sb.Append(' ').Append(v.Bounds.Width.ToString("F0")).Append('x').Append(v.Bounds.Height.ToString("F0"));
            if (!v.IsVisible) sb.Append(" (hidden)");
            if (v is Layoutable lay && lay.Margin != default) sb.Append(" m=").Append(lay.Margin);
            if (v is TemplatedControl tc && tc.Theme != null) sb.Append(" theme");
            if (v is ItemsControl ic) sb.Append(" items=").Append(ic.ItemCount).Append(" src=").Append(ic.ItemsSource?.GetType().Name ?? "null").Append(" tpl=").Append(ic.Template?.GetType().Name ?? "null").Append(" theme=").Append(ic.Theme != null).Append(" panel=").Append(ic.ItemsPanel?.GetType().Name ?? "null").Append(" vc=").Append(ic.GetVisualChildren().Count());
            if (v is TextBlock t && !string.IsNullOrEmpty(t.Text)) sb.Append(" “").Append(t.Text.Length > 40 ? t.Text.Substring(0, 40) : t.Text).Append('”');
            sb.AppendLine();
            foreach (var child in v.GetVisualChildren()) {
                Dump(child, depth + 1, sb);
            }
        }
    }
}
