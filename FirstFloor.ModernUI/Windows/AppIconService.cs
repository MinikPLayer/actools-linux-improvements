using System;
using System.Diagnostics;
using Icon = System.Drawing.Icon;
using Bitmap = Avalonia.Media.Imaging.Bitmap;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using JetBrains.Annotations;

namespace FirstFloor.ModernUI.Windows {
    public interface IAppIconProvider {
        Uri GetTrayIcon();

        Uri GetAppIcon();
    }

    public static class AppIconService {
        private static IAppIconProvider _provider;

        public static void Initialize([NotNull] IAppIconProvider provider) {
            Debug.Assert(_provider == null, @"Already initialized");
            _provider = provider;
        }

        /// <summary>
        /// Resources are addressed with avares:// URIs, plain files with file:// ones.
        /// </summary>
        [NotNull]
        private static Stream OpenStream(Uri uri) {
            if (uri.IsAbsoluteUri && uri.Scheme == "file") return File.OpenRead(uri.LocalPath);
            return AssetLoader.Open(uri);
        }

        [NotNull]
        public static Uri GetTrayIconUri() {
            return _provider.GetTrayIcon();
        }

        [NotNull]
        public static Uri GetAppIconUri() {
            return _provider.GetAppIcon();
        }

        private static Icon _trayIcon, _appIcon;
        private static Bitmap _trayImage, _appImage;

        /// <summary>
        /// System.Drawing’s icons are only used by WinForms windows (renderers, tray icon), so Windows only.
        /// </summary>
        [CanBeNull]
        public static Icon GetTrayIcon() {
            if (!OperatingSystem.IsWindows()) return null;
            return _trayIcon ?? (_trayIcon = ReadIcon(GetTrayIconUri()));
        }

        [CanBeNull]
        public static Icon GetAppIcon() {
            if (!OperatingSystem.IsWindows()) return null;
            return _appIcon ?? (_appIcon = ReadIcon(GetAppIconUri()));
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static Icon ReadIcon(Uri uri) {
            using (var stream = OpenStream(uri)) {
                return new Icon(stream);
            }
        }

        [NotNull]
        public static Bitmap GetTrayIconImage() {
            return _trayImage ?? (_trayImage = ReadImage(GetTrayIconUri()));
        }

        [NotNull]
        public static Bitmap GetAppIconImage() {
            return _appImage ?? (_appImage = ReadImage(GetAppIconUri()));
        }

        private static Bitmap ReadImage(Uri uri) {
            using (var stream = OpenStream(uri)) {
                return new Bitmap(stream);
            }
        }

        [NotNull]
        public static Avalonia.Controls.WindowIcon GetWindowIcon() {
            using (var stream = OpenStream(GetAppIconUri())) {
                return new Avalonia.Controls.WindowIcon(stream);
            }
        }
    }
}
