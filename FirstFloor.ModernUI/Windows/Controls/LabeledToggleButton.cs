using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FirstFloor.ModernUI.Windows.Attached;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class LabeledToggleButton : ToggleButton {
        public LabeledToggleButton() {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private bool _loaded;

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs) {
            if (!_loaded) return;
            _loaded = false;
            CompositionTargetEx.Rendering -= OnRendering;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (_loaded) return;
            _loaded = true;
        }

        public static readonly StyledProperty<bool> HighlightCheckedProperty = DP.Register<bool>(nameof(HighlightChecked),
                typeof(LabeledToggleButton));

        public bool HighlightChecked {
            get => GetValue(HighlightCheckedProperty) as bool? == true;
            set => SetValue(HighlightCheckedProperty, value);
        }

        public static readonly StyledProperty<bool> HighlightUncheckedProperty = DP.Register<bool>(nameof(HighlightUnchecked),
                typeof(LabeledToggleButton));

        public bool HighlightUnchecked {
            get => GetValue(HighlightUncheckedProperty) as bool? == true;
            set => SetValue(HighlightUncheckedProperty, value);
        }

        public static readonly StyledProperty<double> AnimationSpeedProperty = DP.Register<double>(nameof(AnimationSpeed),
                typeof(LabeledToggleButton), new PropertyMetadata(150d));

        public double AnimationSpeed {
            get => GetValue(AnimationSpeedProperty) as double? ?? 0d;
            set => SetValue(AnimationSpeedProperty, value);
        }

        private DateTime _previous;

        private void OnRendering(object sender, RenderingEventArgs e) {
            if (!_animateTo2.HasValue) return;

            var delta = (DateTime.Now - _previous).TotalMilliseconds / AnimationSpeed;
            _previous = DateTime.Now;
            SetPosition(_animateTo2.Value < _position ? _position - delta : _position + delta);
        }

        private void AnimateTo(double value) {
            _previous = DateTime.Now;
            SetAnimateTo(value);
        }

        private double? _animateTo2;
        private double _position;

        private void SetAnimateTo(double? value) {
            if (Equals(_animateTo2, value)) return;

            if (_animateTo2 == null) {
                CompositionTargetEx.Rendering += OnRendering;
            } else if (value == null) {
                CompositionTargetEx.Rendering -= OnRendering;
            }

            _animateTo2 = value;
        }

        private FrameworkElement _labelOn, _labelOff, _helper;
        private Thumb _thumb;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            if (_thumb != null) {
                _thumb.DragDelta -= Thumb_DragDelta;
                _thumb.DragCompleted -= Thumb_DragCompleted;
            }

            base.OnApplyTemplate(e);

            _labelOn = this.GetTemplateChild("PART_OnLabel") as FrameworkElement;
            _labelOff = this.GetTemplateChild("PART_OffLabel") as FrameworkElement;
            _helper = this.GetTemplateChild("PART_Helper") as FrameworkElement;
            _thumb = this.GetTemplateChild("PART_Thumb") as Thumb;

            if (_thumb != null) {
                _thumb.DragDelta += Thumb_DragDelta;
                _thumb.DragCompleted += Thumb_DragCompleted;
            }

            if (!Equals(_position, 0d)) {
                SetPosition(_position);
            }
        }

        protected override void OnUnchecked(RoutedEventArgs e) {
            base.OnUnchecked(e);
            AnimateTo(0d);
        }

        protected override void OnChecked(RoutedEventArgs e) {
            base.OnChecked(e);

            if (IsLoaded) {
                AnimateTo(1d);
            } else {
                SetPosition(1d);
            }
        }

        private void SetPosition(double pos) {
            if (pos < 0d) pos = 0d;
            if (pos > 1d) pos = 1d;

            if (_animateTo2.HasValue && Math.Abs(_animateTo2.Value - pos) < 0.0001) {
                SetAnimateTo(null);
            }

            _position = pos;
            if (_thumb != null) {
                RelativeTranslateTransform.SetX(_thumb, pos);
                RelativeTranslateTransform.SetX(_labelOff, pos);
                RelativeTranslateTransform.SetX(_labelOn, pos - 1d);
            }
        }

        private double _lastChange;

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e) {
            SetAnimateTo(null);
            _lastChange = e.HorizontalChange / _helper.ActualWidth;
            SetPosition(_position + _lastChange);
        }

        private void Thumb_DragCompleted(object sender, DragCompletedEventArgs e) {
            var next = _position + _lastChange * (5 + _helper.ActualWidth / 50d);
            if (IsChecked == next > 0.5d) {
                AnimateTo(IsChecked == true ? 1d : 0d);
            } else {
                IsChecked = !IsChecked;
            }
        }

        public static readonly StyledProperty<string> LabelUncheckedProperty = DP.Register<string>(nameof(LabelUnchecked),
                typeof(LabeledToggleButton));

        public string LabelUnchecked {
            get => (string)GetValue(LabelUncheckedProperty);
            set => SetValue(LabelUncheckedProperty, value);
        }

        public static readonly StyledProperty<string> LabelCheckedProperty = DP.Register<string>(nameof(LabelChecked),
                typeof(LabeledToggleButton));

        public string LabelChecked {
            get => (string)GetValue(LabelCheckedProperty);
            set => SetValue(LabelCheckedProperty, value);
        }
    }
}