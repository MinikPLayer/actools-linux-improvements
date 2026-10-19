using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Windows.Media;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Attached {
    public class FancyHintsService : NotifyPropertyChanged {
        public static readonly FancyHintsService Instance = new FancyHintsService();

        private FancyHintsService() { }

        private bool? _enabled;

        public bool Enabled {
            get => _enabled ?? (_enabled = ValuesStorage.Get("Settings.FancyHintsService.Enabled", true)).Value;
            set {
                if (Equals(value, _enabled)) return;
                _enabled = value;
                ValuesStorage.Set("Settings.FancyHintsService.Enabled", value);
                OnPropertyChanged();
            }
        }

        private static readonly List<FancyHint> Hints = new List<FancyHint>(10);

        internal static void Register(FancyHint hint) {
            Hints.Add(hint);
            hint.Show += OnHintShow;
            hint.Unnecessary += OnHintUnnecessary;
        }

        private static Tuple<string, FrameworkElement> _nextHint;

        private static bool TryToShow(FrameworkElement element, FancyHint hint) {
            // TODO: hint bubbles (adorners in the original) aren’t ported yet
            return false;
        }

        private static void OnHintUnnecessary(object sender, EventArgs eventArgs) {
            var hint = (FancyHint)sender;

        }

        private static void OnHintShow(object sender, ShowHintEventArgs eventArgs) {
            if (!Instance.Enabled) return;

            var hint = (FancyHint)sender;

            if (_nextHint != null) {
                var next = _nextHint;
                _nextHint = null;

                if (next.Item1 == hint.Id && TryToShow(next.Item2, hint)) {
                    eventArgs.Shown = true;
                    return;
                }
            }

            Purge();
            foreach (var reference in Elements) {
                if (!reference.TryGetTarget(out var f)) continue;
                if (GetHint(f) == hint.Id && f.IsLoaded && TryToShow(f, hint)) {
                    eventArgs.Shown = true;
                    return;
                }
            }
        }

        private static readonly List<WeakReference<FrameworkElement>> Elements = new List<WeakReference<FrameworkElement>>(10);

        private static void Purge() {
            foreach (var toDelete in Elements.Where(x => !x.TryGetTarget(out _)).ToList()) {
                Elements.Remove(toDelete);
            }
        }

        public static bool GetHintsDecorator(DependencyObject obj) {
            return obj.GetValue(HintsDecoratorProperty) as bool? == true;
        }

        public static void SetHintsDecorator(DependencyObject obj, bool value) {
            obj.SetValue(HintsDecoratorProperty, value);
        }

        public static readonly AttachedProperty<bool> HintsDecoratorProperty = DP.RegisterAttached<bool>("HintsDecorator",
                typeof(FancyHintsService), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None));

        public static string GetHint(DependencyObject obj) {
            return (string)obj.GetValue(HintProperty);
        }

        public static void SetHint(DependencyObject obj, string value) {
            obj.SetValue(HintProperty, value);
        }

        public static readonly AttachedProperty<string> HintProperty = DP.RegisterAttached<string>("Hint",
                typeof(FancyHintsService), new UIPropertyMetadata(OnHintChanged));

        private static void OnHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is FrameworkElement u)) return;
            Purge();

            var reference = Elements.FirstOrDefault(x => x.TryGetTarget(out var f) && ReferenceEquals(f, u));
            if (GetHint(u) == null) {
                if (reference != null) {
                    Elements.Remove(reference);
                    u.Loaded -= OnElementLoaded;
                }
            } else if (reference == null) {
                Elements.Add(new WeakReference<FrameworkElement>(u));
                if (u.IsLoaded) {
                    OnElementLoaded(u, null);
                } else {
                    u.Loaded += OnElementLoaded;
                }
            }
        }

        private static void OnElementLoaded(object sender, RoutedEventArgs routedEventArgs) {
            if (!(sender is FrameworkElement e) || !GetTriggerOnLoad(e) || !e.IsVisible) return;
            var id = GetHint(e);
            _nextHint = Tuple.Create(id, e);
            Hints.FirstOrDefault(x => x.Id == id)?.Trigger();
        }

        #region Style-related
        public static HorizontalAlignment GetHorizontalAlignment(DependencyObject obj) {
            return obj.GetValue(HorizontalAlignmentProperty) as HorizontalAlignment? ?? default;
        }

        public static void SetHorizontalAlignment(DependencyObject obj, HorizontalAlignment value) {
            obj.SetValue(HorizontalAlignmentProperty, value);
        }

        public static readonly AttachedProperty<HorizontalAlignment> HorizontalAlignmentProperty = DP.RegisterAttached<HorizontalAlignment>("HorizontalAlignment",
                typeof(FancyHintsService), new FrameworkPropertyMetadata(HorizontalAlignment.Stretch, FrameworkPropertyMetadataOptions.None));

        public static VerticalAlignment GetVerticalAlignment(DependencyObject obj) {
            return obj.GetValue(VerticalAlignmentProperty) as VerticalAlignment? ?? default;
        }

        public static void SetVerticalAlignment(DependencyObject obj, VerticalAlignment value) {
            obj.SetValue(VerticalAlignmentProperty, value);
        }

        public static readonly AttachedProperty<VerticalAlignment> VerticalAlignmentProperty = DP.RegisterAttached<VerticalAlignment>("VerticalAlignment",
                typeof(FancyHintsService), new FrameworkPropertyMetadata(VerticalAlignment.Stretch, FrameworkPropertyMetadataOptions.None));

        public static HorizontalAlignment GetHorizontalContentAlignment(DependencyObject obj) {
            return obj.GetValue(HorizontalContentAlignmentProperty) as HorizontalAlignment? ?? default;
        }

        public static void SetHorizontalContentAlignment(DependencyObject obj, HorizontalAlignment value) {
            obj.SetValue(HorizontalContentAlignmentProperty, value);
        }

        public static readonly AttachedProperty<HorizontalAlignment> HorizontalContentAlignmentProperty = DP.RegisterAttached<HorizontalAlignment>("HorizontalContentAlignment", typeof(FancyHintsService),
                new FrameworkPropertyMetadata(HorizontalAlignment.Left, FrameworkPropertyMetadataOptions.None));

        public static VerticalAlignment GetVerticalContentAlignment(DependencyObject obj) {
            return obj.GetValue(VerticalContentAlignmentProperty) as VerticalAlignment? ?? default;
        }

        public static void SetVerticalContentAlignment(DependencyObject obj, VerticalAlignment value) {
            obj.SetValue(VerticalContentAlignmentProperty, value);
        }

        public static readonly AttachedProperty<VerticalAlignment> VerticalContentAlignmentProperty = DP.RegisterAttached<VerticalAlignment>("VerticalContentAlignment", typeof(FancyHintsService),
                new FrameworkPropertyMetadata(VerticalAlignment.Top, FrameworkPropertyMetadataOptions.None));

        public static double GetOffsetX(DependencyObject obj) {
            return obj.GetValue(OffsetXProperty) as double? ?? 0d;
        }

        public static void SetOffsetX(DependencyObject obj, double value) {
            obj.SetValue(OffsetXProperty, value);
        }

        public static readonly AttachedProperty<double> OffsetXProperty = DP.RegisterAttached<double>("OffsetX",
                typeof(FancyHintsService), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None));

        public static double GetOffsetY(DependencyObject obj) {
            return obj.GetValue(OffsetYProperty) as double? ?? 0d;
        }

        public static void SetOffsetY(DependencyObject obj, double value) {
            obj.SetValue(OffsetYProperty, value);
        }

        public static readonly AttachedProperty<double> OffsetYProperty = DP.RegisterAttached<double>("OffsetY",
                typeof(FancyHintsService), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.None));

        public static bool GetTriggerOnLoad(DependencyObject obj) {
            return obj.GetValue(TriggerOnLoadProperty) as bool? == true;
        }

        public static void SetTriggerOnLoad(DependencyObject obj, bool value) {
            obj.SetValue(TriggerOnLoadProperty, value);
        }

        public static readonly AttachedProperty<bool> TriggerOnLoadProperty = DP.RegisterAttached<bool>("TriggerOnLoad",
                typeof(FancyHintsService), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None));

        public static object GetAttachTo(DependencyObject obj) {
            return obj.GetValue(AttachToProperty);
        }

        public static void SetAttachTo(DependencyObject obj, object value) {
            obj.SetValue(AttachToProperty, value);
        }

        public static readonly AttachedProperty<object> AttachToProperty = DP.RegisterAttached<object>("AttachTo",
                typeof(FancyHintsService), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
        #endregion
    }
}