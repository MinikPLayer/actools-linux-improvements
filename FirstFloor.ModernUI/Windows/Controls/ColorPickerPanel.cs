using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using FirstFloor.ModernUI.Dialogs;
using FirstFloor.ModernUI.Helpers;

namespace FirstFloor.ModernUI.Windows.Controls {
    public enum ColorPickingMode {
        Hue,
        Saturation,
        Brightness,
        Red,
        Green,
        Blue
    }

    public class ColorPickerPanel : Control {
        public ColorPickerPanel() {
        }

        private Thumb _thumb;
        private Slider _slider;
        private FrameworkElement _palette;
        private FrameworkElement _originalColor;
        private FrameworkElement _pickerButton;

        protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e) {
            this.RememberTemplateScope(e);
            base.OnApplyTemplate(e);
            OriginalColor = Color;

            if (_thumb != null) {
                _thumb.DragDelta -= Thumb_DragDelta;
            }

            if (_palette != null) {
                _palette.RemoveMouseDown(Palette_MouseMove);
                _palette.RemoveMouseMove(Palette_MouseMove);
                _palette.RemoveMouseWheel(Palette_MouseWheel);
            }

            if (_originalColor != null) {
                _originalColor.RemoveMouseLeftButtonUp(OriginalColor_MouseLeftButtonUp);
            }

            if (_pickerButton != null) {
                _pickerButton.RemovePreviewMouseLeftButtonUp(PickerButton_MouseLeftButtonUp);
            }

            _thumb = this.GetTemplateChild("PART_Thumb") as Thumb;
            _slider = this.GetTemplateChild("PART_Slider") as Slider;
            _palette = this.GetTemplateChild("PART_Palette") as FrameworkElement;
            _originalColor = this.GetTemplateChild("PART_OriginalColor") as FrameworkElement;
            _pickerButton = this.GetTemplateChild("PART_PickerButton") as ModernButton;

            if (_thumb != null) {
                _thumb.DragDelta += Thumb_DragDelta;
            }

            if (_palette != null) {
                _palette.AddMouseDown(Palette_MouseMove);
                _palette.AddMouseMove(Palette_MouseMove);
                _palette.AddMouseWheel(Palette_MouseWheel);
                UpdateThumbPosition();
            }

            if (_originalColor != null) {
                _originalColor.AddMouseLeftButtonUp(OriginalColor_MouseLeftButtonUp);
            }

            if (_pickerButton != null) {
                _pickerButton.AddPreviewMouseLeftButtonUp(PickerButton_MouseLeftButtonUp);
            }
        }

