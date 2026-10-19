using System;
using Avalonia.Threading;

namespace System.Windows.Media {
    public class RenderingEventArgs : EventArgs {
        public RenderingEventArgs(TimeSpan renderingTime) {
            RenderingTime = renderingTime;
        }

        public TimeSpan RenderingTime { get; }
    }
}

namespace FirstFloor.ModernUI.Windows {
    /// <summary>
    /// Per-frame callbacks, driven by a ~60 FPS timer (Avalonia has no global “rendering” event).
    /// </summary>
    public static class CompositionTargetEx {
        private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();
        private static DispatcherTimer _timer;
        private static event EventHandler<System.Windows.Media.RenderingEventArgs> FrameUpdating;

        public static event EventHandler<System.Windows.Media.RenderingEventArgs> Rendering {
            add {
                if (FrameUpdating == null) {
                    _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, OnTick);
                    _timer.Start();
                }

                FrameUpdating += value;
            }
            remove {
                FrameUpdating -= value;
                if (FrameUpdating == null) {
                    _timer?.Stop();
                    _timer = null;
                }
            }
        }

        private static void OnTick(object sender, EventArgs e) {
            FrameUpdating?.Invoke(sender, new System.Windows.Media.RenderingEventArgs(Clock.Elapsed));
        }
    }
}
