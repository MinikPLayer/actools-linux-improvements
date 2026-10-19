using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FirstFloor.ModernUI.Dialogs;
using FirstFloor.ModernUI.Windows.Controls;

namespace AcManager.Internal {
    public class ChangelogEntry {
        public ChangelogEntry(string version, string changes, DateTime date) {
            Version = version;
            Changes = changes;
            Date = date;
        }

        public string Version { get; }
        public string Changes { get; }
        public DateTime Date { get; }
    }

    public class WebDownloadRule {
        public WebDownloadRule(string siteUrl, string rule, string[] data) {
            SiteUrl = siteUrl;
            Rule = rule;
            Data = data;
        }

        public string SiteUrl { get; }
        public string Rule { get; }
        public string[] Data { get; }
        public string Domain { get; set; }
    }

    public static class WebClientExtension {
        public static Task<byte[]> DownloadDataTaskAsyncSafe(this WebClient c, string url) {
            return c.DownloadDataTaskAsync(url);
        }
    }

    /// <summary>
    /// Open replacement of the closed-source part of Content Manager: has no keys, no private servers and no sharing,
    /// so the app works like an unregistered one (IsAllRight is false) and everything that needs the private API reports a failure.
    /// </summary>
    public static class InternalUtils {
        public const string ChecksumHeader = "X-Checksum";
        public const string AppReporterNotesKeyHeader = "X-Notes";
        public const string ShareContentTypeHeader = "X-Content-Type";
        public const string ShareContentNameHeader = "X-Content-Name";
        public const string ShareContentTargetHeader = "X-Content-Target";
        public const string ShareAuthorNameHeader = "X-Author-Name";
        public const string ShareRemovalKeyHeader = "X-Removal-Key";

        public class KeyResponse {
            public bool IsBlacklisted;
        }

        public enum AcControlPointCommand {
            FocusNextCar, FocusPlayerCar, FocusPreviousCar, GentleStop, ChangeCamera, ChangeExternalCamera, ChangeExtraCemera,
            RestartSession, Shutdown, SkipSession, StartGame, TeleportToPits, TeleportToPitsWithConfig
        }

        public class AcControlPoint : IDisposable {
            public bool IsAvailable() => false;
            public void Send(AcControlPointCommand cmd) { }
            public void Dispose() { }
        }

        public class DecompressingWebClient : WebClient {
            protected override WebRequest GetWebRequest(Uri address) {
                var request = base.GetWebRequest(address);
                if (request is HttpWebRequest http) {
                    http.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                }
                return request;
            }
        }

        public class ShareResult {
            public string Id;
            public string RemovalKey;
            public string Url => GetUrl(Id);
            public static string GetUrl(string id) => $"{MainApiDomain}/s/{id}";
        }

        public class SharedEntryLoaded {
            public byte[] Data;
            public string EntryType;
            public string Name;
            public string Target;
            public string Author;
        }

        public static string MainApiDomain => "https://acstuff.club";
        public static string PatchDataApiDomain => "https://acstuff.ru";
        public static int KunosServersNumber => 1;
        public static string Revoked => null;
        public static bool IsAllRight => false;
        public static string Key { get; private set; }
        public static string KeySafe => null;

        public static Task<bool?> CheckKeyAsync(string value, string privateValue, string userAgent, CancellationToken cancellation = default) => Task.FromResult<bool?>(false);
        public static void SetSteamId(string steamId) { }
        public static byte[] CmGetData_v3(string url, string userAgent) {
            url = Absolute(url);
            using (var client = new DecompressingWebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = userAgent;
                return client.DownloadData(url);
            }
        }

        public static async Task<Tuple<byte[], DateTime?>> CmGetDataAsync(string url, string userAgent, DateTime? ifModifiedSince, IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default) {
            return Tuple.Create(await CmGetDataAsync(url, userAgent, progress, cancellation), (DateTime?)null);
        }

        public static Task<Tuple<byte[], DateTime?>> CmGetPaintShopDataAsync(string carId, string userAgent, DateTime? ifModifiedSince, IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default) {
            return Task.FromResult<Tuple<byte[], DateTime?>>(null);
        }

