using System;
using System.Linq;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace FirstFloor.ModernUI.Helpers {
    /// <summary>
    /// Debug helper for the port: dumps visual trees of all windows to the log (enabled by CM_DUMP_TREE environment variable).
    /// </summary>
    public static class VisualTreeDumper {
        public static void DumpAfter(TimeSpan delay) {
            var timer = new Avalonia.Threading.DispatcherTimer { Interval = delay };
            timer.Tick += (s, e) => {
                timer.Stop();
                var windows = (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)?.Windows;
                if (windows == null) return;
                foreach (var w in windows.ToList()) {
                    var sb = new StringBuilder();
                    Dump(w, 0, sb);
                    Logging.Write("Tree of " + w.GetType().Name + ":\n" + sb);
                }
            };
            timer.Start();
        }

        private static void Dump(Visual v, int depth, StringBuilder sb) {
            if (depth > 40) return;
            sb.Append(' ', depth * 2).Append(v.GetType().Name);
            if (v is Control c && !string.IsNullOrEmpty(c.Name)) sb.Append('#').Append(c.Name);
            sb.Append(' ').Append(v.Bounds.Width.ToString("F0")).Append('x').Append(v.Bounds.Height.ToString("F0"));
            if (!v.IsVisible) sb.Append(" (hidden)");
            if (v is TextBlock t && !string.IsNullOrEmpty(t.Text)) sb.Append(" “").Append(t.Text.Length > 40 ? t.Text.Substring(0, 40) : t.Text).Append('”');
            sb.AppendLine();
            foreach (var child in v.GetVisualChildren()) {
                Dump(child, depth + 1, sb);
            }
        }
    }
}
