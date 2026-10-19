using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FirstFloor.ModernUI.Helpers;

namespace FirstFloor.ModernUI.Windows.Controls {
    public class TimePassedBlock : TextBlock {
        public TimePassedBlock() {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private DispatcherTimer _timer;

        private void OnLoaded(object sender, RoutedEventArgs e) {
            if (_timer != null) return;
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (o, args) => {
                Update();
            });
            _timer.Start();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            if (_timer == null) return;
            _timer.Stop();
            _timer = null;
        }

        public static readonly StyledProperty<DateTime> FromProperty = DP.Register<DateTime>(nameof(From),
                typeof(TimePassedBlock), new PropertyMetadata(OnFromChanged));

        public DateTime From {
            get => GetValue(FromProperty) as DateTime? ?? default;
            set => SetValue(FromProperty, value);
        }

        private static void OnFromChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((TimePassedBlock)o).OnFromChanged((DateTime)e.NewValue);
        }

        private DateTime _dateTime;

        private void OnFromChanged(DateTime newValue) {
            _dateTime = newValue;
            this.ToolTip = newValue.ToString(CultureInfo.CurrentUICulture);
            Update();
        }

        private void Update() {
            var passed = DateTime.Now - _dateTime;
            Text = $"{passed.ToReadableTime()} ago";
        }
    }
}