using System;
using System.IO;
using System.Linq;

namespace AcTools.Windows.Compat {
    public enum PowerLineStatus {
        Offline = 0,
        Online = 1,
        Unknown = 255
    }

    public class PowerStatus {
        public PowerLineStatus PowerLineStatus {
            get {
                try {
                    if (OperatingSystem.IsLinux()) {
                        var supplies = Directory.GetDirectories("/sys/class/power_supply");
                        var mains = supplies.Where(x => File.Exists(Path.Combine(x, "type")) && File.ReadAllText(Path.Combine(x, "type")).Trim() == "Mains").ToList();
                        if (mains.Count == 0) return PowerLineStatus.Online;
                        return mains.Any(x => File.ReadAllText(Path.Combine(x, "online")).Trim() == "1") ? PowerLineStatus.Online : PowerLineStatus.Offline;
                    }
                } catch (Exception) {
                    // ignored
                }

                return PowerLineStatus.Unknown;
            }
        }
    }

    /// <summary>
    /// Replacement of WinForms’ SystemInformation (only the parts that are in use).
    /// </summary>
    public static class SystemInformation {
        public static PowerStatus PowerStatus { get; } = new PowerStatus();
    }
}
