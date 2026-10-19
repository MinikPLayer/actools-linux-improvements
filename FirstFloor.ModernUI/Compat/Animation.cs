using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace System.Windows {
    public enum FillBehavior {
        HoldEnd, Stop
    }

    public struct Duration {
        public Duration(TimeSpan timeSpan) {
            TimeSpan = timeSpan;
        }

        public TimeSpan TimeSpan { get; }

        public static implicit operator Duration(TimeSpan timeSpan) => new Duration(timeSpan);
    }
}

namespace System.Windows.Media.Animation {
    /// <summary>
    /// Minimal emulation of WPF animations started by BeginAnimation(): from/to on a double or color property with duration, easing,
    /// begin time and fill behavior. Finished animations are held by setting the value locally.
    /// </summary>
    public abstract class AnimationTimeline {
        public Duration Duration { get; set; } = new Duration(TimeSpan.FromSeconds(1));
        public TimeSpan? BeginTime { get; set; }
        public FillBehavior FillBehavior { get; set; }
        public Easing EasingFunction { get; set; }
        public Action Completed;

        internal abstract object GetFrom(AvaloniaObject target, AvaloniaProperty property);
        internal abstract object GetTo(AvaloniaObject target, AvaloniaProperty property);
    }

    public class DoubleAnimation : AnimationTimeline {
        public DoubleAnimation() { }

        public DoubleAnimation(double to, Duration duration) {
            To = to;
            Duration = duration;
        }

        public DoubleAnimation(double from, double to, Duration duration) {
            From = from;
            To = to;
            Duration = duration;
        }

        public double? From { get; set; }
        public double? To { get; set; }
        public double? By { get; set; }

        internal override object GetFrom(AvaloniaObject target, AvaloniaProperty property)
            => From ?? Convert.ToDouble(target.GetValue(property));

        internal override object GetTo(AvaloniaObject target, AvaloniaProperty property)
            => To ?? (From ?? Convert.ToDouble(target.GetValue(property))) + (By ?? 0d);
    }

    public class ColorAnimation : AnimationTimeline {
        public ColorAnimation() { }

        public ColorAnimation(Color to, Duration duration) {
            To = to;
            Duration = duration;
        }

        public ColorAnimation(Color from, Color to, Duration duration) {
            From = from;
            To = to;
            Duration = duration;
        }

        public Color? From { get; set; }
        public Color? To { get; set; }

        internal override object GetFrom(AvaloniaObject target, AvaloniaProperty property)
            => From ?? (Color)target.GetValue(property);

        internal override object GetTo(AvaloniaObject target, AvaloniaProperty property)
            => To ?? From ?? (Color)target.GetValue(property);
    }

    public static class AnimationExtension {
        private static readonly ConditionalWeakTable<AvaloniaObject, System.Collections.Generic.Dictionary<AvaloniaProperty, CancellationTokenSource>> Running
                = new ConditionalWeakTable<AvaloniaObject, System.Collections.Generic.Dictionary<AvaloniaProperty, CancellationTokenSource>>();

        extension(AvaloniaObject target) {
            /// <summary>Starts (or, with null, stops) an animation of the property.</summary>
            public void BeginAnimation(AvaloniaProperty property, AnimationTimeline timeline) {
                var running = Running.GetOrCreateValue(target);
                lock (running) {
                    if (running.TryGetValue(property, out var previous)) {
                        previous.Cancel();
                        running.Remove(property);
                    }

                    if (timeline == null || !(target is Animatable animatable)) return;

                    var cancellation = new CancellationTokenSource();
                    running[property] = cancellation;

                    var from = timeline.GetFrom(target, property);
                    var to = timeline.GetTo(target, property);
                    var animation = new Avalonia.Animation.Animation {
                        Duration = timeline.Duration.TimeSpan,
                        Delay = timeline.BeginTime ?? TimeSpan.Zero,
                        Easing = timeline.EasingFunction ?? new LinearEasing(),
                        FillMode = FillMode.Both,
                        Children = {
                            new KeyFrame { Cue = new Cue(0d), Setters = { new Setter(property, from) } },
                            new KeyFrame { Cue = new Cue(1d), Setters = { new Setter(property, to) } }
                        }
                    };

                    Run();

                    async void Run() {
                        try {
                            await animation.RunAsync(animatable, cancellation.Token);
                            if (!cancellation.IsCancellationRequested) {
                                if (timeline.FillBehavior == FillBehavior.HoldEnd) {
                                    target.SetValue(property, to);
                                }

                                lock (running) {
                                    if (running.TryGetValue(property, out var current) && current == cancellation) {
                                        running.Remove(property);
                                    }
                                }

                                timeline.Completed?.Invoke();
                            }
                        } catch (OperationCanceledException) { }
                    }
                }
            }
        }
    }
}