        private void OriginalColor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            Color = OriginalColor;
        }

        private void PickerButton_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var picker = ColorPicker.GetLastOpened();
            if (picker?.Popup != null) {
                picker.Popup.StaysOpen = true;
            }

            Color = ScreenColorPickerDialog.Pick() ?? Color;

            if (picker?.Popup != null) {
                picker.Popup.Focus();
                picker.Popup.StaysOpen = false;
            }
        }

        private void ThumbMoved() {
            if (_palette == null) return;

            var position = Mouse.GetPosition(_palette);
            var x = position.X / _palette.ActualWidth;
            var y = 1 - position.Y / _palette.ActualHeight;

            switch (Mode) {
                case ColorPickingMode.Hue:
                    Saturation = (int)(100d * x);
                    Brightness = (int)(100d * y);
                    break;

                case ColorPickingMode.Saturation:
                    Hue = (int)(359d * x);
                    Brightness = (int)(100d * y);
                    break;

                case ColorPickingMode.Brightness:
                    Hue = (int)(359d * x);
                    Saturation = (int)(100d * y);
                    break;

                case ColorPickingMode.Red:
                    Blue = (int)(255d * x);
                    Green = (int)(255d * y);
                    break;

                case ColorPickingMode.Green:
                    Blue = (int)(255d * x);
                    Red = (int)(255d * y);
                    break;

                case ColorPickingMode.Blue:
                    Red = (int)(255d * x);
                    Green = (int)(255d * y);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            UpdateThumbPosition();
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e) {
            ThumbMoved();
        }

        private void Palette_MouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            ThumbMoved();
        }

        private void Palette_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (_slider == null) return;
            _slider.Value += e.Delta / _slider.Maximum;
        }

        private bool _skip;

        private void OnColorChanged(Color newValue) {
            if (_skip) {
                return;
            }
            _skip = true;
            UpdateRgb(newValue);
            UpdateHsb(newValue);
            _skip = false;
        }

        private void UpdateThumbPosition() {
            double x, y;

            switch (Mode) {
                case ColorPickingMode.Hue:
                    x = Saturation / 100d;
                    y = Brightness / 100d;
                    break;

                case ColorPickingMode.Saturation:
                    x = Hue / 359d;
                    y = Brightness / 100d;
                    break;

                case ColorPickingMode.Brightness:
                    x = Hue / 359d;
                    y = Saturation / 100d;
                    break;

                case ColorPickingMode.Red:
                    x = Blue / 255d;
                    y = Green / 255d;
                    break;

                case ColorPickingMode.Green:
                    x = Blue / 255d;
                    y = Red / 255d;
                    break;

                case ColorPickingMode.Blue:
                    x = Red / 255d;
                    y = Green / 255d;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            SetValue(ThumbXPropertyKey.DependencyProperty, (int)(x * 256) - 10);
            SetValue(ThumbYPropertyKey.DependencyProperty, (int)((1d - y) * 256) - 10);
            SetValue(IsBrightPropertyKey.DependencyProperty, Brightness > 50);
        }

        private void UpdateRgb(Color value) {
            Red = value.R;
            Green = value.G;
            Blue = value.B;

            UpdateThumbPosition();
        }

        private void UpdateHsb(Color value) {
            Hue = (int)value.GetHue();

            var brightness = value.GetBrightness();
            Brightness = (int)(brightness * 100);

            var saturation = value.GetSaturation();
            Saturation = (int)(saturation * 100);

            UpdateThumbPosition();
        }

        private void OnRgbChanged() {
            if (_skip) {
                return;
            }
            _skip = true;
            var color = Color.FromRgb((byte)Red, (byte)Green, (byte)Blue);
            Color = color;
            UpdateHsb(color);
            _skip = false;
        }

        private void OnHsbChanged() {
            if (_skip) {
                return;
            }
            _skip = true;
            var color = ColorExtension.FromHsb(Hue, 0.01 * Saturation, 0.01 * Brightness);
            Color = color;
            UpdateRgb(color);
            _skip = false;
        }

        #region Properties
        public static readonly StyledProperty<Color> OriginalColorProperty = DP.Register<Color>(nameof(OriginalColor), typeof(ColorPickerPanel));

        public Color OriginalColor {
            get => GetValue(OriginalColorProperty) as Color? ?? default;
            set => SetValue(OriginalColorProperty, value);
        }

        public static readonly StyledProperty<Color> ColorProperty = DP.Register<Color>(nameof(Color), typeof(ColorPickerPanel), new FrameworkPropertyMetadata(Color.FromRgb(255, 127, 0), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

        public Color Color {
            get => GetValue(ColorProperty) as Color? ?? default;
            set => SetValue(ColorProperty, value);
        }

        private static void OnColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ColorPickerPanel)o).OnColorChanged((Color)e.NewValue);
        }

        private static object ChannelCoerceValueCallback(DependencyObject dependencyObject, object baseValue) {
            return Math.Max(Math.Min((int)baseValue, 255), 0);
        }

        public static readonly StyledProperty<int> RedProperty = DP.Register<int>(nameof(Red), typeof(ColorPickerPanel), new PropertyMetadata(0, OnRgbChanged, ChannelCoerceValueCallback));

        public int Red {
            get => GetValue(RedProperty) as int? ?? default;
            set => SetValue(RedProperty, Math.Max(Math.Min(value, 255), 0));
        }

        public static readonly StyledProperty<int> GreenProperty = DP.Register<int>(nameof(Green), typeof(ColorPickerPanel), new PropertyMetadata(0, OnRgbChanged, ChannelCoerceValueCallback));

        public int Green {
            get => GetValue(GreenProperty) as int? ?? default;
            set => SetValue(GreenProperty, Math.Max(Math.Min(value, 255), 0));
        }

        public static readonly StyledProperty<int> BlueProperty = DP.Register<int>(nameof(Blue), typeof(ColorPickerPanel), new PropertyMetadata(0, OnRgbChanged, ChannelCoerceValueCallback));

        public int Blue {
            get => GetValue(BlueProperty) as int? ?? default;
            set => SetValue(BlueProperty, Math.Max(Math.Min(value, 255), 0));
        }

        private static void OnRgbChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ColorPickerPanel)o).OnRgbChanged();
        }

        public static readonly StyledProperty<int> HueProperty = DP.Register<int>(nameof(Hue), typeof(ColorPickerPanel), new PropertyMetadata(0, OnHsbChanged, HueCoerceValueCallback));

        private static object HueCoerceValueCallback(DependencyObject dependencyObject, object baseValue) {
            return Math.Max(Math.Min((int)baseValue, 359), 0);
        }

        public int Hue {
            get => GetValue(HueProperty) as int? ?? default;
            set => SetValue(HueProperty, value);
        }

        public static readonly StyledProperty<int> SaturationProperty = DP.Register<int>(nameof(Saturation), typeof(ColorPickerPanel), new PropertyMetadata(0, OnHsbChanged, SaturationCoerceValueCallback));

        private static object SaturationCoerceValueCallback(DependencyObject dependencyObject, object baseValue) {
            return Math.Max(Math.Min((int)baseValue, 100), 0);
        }

        public int Saturation {
            get => GetValue(SaturationProperty) as int? ?? default;
            set => SetValue(SaturationProperty, value);
        }

        public static readonly StyledProperty<int> BrightnessProperty = DP.Register<int>(nameof(Brightness), typeof(ColorPickerPanel), new PropertyMetadata(0, OnHsbChanged, BrightnessCoerceValueCallback));

        private static object BrightnessCoerceValueCallback(DependencyObject dependencyObject, object baseValue) {
            return Math.Max(Math.Min((int)baseValue, 100), 0);
        }

        public int Brightness {
            get => GetValue(BrightnessProperty) as int? ?? default;
            set => SetValue(BrightnessProperty, value);
        }

        private static void OnHsbChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ColorPickerPanel)o).OnHsbChanged();
        }

        public static readonly DependencyPropertyKey ThumbXPropertyKey = DP.RegisterReadOnly(nameof(ThumbX), typeof(int), typeof(ColorPickerPanel), new PropertyMetadata(0));

        public static readonly DependencyProperty ThumbXProperty = ThumbXPropertyKey.DependencyProperty;

        public int ThumbX => GetValue(ThumbXProperty) as int? ?? default;

        public static readonly DependencyPropertyKey ThumbYPropertyKey = DP.RegisterReadOnly(nameof(ThumbY), typeof(int), typeof(ColorPickerPanel), new PropertyMetadata(0));

        public static readonly DependencyProperty ThumbYProperty = ThumbYPropertyKey.DependencyProperty;

        public int ThumbY => GetValue(ThumbYProperty) as int? ?? default;

        public static readonly DependencyPropertyKey IsBrightPropertyKey = DP.RegisterReadOnly(nameof(IsBright), typeof(bool), typeof(ColorPickerPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty IsBrightProperty = IsBrightPropertyKey.DependencyProperty;

        public bool IsBright => GetValue(IsBrightProperty) as bool? ?? default;

        public static readonly StyledProperty<ColorPickingMode> ModeProperty = DP.Register<ColorPickingMode>(nameof(Mode),
                typeof(ColorPickerPanel), new PropertyMetadata(OnModeChanged));

        public ColorPickingMode Mode {
            get => GetValue(ModeProperty) as ColorPickingMode? ?? default;
            set => SetValue(ModeProperty, value);
        }

        private static void OnModeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((ColorPickerPanel)o).UpdateThumbPosition();
        }
        #endregion
    }
}