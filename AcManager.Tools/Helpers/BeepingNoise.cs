using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AcManager.Tools.Helpers.Api;
using AcTools.Utils.Helpers;
using FirstFloor.ModernUI.Helpers;
using SharpCompress.Archives.Zip;

namespace AcManager.Tools.Helpers {
    public enum BeepingNoiseType {
        [Description("Disabled")]
        Disabled = 0,

        [Description("System")]
        System = 1,

        [Description("Custom")]
        Custom = 2
    }

    public static class BeepingNoise {
        [DllImport("winmm.dll", EntryPoint = "PlaySoundW")]
        private static extern bool PlaySound(byte[] data, IntPtr hmod, uint flags);

        [DllImport("user32.dll")]
        private static extern bool MessageBeep(uint type);

        private static void RunLinuxPlayer(string filename) {
            foreach (var player in new[] { "paplay", "aplay", "play" }) {
                try {
                    var info = new ProcessStartInfo(player) { UseShellExecute = false, CreateNoWindow = true };
                    info.ArgumentList.Add(filename);
                    using (var process = Process.Start(info)) {
                        process?.WaitForExit(10000);
                        return;
                    }
                } catch (Exception) {
                    // try the next one
                }
            }

            Logging.Warning("No sound player found (tried paplay, aplay, play)");
        }

        private static void PlayWav(byte[] wavBytes) {
            Task.Run(() => {
                try {
                    if (OperatingSystem.IsWindows()) {
                        // SND_MEMORY | SND_SYNC
                        PlaySound(wavBytes, IntPtr.Zero, 0x0004);
                    } else {
                        var temp = Path.Combine(Path.GetTempPath(), $"cm-beep-{Guid.NewGuid():N}.wav");
                        File.WriteAllBytes(temp, wavBytes);
                        try {
                            RunLinuxPlayer(temp);
                        } finally {
                            File.Delete(temp);
                        }
                    }
                } catch (Exception e) {
                    Logging.Warning(e);
                }
            });
        }

        private static void PlaySystemSound() {
            if (OperatingSystem.IsWindows()) {
                MessageBeep(0x30);
            } else {
                Task.Run(() => {
                    foreach (var file in new[] {
                        "/usr/share/sounds/freedesktop/stereo/dialog-warning.oga",
                        "/usr/share/sounds/freedesktop/stereo/bell.oga"
                    }) {
                        if (File.Exists(file)) {
                            RunLinuxPlayer(file);
                            return;
                        }
                    }

                    Console.Write('\a');
                });
            }
        }

        public static async Task Play(BeepingNoiseType type) {
            switch (type) {
                case BeepingNoiseType.Disabled:
                    break;
                case BeepingNoiseType.System:
                    PlaySystemSound();
                    break;
                case BeepingNoiseType.Custom:
                    try {
                        var data = await CmApiProvider.GetStaticDataBytesAsync("audio_error", TimeSpan.FromDays(3)).ConfigureAwait(false);
                        if (data != null) {
                            using (var stream = new MemoryStream(data))
                            using (var zip = ZipArchive.Open(stream)) {
                                var entry = zip.Entries.FirstOrDefault(x => x.Key == @"error.wav");
                                if (entry == null) throw new Exception("Invalid data");
                                using (var s = entry.OpenEntryStream()) {
                                    PlayWav(await s.ReadAsBytesAndDisposeAsync().ConfigureAwait(false));
                                }
                            }
                        }
                    } catch (Exception e) {
                        Logging.Warning(e);
                    }
                    break;
            }
        }
    }
}