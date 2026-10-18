using System;
using System.IO;
using System.Reflection;

namespace AcTools.Utils.Helpers {
    public static class MainExecutingFile {
        private static string _location;
        private static string _name;
        private static string _directory;

        public static string Location => _location ?? (_location = GetLocation());

        private static string GetLocation() {
            // With .NET Core, entry assembly is a DLL, but we need EXE: it is what is going to be used to restart the app, for example
            var path = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(path) && !string.Equals(Path.GetFileNameWithoutExtension(path), "dotnet", StringComparison.OrdinalIgnoreCase)) {
                return path;
            }

            var entry = Assembly.GetEntryAssembly()?.Location ?? "";
            var exe = Path.ChangeExtension(entry, ".exe");
            return File.Exists(exe) ? exe : entry;
        }


        public static string Name => _name ?? (_name = Path.GetFileName(Location));

        public static string Directory => _directory ?? (_directory = Path.GetDirectoryName(Location));

        public static bool IsInDevelopment => string.Equals(Name, @"devenv.exe", StringComparison.OrdinalIgnoreCase) ||
                                              string.Equals(Name, @"XDesProc.exe", StringComparison.OrdinalIgnoreCase);

        private const long PackedSizeAtLeast = 2000000;

        public static bool IsPacked => new FileInfo(Location).Length > PackedSizeAtLeast;
    }
}
