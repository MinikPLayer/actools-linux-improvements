using Avalonia;
using System;
using System.Windows;
using System.Windows.Controls;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Serialization;

namespace FirstFloor.ModernUI.Windows.Controls {
    public enum DoubleSliderBindingMode {
        FromTo,
        FromToFixed,
        PositionRange
    }

    public class DoubleThumb : ThumbExt {
        static DoubleThumb() {
        }


        public static readonly StyledProperty<double> RangeLeftProperty = DP.Register<double>(nameof(RangeLeft),
                typeof(DoubleThumb), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double RangeLeft {
            get => GetValue(RangeLeftProperty) as double? ?? default;
            set => SetValue(RangeLeftProperty, value);
        }

        public static readonly StyledProperty<double> RangeRightProperty = DP.Register<double>(nameof(RangeRight),
                typeof(DoubleThumb), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public double RangeRight {
            get => GetValue(RangeRightProperty) as double? ?? default;
            set => SetValue(RangeRightProperty, value);
        }

        public static readonly StyledProperty<double> RangeLeftLimitProperty = DP.Register<double>(nameof(RangeLeftLimit),
                typeof(DoubleThumb));

        public double RangeLeftLimit {
            get => GetValue(RangeLeftLimitProperty) as double? ?? default;
            set => SetValue(RangeLeftLimitProperty, value);
        }

        public static readonly StyledProperty<double> RangeRightLimitProperty = DP.Register<double>(nameof(RangeRightLimit),
                typeof(DoubleThumb));

        public double RangeRightLimit {
            get => GetValue(RangeRightLimitProperty) as double? ?? default;
            set => SetValue(RangeRightLimitProperty, value);
        }

        public static readonly StyledProperty<double> RangeLeftWidthProperty = DP.Register<double>(nameof(RangeLeftWidth),
                typeof(DoubleThumb));

        public double RangeLeftWidth {
            get => GetValue(RangeLeftWidthProperty) as double? ?? default;
            set => SetValue(RangeLeftWidthProperty, value);
        }

        public static readonly StyledProperty<double> ChangeProperty = DP.Register<double>(nameof(Change),
                typeof(DoubleThumb));

        public double Change {
            get => GetValue(ChangeProperty) as double? ?? default;
            set => SetValue(ChangeProperty, value);
        }

        public static readonly StyledProperty<double> RangeRightWidthProperty = DP.Register<double>(nameof(RangeRightWidth),
                typeof(DoubleThumb));

        public double RangeRightWidth {
            get => GetValue(RangeRightWidthProperty) as double? ?? default;
            set => SetValue(RangeRightWidthProperty, value);
        }

        // some attached stuff for styles
        public static bool GetIsLeftSubThumb(DependencyObject obj) {
            return obj.GetValue(IsLeftSubThumbProperty) as bool? ?? default;
        }

        public static void SetIsLeftSubThumb(DependencyObject obj, bool value) {
            obj.SetValue(IsLeftSubThumbProperty, value);
        }

        public static readonly AttachedProperty<bool> IsLeftSubThumbProperty = DP.RegisterAttached<bool>("IsLeftSubThumb",
                typeof(DoubleThumb), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        public static bool GetHighlightRange(DependencyObject obj) {
            return obj.GetValue(HighlightRangeProperty) as bool? ?? default;
        }

        public static void SetHighlightRange(DependencyObject obj, bool value) {
            obj.SetValue(HighlightRangeProperty, value);
        }

        public static readonly AttachedProperty<bool> HighlightRangeProperty = DP.RegisterAttached<bool>("HighlightRange",
                typeof(DoubleThumb), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    }

    public class DoubleSlider : Slider {
        static DoubleSlider() {
        }

        private bool _valueNotSet;

        public DoubleSlider() {
            _valueNotSet = true;
        }

        private readonly Busy _internalValuesBusy = new Busy();
        private readonly Busy _publicValuesBusy = new Busy();

        #region For thumb
        public static readonly StyledProperty<double> RangeLeftProperty = DP.Register<double>(nameof(RangeLeft),
                typeof(DoubleSlider), new PropertyMetadata(OnRangeLeftChanged));

        public double RangeLeft {
            get => GetValue(RangeLeftProperty) as double? ?? default;
            set => SetValue(RangeLeftProperty, value);
        }

        private static void OnRangeLeftChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((DoubleSlider)o).OnRangeLeftChanged((double)e.NewValue);
        }

        private void OnRangeLeftChanged(double newValue) {
            _internalValuesBusy.Do(() => {
                if (_bindingMode == DoubleSliderBindingMode.PositionRange || _bindingMode == DoubleSliderBindingMode.FromToFixed) {
                    RangeRight = -newValue;
                    Range = -newValue * 2d;
                } else {
                    RangeRight = -newValue;
                    Range = -newValue * 2d;
                    _publicValuesBusy.Do(() => {
                        var newFrom = Value + newValue;
                        var newSliderValue = (newFrom + _to) / 2;
                        From = newFrom;
                        Value = newSliderValue;
                        Range = (newSliderValue - newFrom) * 2;
                        ForceUpdateThumbValues();
                    });
                }
            });
        }

        public static readonly StyledProperty<double> RangeRightProperty = DP.Register<double>(nameof(RangeRight),
                typeof(DoubleSlider), new PropertyMetadata(OnRangeRightChanged));

        public double RangeRight {
            get => GetValue(RangeRightProperty) as double? ?? default;
            set => SetValue(RangeRightProperty, value);
        }

        private static void OnRangeRightChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((DoubleSlider)o).OnRangeRightChanged((double)e.NewValue);
        }

        private void OnRangeRightChanged(double newValue) {
            _internalValuesBusy.Do(() => {
                if (_bindingMode == DoubleSliderBindingMode.PositionRange || _bindingMode == DoubleSliderBindingMode.FromToFixed) {
                    RangeLeft = -newValue;
                    Range = newValue * 2d;
                } else {
                    _publicValuesBusy.Do(() => {
                        var newTo = Value + newValue;
                        var newSliderValue = (_from + newTo) / 2;
                        To = newTo;
                        Value = newSliderValue;
                        Range = (newTo - newSliderValue) * 2;
                        ForceUpdateThumbValues();
                    });
                }
            });
        }

        public static readonly DependencyPropertyKey RangeLeftLimitPropertyKey = DP.RegisterReadOnly(nameof(RangeLeftLimit), typeof(double),
                typeof(DoubleSlider), new PropertyMetadata(0d));

        public static readonly DependencyProperty RangeLeftLimitProperty = RangeLeftLimitPropertyKey.DependencyProperty;

        public double RangeLeftLimit => GetValue(RangeLeftLimitProperty) as double? ?? default;

        public static readonly DependencyPropertyKey RangeRightLimitPropertyKey = DP.RegisterReadOnly(nameof(RangeRightLimit), typeof(double),
                typeof(DoubleSlider), new PropertyMetadata(0d));

        public static readonly DependencyProperty RangeRightLimitProperty = RangeRightLimitPropertyKey.DependencyProperty;

        public double RangeRightLimit => GetValue(RangeRightLimitProperty) as double? ?? default;

        public static readonly DependencyPropertyKey RangeLeftWidthPropertyKey = DP.RegisterReadOnly(nameof(RangeLeftWidth), typeof(double),
                typeof(DoubleSlider), new PropertyMetadata(0d));

        public static readonly DependencyProperty RangeLeftWidthProperty = RangeLeftWidthPropertyKey.DependencyProperty;

        public double RangeLeftWidth => GetValue(RangeLeftWidthProperty) as double? ?? default;

        public static readonly DependencyPropertyKey RangeRightWidthPropertyKey = DP.RegisterReadOnly(nameof(RangeRightWidth), typeof(double),
                typeof(DoubleSlider), new PropertyMetadata(0d));

        public static readonly DependencyProperty RangeRightWidthProperty = RangeRightWidthPropertyKey.DependencyProperty;

        public double RangeRightWidth => GetValue(RangeRightWidthProperty) as double? ?? default;

        public static readonly StyledProperty<double> ThumbSizeProperty = DP.Register<double>(nameof(ThumbSize),
                typeof(DoubleSlider), new FrameworkPropertyMetadata(11d, (o, args) => {
                    var s = o as DoubleSlider;
                    if (s != null) {
                        SetThumbSizeDelta(s, s.ThumbSize - s.ThumbSubSize);
                    }
                }));

        public double ThumbSize {
            get => GetValue(ThumbSizeProperty) as double? ?? default;
            set => SetValue(ThumbSizeProperty, value);
        }

        public static readonly StyledProperty<double> ThumbSubSizeProperty = DP.Register<double>(nameof(ThumbSubSize),
                typeof(DoubleSlider), new FrameworkPropertyMetadata(9d, (o, args) => {
                    var s = o as DoubleSlider;
                    if (s != null) {
                        SetThumbSizeDelta(s, s.ThumbSize - s.ThumbSubSize);
                    }
                }));

        public double ThumbSubSize {
            get => GetValue(ThumbSubSizeProperty) as double? ?? default;
            set => SetValue(ThumbSubSizeProperty, value);
        }

        public static double GetThumbSizeDelta(DependencyObject obj) {
            return obj.GetValue(ThumbSizeDeltaProperty) as double? ?? default;
        }

        public static void SetThumbSizeDelta(DependencyObject obj, double value) {
            obj.SetValue(ThumbSizeDeltaProperty, value);
        }

        public static readonly AttachedProperty<double> ThumbSizeDeltaProperty = DP.RegisterAttached<double>("ThumbSizeDelta",
                typeof(DoubleSlider), new FrameworkPropertyMetadata(2d, FrameworkPropertyMetadataOptions.Inherits));


        private void ForceUpdateThumbValues() {
            var half = Range / 2d;

            var value = Value;
            var minimum = Minimum;
            var maximum = Maximum;

            var range = maximum - minimum;
            var leftLimit = value - minimum;
            var rightLimit = maximum - value;

            SetValue(RangeLeftLimitPropertyKey.DependencyProperty, -leftLimit);
            SetValue(RangeRightLimitPropertyKey.DependencyProperty, rightLimit);

            RangeLeft = -(value - half < minimum ? leftLimit : half);
            RangeRight = value + half > maximum ? rightLimit : half;

            var thumbSize = ThumbSize;
            var thumbSubSize = ThumbSubSize;
            SetValue(RangeLeftWidthPropertyKey.DependencyProperty, (Bounds.Width - thumbSize) * leftLimit / range + thumbSubSize);
            SetValue(RangeRightWidthPropertyKey.DependencyProperty, (Bounds.Width - thumbSize) * rightLimit / range + thumbSubSize);
        }

        private void UpdateThumbValues() {
            _internalValuesBusy.Do(ForceUpdateThumbValues);
        }
        #endregion

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == ValueProperty) {
                var newValue = change.GetNewValue<double>();
                _valueNotSet = false;
                _publicValuesBusy.Do(() => {
                    From = newValue - _range / 2;
                    To = newValue + _range / 2;
                });
                UpdateThumbValues();
            } else if (change.Property == MinimumProperty || change.Property == MaximumProperty || change.Property == BoundsProperty) {
                UpdateThumbValues();
            }
        }

