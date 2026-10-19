using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using FirstFloor.ModernUI.Dialogs;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Serialization;
using FirstFloor.ModernUI.Windows.Media;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows.Controls {
    /// <summary>
    /// Window with some extra features: remembers its location and size, dims its owner when shown as a dialog,
    /// keeps track of the last active window, has a synchronous ShowDialog() (as in WPF).
    /// </summary>
    /// <remarks>
    /// DPI handling is done by Avalonia, so everything related to it from the WPF version is gone.
    /// </remarks>
    public abstract class DpiAwareWindow : Window {
        public static bool OptionVerboseMode;

        public static event EventHandler NewWindowCreated;

        protected DpiAwareWindow() {
            Keyboard.Hook();

            Opened += OnOpened;
            Activated += OnActivated;
            PositionChanged += OnPositionChanged;
            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            Owner = GetDefaultOwner(true);
            NewWindowCreated?.Invoke(this, EventArgs.Empty);
        }

        #region Owner (settable, unlike in Avalonia)
        private Window _owner;

        public new Window Owner {
            get => _owner;
            set => _owner = value;
        }
        #endregion

        #region Keep track of the last active window to make sure new ones are shown where needed
        [CanBeNull]
        public static DpiAwareWindow LastActiveWindow { get; private set; }

        public DateTime LastActivated { get; private set; }

        private void OnActivated(object sender, EventArgs e) {
            LastActiveWindow = this;
            LastActivated = DateTime.Now;
            OnActivatedOverride();
        }
        #endregion

        #region Dimming
        private DpiAwareWindow _dimmedOwner;

        private void DimOwner() {
            UndimOwner();

            _dimmedOwner = Owner as DpiAwareWindow;
            if (_dimmedOwner?.IsDimmed == false) {
                _dimmedOwner.IsDimmed = true;
            } else {
                _dimmedOwner = null;
            }
        }

        private void UndimOwner() {
            if (_dimmedOwner != null) {
                _dimmedOwner.IsDimmed = false;
                _dimmedOwner = null;
            }
        }

        public static readonly StyledProperty<bool> IsDimmedProperty = DP.Register<bool>(nameof(IsDimmed), typeof(DpiAwareWindow));

        public bool IsDimmed {
            get => GetValue(IsDimmedProperty) as bool? == true;
            set => SetValue(IsDimmedProperty, value);
        }
        #endregion

        #region Various show-related stuff, including improved ShowDialog() method
        private static IEnumerable<Window> GetWindows() {
            return (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Windows
                    ?? (IReadOnlyList<Window>)Array.Empty<Window>();
        }

        private static Window GetMainWindow() {
            return (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        }

        private Window GetDefaultOwner(bool allowCurrent) {
            var main = GetMainWindow();
            return main == null || ReferenceEquals(main, this) ? null
                    : GetWindows().OfType<DpiAwareWindow>().Where(x => allowCurrent || !ReferenceEquals(x, Owner)).FirstOrDefault(x => x.IsActive)
                            ?? (main.IsVisible ? main : null);
        }

        public static readonly StyledProperty<bool> DoNotAttachToWaitingDialogsProperty = DP.Register<bool>(nameof(DoNotAttachToWaitingDialogs),
                typeof(DpiAwareWindow), new PropertyMetadata(false, (o, e) => { ((DpiAwareWindow)o)._doNotAttachToWaitingDialogs = (bool)e.NewValue; }));

        private bool _doNotAttachToWaitingDialogs;

        public bool DoNotAttachToWaitingDialogs {
            get => _doNotAttachToWaitingDialogs;
            set => SetValue(DoNotAttachToWaitingDialogsProperty, value);
        }

        public static readonly DependencyPropertyKey ShownAsDialogPropertyKey = DP.RegisterReadOnly(nameof(ShownAsDialog), typeof(bool),
                typeof(DpiAwareWindow), new PropertyMetadata(false));

        public static readonly DependencyProperty ShownAsDialogProperty = ShownAsDialogPropertyKey.DependencyProperty;

        public bool ShownAsDialog => (bool)GetValue(ShownAsDialogProperty);

        protected bool DoNotAffectOwner;

        /// <summary>
        /// Result which will be returned by ShowDialog().
        /// </summary>
        public bool? DialogResult {
            get => _dialogResult;
            set {
                _dialogResult = value;
                if (ShownAsDialog) {
                    Close();
                }
            }
        }

        private bool? _dialogResult;

        /// <summary>
        /// Synchronous modal dialog, just like in WPF: runs a nested message loop until window is closed.
        /// </summary>
        public new bool? ShowDialog() {
            if (!DoNotAffectOwner) {
                var doNotAttachToWaitingDialogs = DoNotAttachToWaitingDialogs;
                if (Owner != null && (!Owner.IsVisible || doNotAttachToWaitingDialogs && Owner is WaitingDialog)) {
                    var defaultOwner = GetDefaultOwner(false);
                    Owner = doNotAttachToWaitingDialogs && defaultOwner is WaitingDialog ? null : defaultOwner;
                }
            }

            DimOwner();

            if (Owner == null || !Owner.IsVisible) {
                ShowInTaskbar = true;
            }

            try {
                SetValue(ShownAsDialogPropertyKey.DependencyProperty, true);
                _dialogResult = null;

                var cancellation = new CancellationTokenSource();
                Task task;
                if (Owner != null) {
                    task = base.ShowDialog(Owner);
                } else {
                    Show();
                    var completion = new TaskCompletionSource<bool>();
                    Closed += (s, a) => completion.TrySetResult(true);
                    task = completion.Task;
                }

                task.ContinueWith(_ => cancellation.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
                if (!task.IsCompleted) {
                    Dispatcher.UIThread.MainLoop(cancellation.Token);
                }

                return _dialogResult;
            } finally {
                SetValue(ShownAsDialogPropertyKey.DependencyProperty, false);
                UndimOwner();
            }
        }

        public Task<bool?> ShowDialogAsync() {
            var completion = new TaskCompletionSource<bool?>();
            Dispatcher.UIThread.Post(() => completion.SetResult(ShowDialog()));
            return completion.Task;
        }

        public Task ShowAndWaitAsync() {
            var task = new TaskCompletionSource<object>();
            Closed += (s, a) => task.SetResult(null);
            Show();
            Focus();
            return task.Task;
        }

        protected sealed override void OnClosing(WindowClosingEventArgs e) {
            if (e.Cancel) return;
            OnClosingOverride(e);
            if (e.Cancel) return;

            base.OnClosing(e);
            if (e.Cancel) return;

            try {
                _closed = true;
                if (IsActive) {
                    Owner?.Activate();
                }
            } catch (Exception ex) {
                Helpers.Logging.Error(ex);
            }
        }

        protected sealed override void OnClosed(EventArgs e) {
            _closed = true;
            UndimOwner();
            base.OnClosed(e);
            OnClosedOverride();
        }

        public new void Show() {
            if (Owner != null && Owner.IsVisible && !ReferenceEquals(Owner, this)) {
                base.Show(Owner);
            } else {
                base.Show();
            }
        }
        #endregion

        #region Safer implementation of the Close() method
        private bool _closed;

        public bool IsClosed() {
            return _closed;
        }

        public new void Close() {
            try {
                base.Close();
            } catch (InvalidOperationException e) {
                Helpers.Logging.Error(e.Message);
            } catch (Exception e) {
                Helpers.Logging.Error(e);
            }
        }
        #endregion

        #region Some ready-to-override methods to decrease the amount of self-listeners
        protected virtual void OnActivatedOverride() { }
        protected virtual void OnLoadedOverride() { }
        protected virtual void OnUnloadedOverride() { }
        protected virtual void OnLocationChangedOverride() { }
        protected virtual void OnSizeChangedOverride(SizeChangedEventArgs e) { }
        protected virtual void OnStateChangedOverride() { }
        protected virtual void OnClosingOverride(WindowClosingEventArgs e) { }
        protected virtual void OnClosedOverride() { }
        protected virtual void OnInitializedOverride() { }
        protected virtual void OnSourceInitializedOverride() { }

        protected override void OnInitialized() {
            base.OnInitialized();
            OnInitializedOverride();
        }

        private void OnOpened(object sender, EventArgs e) {
            LoadLocationAndSize();
            OnSourceInitializedOverride();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            _shownAndReady = true;
            OnLoadedOverride();
        }

        private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs) {
            OnUnloadedOverride();
        }

        protected override void OnPropertyChanged(Avalonia.AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);
            if (change.Property == WindowStateProperty) {
                UpdateActualLocation();
                SaveLocationAndSize();
                OnStateChangedOverride();
            }
        }
        #endregion

        #region Methods to save state
        private void OnPositionChanged(object sender, PixelPointEventArgs e) {
            UpdateActualLocation();
            SaveLocationAndSize();
            OnLocationChangedOverride();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateActualLocation();
            SaveLocationAndSize();
            OnSizeChangedOverride(e);
        }
        #endregion

        #region Actual location
        public static readonly DependencyPropertyKey ActualLeftPropertyKey = DP.RegisterReadOnly(nameof(ActualLeft), typeof(double), typeof(DpiAwareWindow), new PropertyMetadata(0d));
        public static readonly DependencyProperty ActualLeftProperty = ActualLeftPropertyKey.DependencyProperty;
        public double ActualLeft => GetValue(ActualLeftProperty) as double? ?? 0d;

        public static readonly DependencyPropertyKey ActualTopPropertyKey = DP.RegisterReadOnly(nameof(ActualTop), typeof(double), typeof(DpiAwareWindow), new PropertyMetadata(0d));
        public static readonly DependencyProperty ActualTopProperty = ActualTopPropertyKey.DependencyProperty;
        public double ActualTop => GetValue(ActualTopProperty) as double? ?? 0d;

        public static readonly DependencyPropertyKey ActualRightPropertyKey = DP.RegisterReadOnly(nameof(ActualRight), typeof(double), typeof(DpiAwareWindow), new PropertyMetadata(0d));
        public static readonly DependencyProperty ActualRightProperty = ActualRightPropertyKey.DependencyProperty;
        public double ActualRight => GetValue(ActualRightProperty) as double? ?? 0d;

        public static readonly DependencyPropertyKey ActualBottomPropertyKey = DP.RegisterReadOnly(nameof(ActualBottom), typeof(double), typeof(DpiAwareWindow), new PropertyMetadata(0d));
        public static readonly DependencyProperty ActualBottomProperty = ActualBottomPropertyKey.DependencyProperty;
        public double ActualBottom => GetValue(ActualBottomProperty) as double? ?? 0d;

        private void UpdateActualLocation() {
            var scale = RenderScaling;
            var left = Position.X / scale;
            var top = Position.Y / scale;
            SetValue(ActualLeftPropertyKey.DependencyProperty, left);
            SetValue(ActualTopPropertyKey.DependencyProperty, top);
            SetValue(ActualRightPropertyKey.DependencyProperty, left + Bounds.Width);
            SetValue(ActualBottomPropertyKey.DependencyProperty, top + Bounds.Height);
        }
        #endregion

        #region Location and size
        private static readonly List<IDisposable> DisableAnyLogicList = new List<IDisposable>();

        private static bool IsAnyLogicDisabled() {
            return DisableAnyLogicList.Count > 0;
        }

        public static IDisposable DisableAnyLogic() {
            IDisposable[] instance = { null };
            instance[0] = new ActionAsDisposable(() => {
                if (DisableAnyLogicList.Remove(instance[0]) && !IsAnyLogicDisabled()) {
                    foreach (var window in GetWindows().OfType<DpiAwareWindow>()) {
                        window.LoadLocationAndSize();
                    }
                }
            });
            DisableAnyLogicList.Add(instance[0]);
            return instance[0];
        }

        protected class ActionAsDisposable : IDisposable {
            private readonly Action _action;

            public ActionAsDisposable(Action action) {
                _action = action;
            }

            public void Dispose() {
                _action.Invoke();
            }
        }

        public static readonly StyledProperty<string> LocationAndSizeKeyProperty = DP.Register<string>(nameof(LocationAndSizeKey),
                typeof(DpiAwareWindow), new PropertyMetadata(null, (o, e) => { ((DpiAwareWindow)o)._locationAndSizeKey = (string)e.NewValue; }));

        private string _locationAndSizeKey;

        [Localizable(false)]
        public string LocationAndSizeKey {
            get => _locationAndSizeKey;
            set => SetValue(LocationAndSizeKeyProperty, value);
        }

        private class Placement {
            public int Left, Top, Width, Height;
            public double ScaleX = 1d, ScaleY = 1d;
            public bool IsMaximized;

            public static Placement FromString(string s) {
                return s.Split(';').Select(l => l.Split(new[] { '=' }, 2)).Aggregate(new Placement(), (r, a) => {
                    var f = a.Length == 2 ? r.GetType().GetField(a[0].Trim()) : null;
                    f?.SetValue(r, a[1].As(f.FieldType, f.GetValue(r)));
                    return r;
                });
            }

            public override string ToString() {
                return string.Join(@";", GetType().GetFields().Select(p => p.Name + '=' + p.GetValue(this).As<string>()));
            }
        }

        static DpiAwareWindow() {
            SimpleSerialization.Register(Placement.FromString);
        }

        public static readonly StyledProperty<bool> ConsiderPreferredFullscreenProperty = DP.Register<bool>(nameof(ConsiderPreferredFullscreen),
                typeof(DpiAwareWindow), new PropertyMetadata(true, (o, e) => { ((DpiAwareWindow)o)._considerPreferredFullscreen = (bool)e.NewValue; }));

        private bool _considerPreferredFullscreen = true;

        public bool ConsiderPreferredFullscreen {
            get => _considerPreferredFullscreen;
            set => SetValue(ConsiderPreferredFullscreenProperty, value);
        }

        private bool _shownAndReady, _loadingLocationAndSize;

        private void LoadLocationAndSize() {
            if (!AppearanceManager.Instance.ManageWindowsLocation || IsAnyLogicDisabled()) return;

            _loadingLocationAndSize = true;
            try {
                var key = LocationAndSizeKey;
                if (key == null || AppearanceManager.Instance.PreferFullscreenMode && ConsiderPreferredFullscreen) {
                    SetDefaultLocation();
                    return;
                }

                var placement = ValuesStorage.Get<string>(key).As<Placement>();
                if (placement == null || placement.Width <= 0 || placement.Height <= 0) {
                    SetDefaultLocation();
                    return;
                }

                WindowState = WindowState.Normal;
                var scale = RenderScaling;
                var screen = Screens?.ScreenFromPoint(new PixelPoint(placement.Left + 10, placement.Top + 10)) ?? Screens?.Primary;
                var area = screen?.WorkingArea ?? new PixelRect(0, 0, 1920, 1080);

                Width = Math.Min(placement.Width, area.Width / scale);
                Height = Math.Min(placement.Height, area.Height / scale);

                var x = Math.Max(area.X, Math.Min(placement.Left, area.Right - (int)(Width * scale)));
                var y = Math.Max(area.Y, Math.Min(placement.Top, area.Bottom - (int)(Height * scale)));
                Position = new PixelPoint(x, y);

                if (placement.IsMaximized) {
                    WindowState = WindowState.Maximized;
                }
            } catch (Exception e) {
                Helpers.Logging.Error(e);
            } finally {
                _loadingLocationAndSize = false;
            }
        }

        private void SetDefaultLocation() {
            var area = (Screens?.ScreenFromWindow(this) ?? Screens?.Primary)?.WorkingArea;
            if (area == null) return;

            var scale = RenderScaling;
            if (Bounds.Width > area.Value.Width / scale) {
                Width = area.Value.Width / scale;
            }

            if (Bounds.Height > area.Value.Height / scale) {
                Height = area.Value.Height / scale;
            }
        }

        private void SaveLocationAndSize() {
            if (!_shownAndReady || _loadingLocationAndSize || IsAnyLogicDisabled() || LocationAndSizeKey == null) return;
            if (!AppearanceManager.Instance.ManageWindowsLocation || WindowState == WindowState.Minimized) return;

            try {
                var maximized = WindowState == WindowState.Maximized;
                var previous = maximized ? ValuesStorage.Get<string>(LocationAndSizeKey).As<Placement>() : null;
                var scale = RenderScaling;
                var placement = maximized && previous != null ? previous : new Placement {
                    Left = Position.X,
                    Top = Position.Y,
                    Width = (int)Bounds.Width,
                    Height = (int)Bounds.Height,
                    ScaleX = 1d,
                    ScaleY = 1d,
                };

                placement.IsMaximized = maximized;
                ValuesStorage.Set(LocationAndSizeKey, placement.ToString());
            } catch (Exception e) {
                Helpers.Logging.Error(e);
            }
        }

        public static readonly StyledProperty<bool> PreferredFullscreenModeProperty = DP.Register<bool>(nameof(PreferredFullscreenMode), typeof(DpiAwareWindow),
                new PropertyMetadata(false));

        public bool PreferredFullscreenMode => GetValue(PreferredFullscreenModeProperty);

        public void UpdatePreferredFullscreenMode(bool maximized = false, bool centerAlign = false) { }

        public void EnsureOnScreen() {
            var area = (Screens?.ScreenFromWindow(this) ?? Screens?.Primary)?.WorkingArea;
            if (area == null) return;

            var scale = RenderScaling;
            var x = Math.Max(area.Value.X, Math.Min(Position.X, area.Value.Right - (int)(Bounds.Width * scale)));
            var y = Math.Max(area.Value.Y, Math.Min(Position.Y, area.Value.Bottom - (int)(Bounds.Height * scale)));
            if (x != Position.X || y != Position.Y) {
                Position = new PixelPoint(x, y);
            }
        }
        #endregion

        #region Things which were handled by Windows-specific code in WPF version
        public static readonly StyledProperty<bool> BlurBackgroundProperty = DP.Register<bool>(nameof(BlurBackground), typeof(DpiAwareWindow),
                new PropertyMetadata(false, (o, e) => {
                    ((DpiAwareWindow)o).TransparencyLevelHint = (bool)e.NewValue
                            ? new[] { WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.None }
                            : new[] { WindowTransparencyLevel.None };
                }));

        public bool BlurBackground {
            get => GetValue(BlurBackgroundProperty) as bool? == true;
            set => SetValue(BlurBackgroundProperty, value);
        }

        public static readonly StyledProperty<bool> PreventActivationProperty = DP.Register<bool>(nameof(PreventActivation), typeof(DpiAwareWindow));

        public bool PreventActivation {
            get => GetValue(PreventActivationProperty) as bool? == true;
            set => SetValue(PreventActivationProperty, value);
        }

        public static readonly StyledProperty<bool> ToolWindowProperty = DP.Register<bool>(nameof(ToolWindow), typeof(DpiAwareWindow),
                new PropertyMetadata(false, (o, e) => { ((DpiAwareWindow)o).ShowInTaskbar = !(bool)e.NewValue; }));

        public bool ToolWindow {
            get => GetValue(ToolWindowProperty) as bool? == true;
            set => SetValue(ToolWindowProperty, value);
        }

        /// <summary>
        /// Scale of UI is set by user, DPI scaling is handled by Avalonia.
        /// </summary>
        public double ScaleX => 1d;

        public double ScaleY => 1d;

        public void SetAppScale(double value) { }

        public void UpdateTextFormatting() { }

        public static readonly DependencyPropertyKey IconPathThicknessPropertyKey = DP.RegisterReadOnly(nameof(IconPathThickness), typeof(double),
                typeof(DpiAwareWindow), new PropertyMetadata(1d));

        public static readonly DependencyProperty IconPathThicknessProperty = IconPathThicknessPropertyKey.DependencyProperty;
        public double IconPathThickness => GetValue(IconPathThicknessProperty) as double? ?? 0d;

        public static readonly double UnlimitedSize = 99999d;

        public double DeviceScaleX => RenderScaling;
        public double DeviceScaleY => RenderScaling;
        public double DeviceLeft => Position.X;
        public double DeviceTop => Position.Y;
        public double DeviceWidth => Bounds.Width * RenderScaling;
        public double DeviceHeight => Bounds.Height * RenderScaling;
        #endregion

        public void BringToFront() {
            if (!IsVisible) {
                Show();
            }

            if (WindowState == WindowState.Minimized) {
                WindowState = WindowState.Normal;
            }

            Topmost = true;
            Topmost = false;
            Activate();
            Focus();
        }

        public async void ShowInvisible() {
            var needToShowInTaskbar = ShowInTaskbar;
            var initialWindowState = WindowState;
            var hitTestVisible = IsHitTestVisible;

            try {
                ShowInTaskbar = false;
                IsHitTestVisible = false;
                Opacity = 0.01d;

                Show();
                await Task.Yield();
                Hide();
                Opacity = 1d;
            } finally {
                ShowInTaskbar = needToShowInTaskbar;
                WindowState = initialWindowState;
                IsHitTestVisible = hitTestVisible;
            }
        }
    }
}
