using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace AcTools.Utils {
    public static class AcRootFinder {
        /// <summary>
        /// Native Linux: Steam directories (including additional libraries listed in libraryfolders.vdf).
        /// </summary>
        private static IEnumerable<string> GetNativeSteamDirectories() {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home)) yield break;

            var roots = new[] {
                Path.Combine(home, ".steam", "steam"),
                Path.Combine(home, ".local", "share", "Steam"),
                Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam")
            };

            var returned = new HashSet<string>();
            foreach (var root in roots.Where(Directory.Exists)) {
                if (returned.Add(Path.GetFullPath(root))) yield return root;

                string text;
                try {
                    var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                    if (!File.Exists(vdf)) continue;
                    text = File.ReadAllText(vdf);
                } catch (Exception e) {
                    AcToolsLogging.Write(e);
                    continue;
                }

                foreach (Match m in Regex.Matches(text, "\"path\"\\s+\"(.+?)\"")) {
                    var path = m.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(path) && returned.Add(Path.GetFullPath(path))) yield return path;
                }
            }
        }

        private static string GetLinuxSteamAppsDirectory()
        {
            if (!OperatingSystem.IsWindows()) {
                return GetNativeSteamDirectories().FirstOrDefault();
            }

            const string BASE_PATH = "Z:\\home";
            const string SUB_PATH = ".steam\\steam";

            AcToolsLogging.Write("Searching for a linux path...");

            var user = Environment.GetEnvironmentVariable("USER");
            if (string.IsNullOrEmpty(user))
            {
                AcToolsLogging.Write("User env variable not defined.");
                return null;
            }

            var userHome = Path.Combine(BASE_PATH, user);
            if (!Directory.Exists(Path.Combine(userHome)))
            {
                AcToolsLogging.Write("User home doesn't exist.");
                return null;
            }

            try
            {
                var homeUserAcPath = Path.Combine(userHome, SUB_PATH);
                if (Directory.Exists(homeUserAcPath))
                {
                    return homeUserAcPath;
                }

                AcToolsLogging.Write("Cannot find Linux assetto corsa directory.");
                return null;
            }
            catch (Exception e)
            {
                AcToolsLogging.Write(e);
            }

            return null;
        }

        private static string GetSteamDirectoryFromCurrentPath()
        {
            var cwd = Directory.GetCurrentDirectory();
            var target = Path.Combine(cwd, "..\\..\\..\\");

            if (Directory.Exists(target) && File.Exists(Path.Combine(target, "Steam.exe")))
                return target;
            
            return null;
        }

        private static RegistryKey OpenSteamKey() {
            // there is no registry on other platforms
            return OperatingSystem.IsWindows() ? Registry.CurrentUser?.OpenSubKey(@"Software\Valve\Steam") : null;
        }

        public static IEnumerable<string> GetSteamDirectories(bool checkReg)
        {
            var linuxSteamAppsDirectory = GetLinuxSteamAppsDirectory();
            if (linuxSteamAppsDirectory != null)
            {
                AcToolsLogging.Write($"Got Steam apps directory (linux): ${linuxSteamAppsDirectory}");
                yield return linuxSteamAppsDirectory;
            }

            var steamAppsCwdDir = GetSteamDirectoryFromCurrentPath();
            if (steamAppsCwdDir != null)
            {
                AcToolsLogging.Write($"Got Steam apps directory from CWD: ${linuxSteamAppsDirectory}");
                yield return steamAppsCwdDir;
            }

            if(checkReg)
            {
                var regKey = OpenSteamKey();
                if (regKey != null)
                {
                    var steamPath = regKey.GetValue("SteamPath")?.ToString();
                    if (steamPath != null) yield return steamPath;
                }
            }

        }

        private static IEnumerable<string> GetSteamAppsDirectories() {
            if (!OperatingSystem.IsWindows()) {
                foreach (var dir in GetNativeSteamDirectories()) {
                    yield return Path.Combine(dir, "steamapps");
                }

                yield break;
            }

            foreach(var dir in GetSteamDirectories(false))
                yield return Path.Combine(dir, "steamapps");
            
            var regKey = OpenSteamKey();
            if (regKey == null) yield break;

            var sourceMods = regKey.GetValue("SourceModInstallPath")?.ToString();
            if (sourceMods != null) yield return Path.GetDirectoryName(sourceMods);

            var steamPath = regKey.GetValue("SteamPath")?.ToString();
            if (steamPath == null) yield break;
            var config = File.ReadAllText(Path.Combine(steamPath, @"config", @"config.vdf"));

            var match = Regex.Match(config, "\"BaseInstallFolder_\\d\"\\s+\"(.+?)\"");
            while (match.Success) {
                if (match.Groups.Count > 1) {
                    yield return Path.Combine(match.Groups[1].Value.Replace(@"\\", @"\"), "SteamApps");
                }

                match = match.NextMatch();
            }
        }

        [CanBeNull]
        public static string TryToFind() {
            try {
                foreach (var searchCandidate in GetSteamAppsDirectories())
                {
                    var acPath = Path.Combine(searchCandidate, @"common", @"assettocorsa");
                    if (Directory.Exists(acPath))
                        return acPath;
                }
                return null;
            } catch (Exception e) {
                AcToolsLogging.Write(e);
                return null;
            }
        }
    }
}