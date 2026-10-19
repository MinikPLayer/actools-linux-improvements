using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows {
    /// <summary>
    /// Desktop notifications. On Linux the system notification daemon is used (via notify-send) when it’s available,
    /// otherwise (and on Windows) a small borderless window is shown in the bottom right corner of the screen.
    /// </summary>
    public static class Toast {
        public static bool OptionFallbackMode = false;

        private static Action _defaultAction;

        public static void SetDefaultAction(Action defaultAction) {
            _defaultAction = defaultAction;
        }

        /// <summary>
        /// Show a toast.
        /// </summary>
        /// <param name="title">Ex.: “Something Happened”</param>
        /// <param name="message">Ex.: “This and that. Without dot in the end”</param>
        /// <param name="click">Click action</param>
        public static void Show(string title, string message, Action click = null) {
            Show(title.ToTitle(), message, null, click ?? _defaultAction);
        }

        /// <summary>
        /// Show a toast.
        /// </summary>
        /// <param name="title">Ex.: “Something Happened”</param>
        /// <param name="message">Ex.: “This and that. Without dot in the end”</param>
        /// <param name="icon">Uri to some icon</param>
        /// <param name="click">Click action</param>
        public static void Show(string title, string message, [CanBeNull] Uri icon, Action click = null) {
            ActionExtension.InvokeInMainThreadAsync(() => {
                if (OperatingSystem.IsLinux() && !OptionFallbackMode && TryNotifySend(title, message)) return;
                ShowFallback(title, message, click);
            });
        }

        private static bool? _notifySendAvailable;

        private static bool TryNotifySend(string title, string message) {
            if (_notifySendAvailable == false) return false;

            try {
                if (_notifySendAvailable == null) {
                    var path = Environment.GetEnvironmentVariable("PATH") ?? "";
                    _notifySendAvailable = path.Split(':').Any(x => File.Exists(Path.Combine(x, "notify-send")))
                            && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS"));
                    if (_notifySendAvailable == false) return false;
                }

                var info = new ProcessStartInfo("notify-send") { UseShellExecute = false, CreateNoWindow = true };
                info.ArgumentList.Add("--app-name=Content Manager");
                info.ArgumentList.Add(title);
                info.ArgumentList.Add(message);
                Process.Start(info);
                return true;
            } catch (Exception e) {
                Logging.Warning("notify-send isn’t available: " + e.Message);
                _notifySendAvailable = false;
                return false;
            }
        }

        private static void ShowFallback(string title, string message, Action click) {
            try {
                var window = new Window {
                    Width = 340,
                    SizeToContent = SizeToContent.Height,
                    SystemDecorations = SystemDecorations.None,
                    ShowInTaskbar = false,
                    Topmost = true,
                    ShowActivated = false,
                    CanResize = false,
                    Background = new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)),
                    Content = new Border {
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(12),
                        Child = new StackPanel {
                            Spacing = 4,
                            Children = {
                                new TextBlock { Text = title, FontWeight = FontWeight.Bold, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap },
                                new TextBlock { Text = message, Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap }
                            }
                        }
                    }
                };

                window.PointerPressed += (sender, args) => {
                    window.Close();
                    click?.Invoke();
                };

                window.Opened += (sender, args) => {
                    var screen = window.Screens?.Primary;
                    if (screen != null) {
                        var area = screen.WorkingArea;
                        var scale = screen.Scaling;
                        window.Position = new PixelPoint(
                                area.Right - (int)((window.Width + 12) * scale),
                                area.Bottom - (int)((window.Bounds.Height + 12) * scale));
                    }
                };

                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (sender, args) => {
                    timer.Stop();
                    window.Close();
                };
                window.Closed += (sender, args) => timer.Stop();
                timer.Start();
                window.Show();
            } catch (Exception e) {
                Logging.Error(e);
            }
        }
    }
}
