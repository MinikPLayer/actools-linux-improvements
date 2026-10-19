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

        /// <summary>
        /// Debug helper: navigates the main window through all links of its menu, writes the current one to /tmp/cm_crawl.txt.
        /// </summary>
        public static void CrawlPages(TimeSpan delay, TimeSpan perPage) {
            System.Threading.Tasks.Task.Run(async () => {
                await System.Threading.Tasks.Task.Delay(delay);
                var uris = new System.Collections.Generic.List<Uri>();
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                    var window = (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
                            ?.Windows.OfType<FirstFloor.ModernUI.Windows.Controls.ModernWindow>().FirstOrDefault();
                    if (window == null) return;
                    void Add(FirstFloor.ModernUI.Presentation.Link link) {
                        if (link.Source != null) uris.Add(link.Source);
                        if (link is FirstFloor.ModernUI.Presentation.LinkWithList list) {
                            foreach (var child in list.Children) Add(child);
                        }
                    }

                    foreach (var group in window.MenuLinkGroups) {
                        foreach (var link in group.Links) Add(link);
                    }
                });

                var index = 0;
                foreach (var uri in uris.Distinct()) {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => {
                        var window = (Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)
                                ?.Windows.OfType<FirstFloor.ModernUI.Windows.Controls.ModernWindow>().FirstOrDefault();
                        window?.NavigateTo(uri);
                    });
                    System.IO.File.WriteAllText("/tmp/cm_crawl.txt", (++index) + " " + uri);
                    await System.Threading.Tasks.Task.Delay(perPage);
                }

                System.IO.File.WriteAllText("/tmp/cm_crawl.txt", "done");
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
