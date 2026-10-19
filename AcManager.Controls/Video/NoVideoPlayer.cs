using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Media;
using AcManager.Tools.Helpers;

namespace AcManager.Controls.Video {
    /// <summary>
    /// PORT: WPF MediaElement and VLC (xZune.Vlc) players are Windows-only, there is no embedded video playback yet. Playing a video
    /// opens it with the default application, the animated background is just not shown.
    /// </summary>
    internal class NoVideoPlayer : IVideoPlayer {
        public Avalonia.Controls.Control Initialize() => new Border();

        public Uri Source { get; set; }
        public Stretch Stretch { get; set; }
        public bool IsLooped { get; set; }
        public double Volume { get; set; }

        public void Play() {
            if (Source != null && File.Exists(Source.OriginalString)) {
                WindowsHelper.OpenFile(Source.OriginalString);
            }
        }

        public void PlayRandomly() { }
        public void Pause() { }
        public void Stop() { }

        public event EventHandler Started { add { } remove { } }
        public event EventHandler Ended { add { } remove { } }

        public void Dispose() { }
    }
}
