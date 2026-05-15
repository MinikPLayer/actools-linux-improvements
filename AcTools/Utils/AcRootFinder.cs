using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace AcTools.Utils {
    public static class AcRootFinder {
        private static string GetLinuxSteamAppsDirectory()
        {
            const string BASE_PATH = "Z:\\home";
            const string SUB_PATH = ".steam\\steam\\steamapps";

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

        private static string GetSteamAppsDirectoryFromCurrentPath()
        {
            var cwd = Directory.GetCurrentDirectory();
            var target = Path.Combine(cwd, "..\\..\\");

            if (Directory.Exists(target))
                return target;
            
            return null;
        }

        private static IEnumerable<string> GetSteamAppsDirectories() {
            var linuxSteamAppsDirectory = GetLinuxSteamAppsDirectory();
            if (linuxSteamAppsDirectory != null)
            {
                AcToolsLogging.Write($"Got Steam apps directory (linux): ${linuxSteamAppsDirectory}");
                yield return linuxSteamAppsDirectory;
            }

            var steamAppsCwdDir = GetSteamAppsDirectoryFromCurrentPath();
            if (steamAppsCwdDir != null)
            {
                AcToolsLogging.Write($"Got Steam apps directory from CWD: ${linuxSteamAppsDirectory}");
                yield return linuxSteamAppsDirectory;
            }

            var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            if (regKey == null) yield break;

            yield return Path.GetDirectoryName(regKey.GetValue("SourceModInstallPath").ToString());

            var steamPath = regKey.GetValue("SteamPath").ToString();
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
                var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
                return regKey == null ? null :
                        (from searchCandidate in GetSteamAppsDirectories()
                         where searchCandidate != null && Directory.Exists(searchCandidate)
                         select Path.Combine(searchCandidate, @"common", @"assettocorsa")).FirstOrDefault(Directory.Exists);
            } catch (Exception e) {
                AcToolsLogging.Write(e);
                return null;
            }
        }
    }
}