        public static readonly StyledProperty<DoubleSliderBindingMode> BindingModeProperty = DP.Register<DoubleSliderBindingMode>(nameof(BindingMode),
                typeof(DoubleSlider), new PropertyMetadata(DoubleSliderBindingMode.FromToFixed,
                        (o, e) => { ((DoubleSlider)o)._bindingMode = (DoubleSliderBindingMode)e.NewValue; }));

        private DoubleSliderBindingMode _bindingMode = DoubleSliderBindingMode.FromToFixed;

        public DoubleSliderBindingMode BindingMode {
            get => _bindingMode;
            set => SetValue(BindingModeProperty, value);
        }

        public static readonly StyledProperty<double> RangeProperty = DP.Register<double>(nameof(Range), typeof(DoubleSlider),
                new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnRangeChanged, CoerceRangeCallback));

        private static object CoerceRangeCallback(DependencyObject d, object baseValue) {
            var r = (DoubleSlider)d;
            return Clamp(baseValue.As<double>(), 0d, (r.Maximum - r.Minimum) * 2d);
        }

        private double _range;

        public double Range {
            get => _range;
            set => SetValue(RangeProperty, value);
        }

        private static void OnRangeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((DoubleSlider)o).OnRangeChanged((double)e.NewValue);
        }

        private void OnRangeChanged(double newValue) {
            _range = newValue;
            _publicValuesBusy.Do(() => {
                From = Value - _range / 2;
                To = Value + _range / 2;
            });
            UpdateThumbValues();
        }

        private static double Clamp(double v, double min, double max) => v < min ? min : v > max ? max : v;

        public static readonly StyledProperty<double> FromProperty = DP.Register<double>(nameof(From), typeof(DoubleSlider),
                new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnFromChanged, CoerceFromCallback));

        private static object CoerceFromCallback(DependencyObject d, object baseValue) {
            var r = (DoubleSlider)d;
            return Clamp(baseValue.As<double>(), r.Minimum, r.Maximum);
        }

        private double _from = double.NaN;

        public double From {
            get => _from;
            set => SetValue(FromProperty, value);
        }

        private static void OnFromChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((DoubleSlider)o).OnFromChanged((double)e.NewValue);
        }

        private void OnFromChanged(double newValue) {
            _from = newValue;
            if (double.IsNaN(_from)) return;

            _publicValuesBusy.Do(() => {
                if (_bindingMode == DoubleSliderBindingMode.FromTo || _bindingMode == DoubleSliderBindingMode.FromToFixed || _valueNotSet) {
                    if (double.IsNaN(_to)) return;
                    Value = (_from + _to) / 2;
                    Range = Math.Max(_to - _from, 0d);
                } else {
                    var value = Value;
                    var half = Math.Max(value - _from, 0d);
                    To = Value + half;
                    Range = half * 2;
                }
            });
        }

        public static readonly StyledProperty<double> ToProperty = DP.Register<double>(nameof(To),
                typeof(DoubleSlider), new FrameworkPropertyMetadata(double.NaN, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnToChanged, CoerceToCallback));

        private static object CoerceToCallback(DependencyObject d, object baseValue) {
            var r = (DoubleSlider)d;
            return Clamp(baseValue.As<double>(), r.Minimum, r.Maximum);
        }

        private double _to = double.NaN;

        public double To {
            get => _to;
            set => SetValue(ToProperty, value);
        }

        private static void OnToChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((DoubleSlider)o).OnToChanged((double)e.NewValue);
        }

        private void OnToChanged(double newValue) {
            _to = newValue;
            if (double.IsNaN(_to)) return;

            _publicValuesBusy.Do(() => {
                if (_bindingMode == DoubleSliderBindingMode.FromTo || _bindingMode == DoubleSliderBindingMode.FromToFixed || _valueNotSet) {
                    if (double.IsNaN(_from)) return;
                    Value = (_from + _to) / 2;
                    Range = Math.Max(_to - _from, 0d);
                } else {
                    var value = Value;
                    var half = Math.Max(_to - value, 0d);
                    From = Value - half;
                    Range = half * 2;
                }
            });
        }
    }
}