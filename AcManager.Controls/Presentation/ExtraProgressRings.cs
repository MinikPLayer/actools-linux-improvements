using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using AcManager.Tools.Helpers;
using AcManager.Tools.Helpers.Api;
using AcTools.Utils;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI;
using FirstFloor.ModernUI.Helpers;
using JetBrains.Annotations;

namespace AcManager.Controls.Presentation {
    public static class ExtraProgressRings {
        public static string OptionAnimationDevelopment = null;

        private static Dictionary<string, Lazy<Style>> _styles = new Dictionary<string, Lazy<Style>> { ["Default"] = null };

        [NotNull]
        public static IReadOnlyDictionary<string, Lazy<Style>> StylesLazy => _styles;

        public static event EventHandler StylesUpdated;

        public static void Initialize() {
            LoadStylesAsync();
        }

        private const string DataId = "loading_icons";

        private static void LoadStylesAsync() {
            if (OptionAnimationDevelopment != null && File.Exists(OptionAnimationDevelopment)) {
                using (var stream = File.Open(OptionAnimationDevelopment, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    _styles = ReadStyles(stream);
                }
                SimpleDirectoryWatcher.WatchFile(OptionAnimationDevelopment, () => {
                    ActionExtension.InvokeInMainThreadAsync(() => {
                        using (var stream = File.Open(OptionAnimationDevelopment, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                            _styles = ReadStyles(stream);
                        }
                    });
                });
                return;
            }

            LoadStylesImmediately();
            Task.Delay(3000).ContinueWith(t => EnsureStylesUpdated()).Ignore();
        }

        private static void LoadStylesImmediately() {
            try {
                var filename = FilesStorage.Instance.GetFilename("Static", $"{DataId}.zip");
                if (File.Exists(filename)) {
                    using (var stream = File.OpenRead(filename)) {
                        SetStylesFromArchive(stream);
                    }
                }
            } catch (Exception e) {
                Logging.Warning(e.Message);
            }
        }

        private static async Task EnsureStylesUpdated() {
            try {
                var data = await CmApiProvider.GetStaticDataBytesIfUpdatedAsync(DataId, TimeSpan.FromDays(30));
                if (data != null) {
                    using (var stream = new MemoryStream(data, false)) {
                        SetStylesFromArchive(stream);
                    }
                }
            } catch (Exception e) {
                Logging.Warning(e.Message);
            }
        }

        private static void SetStylesFromArchive(Stream stream) {
            using (var archive = new ZipArchive(stream))
            using (var entry = archive.GetEntry("ModernProgressRing.Imported.xaml")?.Open()) {
                _styles = ReadStyles(entry) ?? _styles;
            }
        }

        /// <summary>
        /// PORT: downloaded progress ring styles are WPF XAML (storyboards, VisualStateManager), they can’t be loaded by Avalonia.
        /// Only the default one is available.
        /// </summary>
        [Pure, CanBeNull]
        private static Dictionary<string, Lazy<Style>> ReadStyles([CanBeNull] Stream stream) {
            if (stream == null) return null;
            Logging.Warning("Extra progress ring styles are not supported on this platform");
            return new Dictionary<string, Lazy<Style>> { ["Default"] = null };
        }
    }
}