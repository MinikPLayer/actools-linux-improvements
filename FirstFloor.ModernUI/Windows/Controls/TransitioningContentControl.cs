using System;
using System.Windows;
using Avalonia.Animation;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Content control which animates content changes (WPF version used visual states with storyboards, here it’s Avalonia’s page transitions).
    /// </summary>
    public class TransitioningContentControl : Avalonia.Controls.TransitioningContentControl {
        public const string DefaultTransitionState = "DefaultTransition";

        public static readonly StyledProperty<string> TransitionProperty = DP.Register<string>(nameof(Transition), typeof(TransitioningContentControl),
                new PropertyMetadata(DefaultTransitionState, OnTransitionChanged));

        public string Transition {
            get => GetValue(TransitionProperty);
            set => SetValue(TransitionProperty, value);
        }

        public static readonly StyledProperty<bool> RestartTransitionOnContentChangeProperty = DP.Register<bool>(nameof(RestartTransitionOnContentChange),
                typeof(TransitioningContentControl), new PropertyMetadata(false));

        public bool RestartTransitionOnContentChange {
            get => GetValue(RestartTransitionOnContentChangeProperty);
            set => SetValue(RestartTransitionOnContentChangeProperty, value);
        }

        public bool IsTransitioning => false;

        public TransitioningContentControl() {
            PageTransition = new CrossFade(TimeSpan.FromMilliseconds(150));
        }

        protected override Type StyleKeyOverride => typeof(Avalonia.Controls.TransitioningContentControl);

        private static void OnTransitionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            var control = (TransitioningContentControl)o;
            switch (e.NewValue as string) {
                case "UpTransition":
                    control.PageTransition = new PageSlide(TimeSpan.FromMilliseconds(200), PageSlide.SlideAxis.Vertical);
                    break;
                case "DownTransition":
                case "RightTransition":
                    control.PageTransition = new PageSlide(TimeSpan.FromMilliseconds(200), PageSlide.SlideAxis.Horizontal);
                    break;
                case "None":
                case null:
                    control.PageTransition = null;
                    break;
                default:
                    control.PageTransition = new CrossFade(TimeSpan.FromMilliseconds(150));
                    break;
            }
        }
    }
}
