using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AcManager.Tools.Managers.Plugins;
using FirstFloor.ModernUI.Helpers;

namespace AcManager.Controls.Video {
    public class VideoPlayer : Border, IDisposable {
        private readonly IVideoPlayer _player;

        public VideoPlayer(bool useSystemPlayer) {
            _player = !useSystemPlayer && PluginsManager.Instance.IsPluginEnabled("VLC") ? (IVideoPlayer)new VlcPlayer() : new WindowsMediaPlayer();

            _player.Stretch = Stretch;
            _player.IsLooped = IsLooped;
            _player.Volume = Volume;

            _player.Started += Player_Start;
            _player.Ended += Player_End;

            Loaded += (sender, args) => {
                if (AutoPlay) {
                    _player.Play();
                }
            };
        }

        public VideoPlayer() : this(false) {}

        protected override void OnVisualParentChanged(DependencyObject oldParent) {
            base.OnVisualParentChanged(oldParent);
            Initialize();
        }

        private void Initialize() {
            if (Child == null) {
                Child = _player.Initialize();
            }
        }

        public event EventHandler Started;

        public event EventHandler Ended;

        private void Player_Start(object sender, EventArgs e) {
            Started?.Invoke(this, e);
        }

        private void Player_End(object sender, EventArgs e) {
            Ended?.Invoke(this, e);
        }

        public void Play() {
            Initialize();
            _player.Play();
        }

        public void PlayRandomly() {
            Initialize();
            _player.PlayRandomly();
        }

        public void Stop() {
            _player.Stop();
        }

        public void Pause() {
            _player.Pause();
        }

        public static readonly StyledProperty<bool> AutoPlayProperty = DP.Register<bool>(nameof(AutoPlay),
                typeof(VideoPlayer));

        public bool AutoPlay {
            get => GetValue(AutoPlayProperty) as bool? == true;
            set => SetValue(AutoPlayProperty, value);
        }

        public static readonly StyledProperty<Stretch> StretchProperty = DP.Register<Stretch>(nameof(Stretch), typeof(VideoPlayer),
                new PropertyMetadata(Stretch.Uniform, OnStretchChanged));

        public Stretch Stretch {
            get => GetValue(StretchProperty) as Stretch? ?? default;
            set => SetValue(StretchProperty, value);
        }

        private static void OnStretchChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((VideoPlayer)o).OnStretchChanged((Stretch)e.NewValue);
        }

        private void OnStretchChanged(Stretch newValue) {
            _player.Stretch = newValue;
        }

        public static readonly StyledProperty<Uri> SourceProperty = DP.Register<Uri>(nameof(Source), typeof(VideoPlayer),
                new PropertyMetadata(OnSourceChanged));

        public Uri Source {
            get => (Uri)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private static void OnSourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((VideoPlayer)o).OnSourceChanged((Uri)e.NewValue);
        }

        private void OnSourceChanged(Uri newValue) {
            _player.Source = newValue == null || !File.Exists(newValue.OriginalString) ? null : newValue;
        }

        public static readonly StyledProperty<bool> IsLoopedProperty = DP.Register<bool>(nameof(IsLooped), typeof(VideoPlayer),
                new PropertyMetadata(false, OnIsLoopedChanged));

        public bool IsLooped {
            get => GetValue(IsLoopedProperty) as bool? == true;
            set => SetValue(IsLoopedProperty, value);
        }

        private static void OnIsLoopedChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((VideoPlayer)o).OnIsLoopedChanged((bool)e.NewValue);
        }

        private void OnIsLoopedChanged(bool newValue) {
            _player.IsLooped = newValue;
        }

        public static readonly StyledProperty<double> VolumeProperty = DP.Register<double>(nameof(Volume), typeof(VideoPlayer),
                new PropertyMetadata(1d, OnVolumeChanged));

        public double Volume {
            get => GetValue(VolumeProperty) as double? ?? 0d;
            set => SetValue(VolumeProperty, value);
        }

        private static void OnVolumeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e) {
            ((VideoPlayer)o).OnVolumeChanged((double)e.NewValue);
        }

        private void OnVolumeChanged(double newValue) {
            _player.Volume = newValue;
        }
        public void Dispose() {
            try {
                _player.Dispose();
            } catch (Exception ex) {
                Logging.Write(ex);
            }
        }
    }

    internal interface IVideoPlayer : IDisposable {
        UIElement Initialize();

        Uri Source { get; set; }
        Stretch Stretch { get; set; }

        bool IsLooped { get; set; }
        double Volume { get; set; }

        void Play();
        void PlayRandomly();
        void Pause();
        void Stop();

        event EventHandler Started;
        event EventHandler Ended;
    }
}