using System;
using System.Linq;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AcTools.Windows.Compat {
    /// <summary>
    /// Minimal replacement for System.Windows.Forms.Cursor.Position. Only functional on Windows
    /// (on other platforms returns (0, 0)), as it’s used for Windows-only tricks (game starter, hotkeys).
    /// </summary>
    public static class Cursor {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint {
            public int X, Y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out NativePoint point);

        public static Point Position {
            get {
                if (!OperatingSystem.IsWindows()) return Point.Empty;
                return GetCursorPos(out var p) ? new Point(p.X, p.Y) : Point.Empty;
            }
        }
    }

    /// <summary>
    /// Minimal replacement for System.Windows.Forms.Screen (Windows only, falls back to 1920×1080 elsewhere).
    /// </summary>
    public class Screen {
        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint {
            public int X, Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MonitorInfo {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public int Flags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(NativePoint point, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        private static Rectangle ToRectangle(NativeRect r) => new Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

        private Screen(Rectangle bounds, Rectangle workingArea, string deviceName = null, bool primary = false) {
            Bounds = bounds;
            WorkingArea = workingArea;
            DeviceName = deviceName ?? @"\\.\DISPLAY1";
            Primary = primary;
        }

        /// <summary>
        /// Creates a screen description, to be used by <see cref="AllScreensProvider"/> on platforms where Win32 can’t be asked.
        /// </summary>
        public static Screen Create(string deviceName, Rectangle bounds, Rectangle workingArea, bool primary) {
            return new Screen(bounds, workingArea, deviceName, primary);
        }

        /// <summary>
        /// Source of screens for platforms other than Windows (set by UI layer, which knows how to ask windowing system).
        /// </summary>
        public static Func<Screen[]> AllScreensProvider { get; set; }

        public string DeviceName { get; }

        public override bool Equals(object obj) => obj is Screen s && s.DeviceName == DeviceName && s.Bounds == Bounds;

        public override int GetHashCode() => (DeviceName ?? "").GetHashCode() ^ Bounds.GetHashCode();

        public bool Primary { get; }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

        private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

        public static Screen[] AllScreens {
            get {
                if (!OperatingSystem.IsWindows()) {
                    return AllScreensProvider?.Invoke() ?? new[] { Fallback() };
                }

                var list = new System.Collections.Generic.List<Screen>();
                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, hdc, rect, data) => {
                    var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                    if (GetMonitorInfo(monitor, ref info)) {
                        list.Add(new Screen(ToRectangle(info.Monitor), ToRectangle(info.Work), info.DeviceName, (info.Flags & 1) != 0));
                    }
                    return true;
                }, IntPtr.Zero);
                return list.Count == 0 ? new[] { Fallback() } : list.ToArray();
            }
        }

        public Rectangle Bounds { get; }

        public Rectangle WorkingArea { get; }

        private static Screen Fallback() {
            var r = new Rectangle(0, 0, 1920, 1080);
            return new Screen(r, r, null, true);
        }

        public static Screen PrimaryScreen => FromPoint(Point.Empty);

        public static Screen FromPoint(Point point) {
            if (!OperatingSystem.IsWindows()) {
                var all = AllScreensProvider?.Invoke();
                return all?.FirstOrDefault(x => x.Bounds.Contains(point)) ?? all?.FirstOrDefault(x => x.Primary) ?? Fallback();
            }

            // MONITOR_DEFAULTTOPRIMARY
            var monitor = MonitorFromPoint(new NativePoint { X = point.X, Y = point.Y }, 1);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            return monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info)
                    ? new Screen(ToRectangle(info.Monitor), ToRectangle(info.Work))
                    : new Screen(new Rectangle(0, 0, GetSystemMetrics(0), GetSystemMetrics(1)), new Rectangle(0, 0, GetSystemMetrics(0), GetSystemMetrics(1)));
        }
    }
}

namespace AcTools.Windows.Compat {
    /// <summary>
    /// Replacement for System.Windows.Forms.SendKeys.SendWait, supports only what’s needed here (single key or Ctrl+key).
    /// Windows only, does nothing elsewhere.
    /// </summary>
    public static class SendKeys {
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, UIntPtr dwExtraInfo);

        public static void SendWait(string keys) {
            if (!OperatingSystem.IsWindows()) return;
            var ctrl = keys.StartsWith("^");

            byte vk;
            if (keys.Contains("{F7}")) {
                vk = (byte)Keys.F7;
            } else {
                var key = keys.Replace("^", "").Replace("(", "").Replace(")", "").ToUpperInvariant();
                if (key.Length != 1) return;
                vk = (byte)key[0];
            }

            if (ctrl) keybd_event((byte)Keys.ControlKey, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, 0, UIntPtr.Zero);
            keybd_event(vk, 0, 2, UIntPtr.Zero);
            if (ctrl) keybd_event((byte)Keys.ControlKey, 0, 2, UIntPtr.Zero);
        }
    }
}