        private static string Absolute(string url) => url.Contains("://") ? url : CmGetApiAddress(url);

        public static async Task<byte[]> CmGetDataAsync(string url, string userAgent, IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default) {
            url = Absolute(url);
            using (var client = new DecompressingWebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = userAgent;
                using (cancellation.Register(client.CancelAsync)) {
                    return await client.DownloadDataTaskAsync(url);
                }
            }
        }

        public static Task<byte[]> CmGetContentDataAsync(string url, string userAgent, IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default) {
            return CmGetDataAsync(url, userAgent, progress, cancellation);
        }

        public static bool AcControlPointExecute(AcControlPointCommand cmd) => false;
        public static string CmGetApiAddress(string path) => $"{MainApiDomain}/api/{path}";
        public static string GetDiscordClientId() => null;
        public static string GetKunosUserAgent() => "AC Remote 1.0";
        public static string GetKunosServerUri(int offset) => "http://93.57.10.21/lobby.ashx";
        public static string GetKunosServerProxyUri() => null;
        public static string GetKunosServerCompressedProxyUri() => null;
        public static void MoveToNextKunosServer() { }
        public static string GetSteamApiCode() => null;
        public static string GetLobbyRegistryUrl(bool dev) => null;
        public static string GetContentRegistryUrl() => null;
        public static string GetGoogleMapsApiCode() => null;
        public static string GetOpenWeatherApiCode() => null;
        public static string GetTimeZoneDbApiCode() => null;
        public static void SendAppReport(byte[] data, string notes, string userAgent) { }
        public static void SendData(byte[] data, string notes, string userAgent) { }
        public static void SendLocale(byte[] data, string notes, string userAgent) { }
        public static IReadOnlyCollection<WebDownloadRule> LoadWebDownloadRules(string userAgent, string domain = null) => new WebDownloadRule[0];
        public static IReadOnlyList<ChangelogEntry> LoadChangelog(string userAgent, bool recentOnly) => new ChangelogEntry[0];
        public static Task<string> PostOnlineDataAsync(string data, string userAgent, CancellationToken cancellation) => Task.FromResult<string>(null);
        public static Task<string> GetOnlineDataAsync(string id, string userAgent, CancellationToken cancellation) => Task.FromResult<string>(null);
        public static Task DeleteSharedEntryAsync(string id, string removalKey, string userAgent) => Task.CompletedTask;
        public static Task<ShareResult> ShareEntryAsync(string typeString, string name, string target, string author, byte[] data, string userAgent, string customId, CancellationToken cancellation) {
            throw new NotSupportedException("Sharing requires the closed-source part of Content Manager");
        }
        public static bool VerifySignature(string filename) => false;
        public static Task<SharedEntryLoaded> GetSharedEntryAsync(string id, string userAgent, CancellationToken cancellation) => Task.FromResult<SharedEntryLoaded>(null);
        public static string GetEmojiUrl() => null;
        public static string GetValuesStorageEncryptionKey() => null;
        public static string GetOnlineInviteEncryptionKey() => null;
        public static Tuple<string> GetPositionStackApiKey() => null;
        public static Tuple<string> GetYouTubeApiKey() => null;
        public static Tuple<string> GetMegaAppKey() => null;
        public static Tuple<string, string> GetGoogleDriveCredentials() => null;
        public static Tuple<string, string> GetGoogleDriveAppFolderCredentials() => null;
        public static Tuple<string, string> GetOneDriveCredentials() => null;
        public static Tuple<string, string> GetOneDriveAppFolderCredentials() => null;
        public static Tuple<string, string> GetYandexDiskAppFolderCredentials() => null;
        public static Tuple<string, string> GetDropboxCredentials() => null;
        public static string GetRaceUPassword(string trackId, string ip, int port) => null;
        public static string GetRaceUChecksum(string steamId) => null;
        public static string GetPackedSteamId(string packed, string additionalSalt) => null;
        public static Tuple<string, string> GetLibrariesOptimizationTweak(int buildVersion) => null;
        public static void Initialize(string filename, string privateValue) { }
        public static void SetKey(string value, string privateValue) { Key = value; }
        public static void Launch(ModernWindow mainWindow) { }
    }
}
