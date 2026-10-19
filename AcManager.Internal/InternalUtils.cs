using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using FirstFloor.ModernUI.Dialogs;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using FirstFloor.ModernUI.Serialization;
using FirstFloor.ModernUI.Windows.Attached;
using FirstFloor.ModernUI.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AcManager.Internal;

public static class InternalUtils
{
	public class KeyResponse
	{
		[JsonProperty("blacklist")]
		public bool IsBlacklisted;
	}

	private class ApiWebClient : WebClient
	{
		public DateTime? IfModifiedSince { get; set; }

		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest webRequest = base.GetWebRequest(address);
			if (webRequest is HttpWebRequest httpWebRequest && IfModifiedSince.HasValue)
			{
				httpWebRequest.IfModifiedSince = IfModifiedSince.Value;
				Logging.Debug((object)httpWebRequest.IfModifiedSince, "GetWebRequest", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 279);
				IfModifiedSince = null;
			}
			return webRequest;
		}
	}

	public enum AcControlPointCommand
	{
		FocusNextCar,
		FocusPlayerCar,
		FocusPreviousCar,
		GentleStop,
		ChangeCamera,
		ChangeExternalCamera,
		ChangeExtraCemera,
		RestartSession,
		Shutdown,
		SkipSession,
		StartGame,
		TeleportToPits,
		TeleportToPitsWithConfig
	}

	public class AcControlPoint : IDisposable
	{
		private Socket _socket;

		private bool _isBroken;

		public bool IsAvailable()
		{
			if (_socket == null && !_isBroken)
			{
				try
				{
					_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					_socket.Connect(SecretKey, int.Parse(SocketValue, NumberStyles.Any, CultureInfo.InvariantCulture));
				}
				catch (Exception ex)
				{
					_isBroken = true;
					Logging.Warning((object)("[I] Cannot process ACCP: " + ex.Message), "IsAvailable", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 592);
				}
			}
			return !_isBroken;
		}

		public void Send(AcControlPointCommand cmd)
		{
			if (!IsAvailable())
			{
				throw new Exception("Not available");
			}
			_socket.Send(BitConverter.GetBytes(GetAcControlValue(cmd)));
		}

		public void Dispose()
		{
			_socket?.Dispose();
			_isBroken = false;
		}
	}

	private class VersionComparer : IComparer<string>
	{
		private static int CompareAsVersionTo(string a, string b)
		{
			if (a == null)
			{
				if (b != null)
				{
					return -1;
				}
				return 0;
			}
			if (b == null)
			{
				return 1;
			}
			string[] array = a.Split(new char[1] { '.' });
			string[] array2 = b.Split(new char[1] { '.' });
			for (int i = 0; i < array.Length && i < array2.Length; i++)
			{
				int num = AlphanumComparatorFast.Compare(array[i], array2[i], true);
				if (num != 0)
				{
					return num;
				}
			}
			return array.Length - array2.Length;
		}

		public int Compare(string x, string y)
		{
			return -CompareAsVersionTo(x, y);
		}
	}

	public class DecompressingWebClient : WebClient
	{
		protected override WebRequest GetWebRequest(Uri address)
		{
			WebRequest webRequest = base.GetWebRequest(address);
			if (webRequest is HttpWebRequest httpWebRequest)
			{
				httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			}
			return webRequest;
		}
	}

	public class ShareResult
	{
		[JsonProperty("id")]
		public string Id;

		[JsonProperty("removalkey")]
		public string RemovalKey;

		[JsonIgnore]
		public string Url => GetUrl(Id);

		public static string GetUrl(string id)
		{
			return ShareServerAddress + id;
		}
	}

	public class SharedEntryLoaded
	{
		public byte[] Data;

		public string EntryType;

		public string Name;

		public string Target;

		public string Author;
	}

	public const string ChecksumHeader = "X-Data-Checksum";

	private static string CmServerAddress = MainApiDomain + "/app/cm/";

	private static string CmContentAddress = MainApiDomain + "/u/content/";

	private static string CmPaintShopAddress = MainApiDomain + "/u/paintshop/";

	private static string CmChecksumSalt = "Scared Average Calf Hot Iberianlynx Glittering Caudata";

	private static string _steamId;

	private static string SecretKey = "localhost";

	private static string SocketValue = "9666";

	private static string ServerAddress = MainApiDomain + "/app/helpers/";

	private static string[] _kunosServerUris = new string[2] { "lobby.assettocorsa.net", "93.57.10.21" };

	private static string _discordClientId = "395632636611264512";

	private static string _kunosUserAgent = "Assetto Corsa Launcher";

	private static int _currentServerUri;

	private static string _kunosServerProxyUri = "http://193.176.79.190:12013/list";

	private static string _kunosServerCompressedProxyUri = "https://acstuff.club/app/lobby-cache/data.bin";

	private static string _steamApiCode = "2FE4FEF5A3D8DA038C3AA97F290DD63E";

	private static string _googleMapsApiCode = "AIzaSyC9_cpsOlGF0rkvB8LWWkUxhozpyIHOPqU";

	private static string[] _openWeatherApiCodes = new string[5] { "cf6d07f80f0d1bb588f0df9a56f58ed0", "488a1650481097de690e09f46f161b06", "5be518143d396400c3f967a552f4e7e5", "f9d7607b3f663fa8a3616dd79636fba6", "94191b614a956c035dab00fbc4280bb7" };

	private static int? _openWeatherApiCodeId;

	private static string[] _timezoneDbApiCodes = new string[3] { "VIB4XMVPIXNK", "7IC6CRQJ22W8", "OPDK4OCH5VD1" };

	private static int? _timezoneDbApiCodeId;

	private static string AppReporterServerAddress = MainApiDomain + "/app/cm-report/";

	private static string AppReporterChecksumSalt = "jwdsra4i8na8m2t9k777rpb94140wwmi79xav2t9qba3v7vifbpousor5anka9k9gdi5p14iqyjbgldin301wcdi";

	private static string AppReporterEncryptionKey = "jcmlhaorp1npzaorkfublnmidfrjxlxrm3b1q0kx844e7b99bbne9udi10sve7b93n6u5wmiilnjnhfri5cxflxr";

	public const string AppReporterNotesKeyHeader = "X-Notes";

	private static DateTime _lastSent;

	private static string _lastChecksum;

	private static readonly Regex NextCursiveFixRegex = new Regex("^(\\s*)(-.+\\n.+\\n)\\s*(?=\\*\\w)", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex CursiveFixRegex = new Regex("^(\\s*)(-.+\\n)\\s*(?=\\*\\w)", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex ListRegex = new Regex("^(\\s*)-", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex ImageRegex = new Regex("!\\[([^\\]]+)\\]\\(([^)]+)\\)", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex LinkRegex = new Regex("\\[([^\\]]+)\\]\\(([^)]+)\\)", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex BoldRegex = new Regex("(?!<\\\\)\\*\\*([\\s\\S]+?)(?!<\\\\)\\*\\*", RegexOptions.Multiline | RegexOptions.Compiled);

	private static readonly Regex TagRegex = new Regex("(?!<\\\\)([*~`])([\\s\\S]+?)(?!<\\\\)\\1", RegexOptions.Multiline | RegexOptions.Compiled);

	private static string OnlineDataServerAddress = MainApiDomain + "/o/";

	private static string OnlineDataChecksumSalt = "m9kjuf8fjtwbbnz4pptt3ak5fem0b4vr7183869g6zlup5b";

	private static string ShareServerAddress = MainApiDomain + "/s/";

	private static string ShareChecksumSalt = "0apuv7viuwd5z5mi0gpkfbt968cnxw29pk384cxrgvipmn292rlwstt9kojkbj4i";

	public const string ShareContentTypeHeader = "X-Data-Content-Type";

	public const string ShareContentNameHeader = "X-Data-Content-Name";

	public const string ShareContentTargetHeader = "X-Data-Content-Target";

	public const string ShareAuthorNameHeader = "X-Data-Author-Name";

	public const string ShareRemovalKeyHeader = "X-Data-Removal-Key";

	private static string[] _valuesStorageEncryptionKey = new string[10] { "Rundown", "Calm", "Baldeagle", "Queasy", "Quahog", "Euphoric", "Kind", "Antelope", "Earnest", "Wolf" };

	private static string[] _onlineInviteEncryptionKey = new string[1] { "Honored" };

	private static string PackedSteamIdChecksumSalt = "Definite Xenopus and Sick Verifiable Xenarthra";

	private static readonly string KeyStorage = "AppKey";

	private static readonly string KeyValidFlag = "AppKeyValid";

	private static string _filename;

	public static string MainApiDomain { get; } = "https://acstuff.club";

	public static string PatchDataApiDomain { get; } = "https://cspdata.acstuff.club";

	public static int KunosServersNumber => _kunosServerUris.Length;

	public static string Revoked { get; private set; }

	public static bool IsAllRight { get; private set; }

	public static string Key { get; private set; }

	public static string KeySafe => ReplaceKeySafe(Key);

	private static bool IsDigit(char a)
	{
		if (a >= '0')
		{
			return a <= '9';
		}
		return false;
	}

	private static bool IsLetter(char a)
	{
		if (a >= 'A')
		{
			return a <= 'Z';
		}
		return false;
	}

	private static char Average(string s)
	{
		return (char)(((IEnumerable<char>)s).Select((Func<char, int>)((char x) => x)).Sum() / s.Length);
	}

	private static bool CompareBits(char a, char b)
	{
		string text = "srp2pgb9t0kq43di";
		if (!IsDigit(a))
		{
			if (IsLetter(a) && Enumerable.Contains("quthA85miczqwipBb9Fducy2e29", b))
			{
				return a == b;
			}
			return false;
		}
		if (IsDigit(b) && (b == Average(text[9].ToString() ?? "") || b == text[12]))
		{
			return a == b;
		}
		return false;
	}

	private static bool CompareBits(string a, string b)
	{
		for (int i = 0; i < a.Length && i < b.Length; i++)
		{
			if (!CompareBits(a[i], b[i]))
			{
				return false;
			}
		}
		return a.Length - b.Length == 0;
	}

	private static string ExtractMeaningfulBit(string hash)
	{
		return hash.Substring(2, 5);
	}

	private static int ProduceValue(string hash)
	{
		return int.Parse((hash.Length > 6) ? "020" : "010", CultureInfo.InvariantCulture) * hash.Length * 25 + (int)double.Parse("000000395.12345678", CultureInfo.InvariantCulture);
	}

	private static bool TestKeyChecksum(string hash)
	{
		return CompareBits(ExtractMeaningfulBit(hash), ProduceValue(hash).ToString("X5", CultureInfo.InvariantCulture));
	}

	private static byte[] Prepare(string s)
	{
		string text = "Wood";
		string text2 = ((s.Length > 47) ? "Of Course" : ((s.Length > 3) ? "Very Salty" : "Almost Not"));
		string text3 = ((s.Length == 0) ? "Balloonfish" : ((s[0] == '-') ? "Bluetickcoonhound" : "Nymphbutterfly"));
		return Encoding.ASCII.GetBytes(s + text2.Split(new char[1] { ' ' })[1] + (char)((s.Length > 4) ? 32u : 25u) + text + text3.ToLowerInvariant());
	}

	private static byte[] PrepareBlacklist(string s)
	{
		string text = "z01qncdivwj98uxrs8shh0k9goljif6r4l6m9529iocd6lxrewg3c8fr9vcdte29";
		return Encoding.ASCII.GetBytes(s + text);
	}

	private static string ToHexString(this byte[] data)
	{
		char[] array = new char[16]
		{
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
			'A', 'B', 'C', 'D', 'E', 'F'
		};
		int num = -1;
		int num2 = -1;
		int num3 = data.Length;
		char[] array2 = new char[num3-- * 2];
		while (num < num3)
		{
			byte b = data[++num];
			array2[++num2] = array[b >> 4];
			array2[++num2] = array[b & 0xF];
		}
		return new string(array2, 0, array2.Length);
	}

	private static async Task<bool?> CheckKeyBlacklistAsync(string value, string userAgent, CancellationToken cancellation = default(CancellationToken))
	{
		using SHA1 s = SHA1.Create();
		using DecompressingWebClient client = new DecompressingWebClient
		{
			Headers = { [HttpRequestHeader.UserAgent] = userAgent }
		};
		string text = "z01qncdivwj98uxrs8shh0k9goljif6r4l6m9529iocd6lxrewg3c8fr9vcdte29";
		string text2 = s.ComputeHash(Encoding.ASCII.GetBytes(value + text)).ToHexString();
		try
		{
			string text3 = await client.DownloadStringTaskAsync(CmServerAddress + "key/" + text2);
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			KeyResponse keyResponse = JsonConvert.DeserializeObject<KeyResponse>(text3);
			return !keyResponse.IsBlacklisted;
		}
		catch (WebException ex)
		{
			Logging.Write((object)("Can't check app's key: " + ex.Message), "CheckKeyBlacklistAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 121);
			return null;
		}
		catch (Exception ex2)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			Logging.Write((object)("Can't check app's key: " + ex2), "CheckKeyBlacklistAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 125);
			return null;
		}
	}

	private static string GetPostfixPrepared(string postfix)
	{
		using SHA1 sHA = SHA1.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes(postfix + "qzrunradw4c1issh81h9ws3sr4zakcqwkdnffa6y")).ToHexString();
	}

	private static bool CheckKey(string value, string privateValue)
	{
		bool flag;
		if (!string.IsNullOrWhiteSpace(value) && Regex.IsMatch(value, "([0-9A-Z]{4}-)+[0-9A-Z]{4}"))
		{
			using SHA1 sHA = SHA1.Create();
			flag = TestKeyChecksum(sHA.ComputeHash(Prepare(value)).ToHexString());
			if (!flag && privateValue != null && value.StartsWith("O4AK", StringComparison.OrdinalIgnoreCase))
			{
				string s = value.Substring(4).TrimStart(new char[1] { '-' }) + GetPostfixPrepared(privateValue);
				flag = TestKeyChecksum(sHA.ComputeHash(Prepare(s)).ToHexString());
			}
			if (Manage(sHA.ComputeHash(PrepareBlacklist(value)).ToHexString()))
			{
				flag = false;
			}
		}
		else
		{
			flag = false;
		}
		return flag;
	}

	public static async Task<bool?> CheckKeyAsync(string value, string privateValue, string userAgent, CancellationToken cancellation = default(CancellationToken))
	{
		_ = 3;
		try
		{
			await Task.Delay(100, cancellation);
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			value = CleanUpKey(value);
			bool? result = CheckKey(value, privateValue);
			if (result == true)
			{
				result = await CheckKeyBlacklistAsync(value, userAgent, cancellation);
				if (cancellation.IsCancellationRequested)
				{
					return null;
				}
			}
			else
			{
				await Task.Delay(100, cancellation);
				if (cancellation.IsCancellationRequested)
				{
					return null;
				}
			}
			await Task.Delay(100, cancellation);
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			return result;
		}
		catch (Exception)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			return false;
		}
	}

	private static bool CmTestChecksum(byte[] response, string targetChecksum)
	{
		using SHA1 sHA = SHA1.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes(sHA.ComputeHash(response).ToHexString() + CmChecksumSalt)).ToHexString() == targetChecksum;
	}

	public static void SetSteamId(string steamId)
	{
		using SHA1 sHA = SHA1.Create();
		_steamId = (string.IsNullOrWhiteSpace(steamId) ? null : sHA.ComputeHash(Encoding.UTF8.GetBytes(sHA.ComputeHash(Encoding.UTF8.GetBytes(steamId)).ToHexString() + AppReporterChecksumSalt)).ToHexString());
	}

	public static byte[] CmGetData_v3(string url, string userAgent)
	{
		if (CmServerAddress == null)
		{
			return null;
		}
		if (url.StartsWith("app/") && url.EndsWith("/latest") && !IsAllRight)
		{
			return null;
		}
		try
		{
			using DecompressingWebClient decompressingWebClient = new DecompressingWebClient
			{
				Headers = { [HttpRequestHeader.UserAgent] = userAgent }
			};
			byte[] array = decompressingWebClient.DownloadData(CmServerAddress + url);
			string text = decompressingWebClient.ResponseHeaders.Get("X-Data-Checksum");
			if (text == null)
			{
				Logging.Warning((object)"[I] Checksum is missing", "CmGetData_v3", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 249);
				return null;
			}
			if (!CmTestChecksum(array, text))
			{
				Logging.Warning((object)"[I] Checksum is wrong", "CmGetData_v3", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 254);
				return null;
			}
			return array;
		}
		catch (WebException ex)
		{
			Logging.Warning((object)("[I] Cannot get " + url + ": " + ex.Message), "CmGetData_v3", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 264);
			return null;
		}
		catch (Exception ex2)
		{
			Logging.Warning((object)("[I] Cannot get " + url + ": " + ex2), "CmGetData_v3", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 267);
			return null;
		}
	}

	public static Task<Tuple<byte[], DateTime?>> CmGetDataAsync(string url, string userAgent, DateTime? ifModifiedSince, IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default(CancellationToken))
	{
		if (CmServerAddress == null)
		{
			return null;
		}
		if (url.StartsWith("app/") && url.EndsWith("/latest") && !IsAllRight)
		{
			return null;
		}
		return CmGetDataInnerAsync(CmServerAddress + url, userAgent, ifModifiedSince, progress, cancellation);
	}

	public static Task<Tuple<byte[], DateTime?>> CmGetPaintShopDataAsync(string carId, string userAgent, DateTime? ifModifiedSince, IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default(CancellationToken))
	{
		if (CmPaintShopAddress == null)
		{
			return null;
		}
		return CmGetDataInnerAsync(CmPaintShopAddress + carId, userAgent, ifModifiedSince, progress, cancellation);
	}

	private static async Task<Tuple<byte[], DateTime?>> CmGetDataInnerAsync(string fullUrl, string userAgent, DateTime? ifModifiedSince, IProgress<AsyncProgressEntry> progress, CancellationToken cancellation)
	{
		try
		{
			KillerOrder<ApiWebClient> order = KillerOrder.Create<ApiWebClient>(new ApiWebClient
			{
				Headers = { [HttpRequestHeader.UserAgent] = userAgent }
			}, 30000L);
			try
			{
				ApiWebClient client = order.Victim;
				if (ifModifiedSince.HasValue)
				{
					client.IfModifiedSince = ifModifiedSince.Value;
					Logging.Debug((object)client.IfModifiedSince, "CmGetDataInnerAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 314);
				}
				cancellation.ThrowIfCancellationRequested();
				cancellation.Register(client.CancelAsync);
				if (progress != null)
				{
					int skipEvent = 0;
					AsyncProgressBytesStopwatch stopwatch = new AsyncProgressBytesStopwatch();
					client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args)
					{
						//IL_0041: Unknown result type (might be due to invalid IL or missing references)
						((KillerOrder)order).Delay();
						if (++skipEvent > 5)
						{
							skipEvent = 0;
							progress.Report(AsyncProgressEntry.CreateDownloading(args.BytesReceived, args.TotalBytesToReceive, stopwatch));
						}
					};
				}
				byte[] array = ((progress == null) ? (await client.DownloadDataTaskAsyncSafe(fullUrl)) : (await client.DownloadDataTaskAsync(fullUrl)));
				byte[] array2 = array;
				if (cancellation.IsCancellationRequested)
				{
					return null;
				}
				string text = client.ResponseHeaders.Get("X-Data-Checksum");
				DateTime result;
				if (text != null && CmTestChecksum(array2, text))
				{
					return Tuple.Create(array2, DateTime.TryParse(client.ResponseHeaders["Last-Modified"], out result) ? new DateTime?(result) : ((DateTime?)null));
				}
				Logging.Warning((object)"[I] Checksum is missing or wrong", "CmGetDataInnerAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 351);
				return null;
			}
			finally
			{
				if (order != null)
				{
					((IDisposable)order).Dispose();
				}
			}
		}
		catch (WebException ex)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			HttpStatusCode? httpStatusCode = (ex.Response as HttpWebResponse)?.StatusCode;
			if (httpStatusCode == HttpStatusCode.NotModified)
			{
				return Tuple.Create<byte[], DateTime?>(new byte[0], null);
			}
			Logging.Warning((object)($"[I] Cannot get data: {httpStatusCode}, " + ex.Message), "CmGetDataInnerAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 364);
			return null;
		}
		catch (Exception ex2)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			Logging.Warning((object)("[I] Cannot get data: " + ex2), "CmGetDataInnerAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 368);
			return null;
		}
	}

	public static async Task<byte[]> CmGetDataAsync(string url, string userAgent, IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default(CancellationToken))
	{
		if (CmServerAddress == null)
		{
			return null;
		}
		if (url.StartsWith("app/") && url.EndsWith("/latest") && !IsAllRight)
		{
			return null;
		}
		if (url.StartsWith("app/"))
		{
			string stringToEscape = StringCipher.Encrypt(Key ?? "-", "jpwmnfbijuarbpet4a4j355ij5u0seim");
			string stringToEscape2 = StringCipher.Encrypt(_steamId ?? "-", "jpwmnfbijuarbpet4a4j355ij5u0seim");
			url = url + "?k=" + Uri.EscapeDataString(stringToEscape) + "&s=" + Uri.EscapeDataString(stringToEscape2);
		}
		try
		{
			KillerOrder<DecompressingWebClient> order = KillerOrder.Create<DecompressingWebClient>(new DecompressingWebClient
			{
				Headers = { [HttpRequestHeader.UserAgent] = userAgent }
			}, 120000L);
			try
			{
				DecompressingWebClient client = order.Victim;
				cancellation.ThrowIfCancellationRequested();
				cancellation.Register(client.CancelAsync);
				if (progress != null)
				{
					int skipEvent = 0;
					AsyncProgressBytesStopwatch stopwatch = new AsyncProgressBytesStopwatch();
					client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args)
					{
						//IL_0041: Unknown result type (might be due to invalid IL or missing references)
						((KillerOrder)order).Delay();
						if (++skipEvent > 5)
						{
							skipEvent = 0;
							progress.Report(AsyncProgressEntry.CreateDownloading(args.BytesReceived, args.TotalBytesToReceive, stopwatch));
						}
					};
				}
				byte[] array = ((progress == null) ? (await client.DownloadDataTaskAsyncSafe(CmServerAddress + url)) : (await client.DownloadDataTaskAsync(CmServerAddress + url)));
				byte[] array2 = array;
				if (cancellation.IsCancellationRequested)
				{
					return null;
				}
				string text = client.ResponseHeaders.Get("X-Data-Checksum");
				if (text != null && CmTestChecksum(array2, text))
				{
					return array2;
				}
				Logging.Warning((object)"[I] Checksum is missing or wrong", "CmGetDataAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 425);
				return null;
			}
			finally
			{
				if (order != null)
				{
					((IDisposable)order).Dispose();
				}
			}
		}
		catch (WebException ex)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			HttpStatusCode? httpStatusCode = (ex.Response as HttpWebResponse)?.StatusCode;
			if (httpStatusCode == HttpStatusCode.NotModified)
			{
				return new byte[0];
			}
			Logging.Warning((object)($"[I] Cannot get {url}: {httpStatusCode}, " + ex), "CmGetDataAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 437);
			return null;
		}
		catch (Exception ex2)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			Logging.Warning((object)("[I] Cannot get " + url + ": " + ex2), "CmGetDataAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 441);
			return null;
		}
	}

	public static async Task<byte[]> CmGetContentDataAsync(string url, string userAgent, IProgress<AsyncProgressEntry> progress = null, CancellationToken cancellation = default(CancellationToken))
	{
		if (CmContentAddress == null)
		{
			return null;
		}
		try
		{
			KillerOrder<DecompressingWebClient> order = KillerOrder.Create<DecompressingWebClient>(new DecompressingWebClient
			{
				Headers = 
				{
					[HttpRequestHeader.UserAgent] = userAgent,
					[HttpRequestHeader.Accept] = "application/json"
				}
			}, 30000L);
			try
			{
				DecompressingWebClient client = order.Victim;
				cancellation.ThrowIfCancellationRequested();
				cancellation.Register(client.CancelAsync);
				if (progress != null)
				{
					int skipEvent = 0;
					AsyncProgressBytesStopwatch stopwatch = new AsyncProgressBytesStopwatch();
					client.DownloadProgressChanged += delegate(object sender, DownloadProgressChangedEventArgs args)
					{
						//IL_0041: Unknown result type (might be due to invalid IL or missing references)
						((KillerOrder)order).Delay();
						if (++skipEvent > 5)
						{
							skipEvent = 0;
							progress.Report(AsyncProgressEntry.CreateDownloading(args.BytesReceived, args.TotalBytesToReceive, stopwatch));
						}
					};
				}
				byte[] array = ((progress == null) ? (await client.DownloadDataTaskAsyncSafe(CmContentAddress + url)) : (await client.DownloadDataTaskAsync(CmContentAddress + url)));
				byte[] array2 = array;
				if (cancellation.IsCancellationRequested)
				{
					return null;
				}
				string text = client.ResponseHeaders.Get("X-Data-Checksum");
				if (text != null && CmTestChecksum(array2, text))
				{
					return array2;
				}
				Logging.Warning((object)"[I] Checksum is missing or wrong", "CmGetContentDataAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 496);
				return null;
			}
			finally
			{
				if (order != null)
				{
					((IDisposable)order).Dispose();
				}
			}
		}
		catch (WebException ex)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			HttpStatusCode? httpStatusCode = (ex.Response as HttpWebResponse)?.StatusCode;
			if (httpStatusCode == HttpStatusCode.NotModified)
			{
				return new byte[0];
			}
			Logging.Warning((object)$"[I] Cannot get {url}: {httpStatusCode}, {ex.Message}", "CmGetContentDataAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 508);
			return null;
		}
		catch (Exception arg)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			Logging.Warning((object)$"[I] Cannot get {url}: {arg}", "CmGetContentDataAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 512);
			return null;
		}
	}

	private static int GetAcControlValue(AcControlPointCommand cmd)
	{
		return cmd switch
		{
			AcControlPointCommand.Shutdown => 66, 
			AcControlPointCommand.ChangeCamera => 67, 
			AcControlPointCommand.RestartSession => 68, 
			AcControlPointCommand.StartGame => 69, 
			AcControlPointCommand.FocusNextCar => 70, 
			AcControlPointCommand.FocusPlayerCar => 71, 
			AcControlPointCommand.FocusPreviousCar => 72, 
			AcControlPointCommand.ChangeExtraCemera => 73, 
			AcControlPointCommand.ChangeExternalCamera => 74, 
			AcControlPointCommand.TeleportToPits => 75, 
			AcControlPointCommand.TeleportToPitsWithConfig => 76, 
			AcControlPointCommand.GentleStop => 80, 
			AcControlPointCommand.SkipSession => 81, 
			_ => throw new ArgumentOutOfRangeException("cmd", cmd, null), 
		};
	}

	public static bool AcControlPointExecute(AcControlPointCommand cmd)
	{
		try
		{
			using AcControlPoint acControlPoint = new AcControlPoint();
			acControlPoint.Send(cmd);
			return true;
		}
		catch (Exception ex)
		{
			Logging.Warning((object)("[I] " + ex.Message), "AcControlPointExecute", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 576);
			return false;
		}
	}

	public static string CmGetApiAddress(string path)
	{
		return ServerAddress + path;
	}

	public static string GetDiscordClientId()
	{
		return _discordClientId;
	}

	public static string GetKunosUserAgent()
	{
		return _kunosUserAgent;
	}

	public static string GetKunosServerUri(int offset)
	{
		return _kunosServerUris.ElementAtOrDefault((_currentServerUri + offset) % KunosServersNumber);
	}

	public static string GetKunosServerProxyUri()
	{
		return _kunosServerProxyUri;
	}

	public static string GetKunosServerCompressedProxyUri()
	{
		return _kunosServerCompressedProxyUri;
	}

	public static void MoveToNextKunosServer()
	{
		_currentServerUri++;
	}

	public static string GetSteamApiCode()
	{
		return _steamApiCode;
	}

	public static string GetLobbyRegistryUrl(bool dev)
	{
		return "https://cup.acstuff.club/plugins/" + (dev ? "lobbies-dev" : "lobbies");
	}

	public static string GetContentRegistryUrl()
	{
		return "https://cup.acstuff.club/registry";
	}

	public static string GetGoogleMapsApiCode()
	{
		return _googleMapsApiCode;
	}

	public static string GetOpenWeatherApiCode()
	{
		if (!_openWeatherApiCodeId.HasValue)
		{
			_openWeatherApiCodeId = ValuesStorage.Get<int>(".ipa_owac", new Random(Guid.NewGuid().GetHashCode()).Next(0, _openWeatherApiCodes.Length));
			ValuesStorage.Set(".ipa_owac", (object)_openWeatherApiCodeId);
		}
		return _openWeatherApiCodes[_openWeatherApiCodeId.Value];
	}

	public static string GetTimeZoneDbApiCode()
	{
		if (!_timezoneDbApiCodeId.HasValue)
		{
			_timezoneDbApiCodeId = ValuesStorage.Get<int>(".ipa_tzci", new Random(Guid.NewGuid().GetHashCode()).Next(0, _timezoneDbApiCodes.Length));
			ValuesStorage.Set(".ipa_tzci", (object)_timezoneDbApiCodeId);
		}
		return _timezoneDbApiCodes[_timezoneDbApiCodeId.Value];
	}

	private static void AppReporterSend(string url, byte[] data, string notes, string userAgent)
	{
		if ((DateTime.Now - _lastSent).TotalSeconds < 1.0)
		{
			throw new Exception("Too frequent");
		}
		_lastSent = DateTime.Now;
		string text = AppReporterGetChecksum(data);
		if (text == _lastChecksum)
		{
			throw new Exception("Same data");
		}
		_lastChecksum = text;
		try
		{
			using (DecompressingWebClient decompressingWebClient = new DecompressingWebClient
			{
				Headers = 
				{
					[HttpRequestHeader.UserAgent] = userAgent,
					["X-Data-Checksum"] = text,
					["X-Notes"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(notes))
				}
			})
			{
				decompressingWebClient.UploadData(AppReporterServerAddress + url, data);
			}
			Logging.Write((object)$"[I] Sent {data.Length} byte(s)", "AppReporterSend", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 748);
		}
		catch (Exception ex)
		{
			Logging.Warning((object)("[I] Cannot upload " + url + ": " + ex), "AppReporterSend", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 750);
			throw;
		}
	}

	private static string AppReporterGetChecksum(byte[] data)
	{
		if (AppReporterChecksumSalt == null)
		{
			return null;
		}
		using SHA1 sHA = SHA1.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes(sHA.ComputeHash(data).ToHexString() + AppReporterChecksumSalt)).ToHexString();
	}

	public static void SendAppReport(byte[] data, string notes, string userAgent)
	{
		AppReporterSend("logs", StringCipher.Encrypt(data, AppReporterEncryptionKey), notes, userAgent);
	}

	public static void SendData(byte[] data, string notes, string userAgent)
	{
		AppReporterSend("data", StringCipher.Encrypt(data, AppReporterEncryptionKey), notes, userAgent);
	}

	public static void SendLocale(byte[] data, string notes, string userAgent)
	{
		AppReporterSend("locale", StringCipher.Encrypt(data, AppReporterEncryptionKey), notes, userAgent);
	}

	private static string RepeatString(string s, int number)
	{
		if (s == null)
		{
			return null;
		}
		switch (number)
		{
		case 0:
			return string.Empty;
		case 1:
			return s;
		case 2:
			return s + s;
		default:
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < number; i++)
			{
				stringBuilder.Append(s);
			}
			return stringBuilder.ToString();
		}
		}
	}

	private static string TagRegexCallback(Match m)
	{
		string text;
		switch (m.Groups[1].Value)
		{
		case "*":
			text = "i";
			break;
		case "~":
			text = "s";
			break;
		case "`":
			text = "mono";
			break;
		default:
			return m.Value;
		}
		return "[" + text + "]" + m.Groups[2].Value + "[/" + text + "]";
	}

	private static string ChangelogPrepare(string s)
	{
		s = Regex.Replace(s, "^###.+", "").Trim();
		s = NextCursiveFixRegex.Replace(s, (Match m) => m.Groups[1].Value + m.Groups[2].Value + "\u00a0\u00a0\u00a0" + RepeatString("\u00a0\u00a0", m.Groups[1].Length));
		s = CursiveFixRegex.Replace(s, (Match m) => m.Groups[1].Value + m.Groups[2].Value + "\u00a0\u00a0\u00a0" + RepeatString("\u00a0\u00a0", m.Groups[1].Length));
		s = ListRegex.Replace(s, (Match m) => "\u00a0" + RepeatString("\u00a0\u00a0", m.Groups[1].Length) + ((m.Groups[1].Length < 2) ? "•" : "◦"));
		s = ImageRegex.Replace(s, "\u2003[img=\"$2|240\"]$1[/img]");
		s = LinkRegex.Replace(s, "[url=\"$2\"]$1[/url]");
		s = BoldRegex.Replace(s, "[b]$1[/b]");
		s = TagRegex.Replace(s, TagRegexCallback);
		s = TagRegex.Replace(s, TagRegexCallback);
		s = TagRegex.Replace(s, TagRegexCallback);
		return s;
	}

	public static IReadOnlyCollection<WebDownloadRule> LoadWebDownloadRules(string userAgent, string domain = null)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0088: Expected O, but got Unknown
		using DecompressingWebClient decompressingWebClient = new DecompressingWebClient
		{
			Headers = { [HttpRequestHeader.UserAgent] = userAgent }
		};
		return (from x in ((IEnumerable<JToken>)(JArray)JObject.Parse(decompressingWebClient.DownloadString((domain == null) ? "https://acstuff.club/f/api/posts/content-extra?filter[discussion]=24&filter[type]=comment&sort=-time&page[size]=99999" : ("https://acstuff.club/f/api/posts/content-extra?filter[discussion]=24&filter[content]=" + Uri.EscapeDataString(domain) + "&filter[type]=comment&sort=-time&page[size]=50")))["data"]).Select((Func<JToken, JObject>)((JToken x) => (JObject)((JObject)x)["attributes"]))
			select new
			{
				Author = (int)x["user"],
				SiteUrl = Regex.Match(((object)x["content"]).ToString().Trim(), "^### .+\\(([^\\(]+)\\)\\s*\\n").Groups[1].Value,
				Rule = Regex.Match(((object)x["content"]).ToString().Trim(), "(?<=\\n)```\\w*\\s*?\\n([\\s\\S]+)\\n```").Groups[1].Value,
				Data = HttpUtility.ParseQueryString(Regex.Match(((object)x["content"]).ToString().Trim(), "/q:importWebsite?([^\\)]+)\\)\\.").Groups[1].Value).GetValues("data"),
				Likes = x["likes"].ToObject<int[]>()
			} into x
			where x.SiteUrl != null && x.Rule != null && x.Data != null && (x.Author == 1 || x.Likes.Contains(1) || !x.Rule.StartsWith("javascript:") || x.Likes.Length > 2)
			select new WebDownloadRule(x.SiteUrl, x.Rule, x.Data) into x
			where (domain == null || x.Domain == domain) && x.Data.All((string y) => !string.IsNullOrWhiteSpace(y))
			select x).ToList();
	}

	public static IReadOnlyList<ChangelogEntry> LoadChangelog(string userAgent, bool recentOnly)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		using DecompressingWebClient decompressingWebClient = new DecompressingWebClient
		{
			Headers = { [HttpRequestHeader.UserAgent] = userAgent }
		};
		return (from x in (from x in ((IEnumerable<JToken>)(JArray)JObject.Parse(decompressingWebClient.DownloadString(recentOnly ? (MainApiDomain + "/f/api/posts/content?filter[discussion]=10&filter[user]=1&filter[type]=comment&sort=-time&page[size]=50") : (MainApiDomain + "/f/api/posts/content?filter[discussion]=10&filter[user]=1&filter[type]=comment&sort=-time&page[size]=99999")))["data"]).Select((Func<JToken, JObject>)((JToken x) => (JObject)((JObject)x)["attributes"]))
				select new
				{
					Content = ((object)x["content"]).ToString().Trim(),
					Date = DateTime.Parse(((object)x["time"]).ToString(), CultureInfo.InvariantCulture)
				} into x
				select new ChangelogEntry(Regex.Match(x.Content, "^### ([.\\d]+)").Groups[1].Value, ChangelogPrepare(x.Content), x.Date)).OrderBy((ChangelogEntry x) => x.Version, new VersionComparer())
			where !string.IsNullOrEmpty(x.Version)
			select x).ToList();
	}

	private static string OnlineDataGetChecksum(byte[] data)
	{
		if (OnlineDataChecksumSalt == null)
		{
			return null;
		}
		using SHA1 sHA = SHA1.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes(sHA.ComputeHash(data).ToHexString() + OnlineDataChecksumSalt)).ToHexString().ToLowerInvariant();
	}

	private static bool OnlineDataTestChecksum(byte[] response, string targetChecksum)
	{
		return OnlineDataGetChecksum(response) == targetChecksum;
	}

	public static async Task<string> PostOnlineDataAsync(string data, string userAgent, CancellationToken cancellation = default(CancellationToken))
	{
		try
		{
			byte[] bytes = Encoding.UTF8.GetBytes(data);
			KillerOrder<DecompressingWebClient> client = KillerOrder.Create<DecompressingWebClient>(new DecompressingWebClient
			{
				Headers = 
				{
					[HttpRequestHeader.UserAgent] = userAgent,
					["X-Data-Checksum"] = OnlineDataGetChecksum(bytes)
				}
			}, 10000L, cancellation);
			try
			{
				byte[] bytes2 = await client.Victim.UploadDataTaskAsync(OnlineDataServerAddress, bytes);
				if (cancellation.IsCancellationRequested)
				{
					return null;
				}
				return (string)JObject.Parse(Encoding.UTF8.GetString(bytes2))["id"];
			}
			finally
			{
				((IDisposable)client)?.Dispose();
			}
		}
		catch (Exception ex)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			Logging.Warning((object)("[I] Cannot upload online data: " + ex), "PostOnlineDataAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 947);
			throw;
		}
	}

	public static async Task<string> GetOnlineDataAsync(string id, string userAgent, CancellationToken cancellation = default(CancellationToken))
	{
		KillerOrder<DecompressingWebClient> client = KillerOrder.Create<DecompressingWebClient>(new DecompressingWebClient
		{
			Headers = { [HttpRequestHeader.UserAgent] = userAgent }
		}, 3000L, cancellation);
		try
		{
			byte[] array;
			try
			{
				array = await client.Victim.DownloadDataTaskAsyncSafe(OnlineDataServerAddress + id);
			}
			catch (Exception ex)
			{
				if (cancellation.IsCancellationRequested)
				{
					return null;
				}
				Logging.Warning((object)("[I] Cannot get " + id + ": " + ex), "GetOnlineDataAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 973);
				throw;
			}
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			string text = client.Victim.ResponseHeaders.Get("X-Data-Checksum");
			if (text == null)
			{
				throw new Exception("Checksum is missing");
			}
			if (!OnlineDataTestChecksum(array, text))
			{
				throw new Exception("Checksum doesn't match");
			}
			return Encoding.UTF8.GetString(array);
		}
		finally
		{
			((IDisposable)client)?.Dispose();
		}
	}

	private static string ShareGetChecksum(byte[] data)
	{
		if (ShareChecksumSalt == null)
		{
			return null;
		}
		using SHA1 sHA = SHA1.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes(sHA.ComputeHash(data).ToHexString() + ShareChecksumSalt)).ToHexString();
	}

	private static bool ShareTestChecksum(byte[] response, string targetChecksum)
	{
		return ShareGetChecksum(response) == targetChecksum;
	}

	public static async Task DeleteSharedEntryAsync(string id, string removalKey, string userAgent)
	{
		if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(removalKey))
		{
			throw new Exception("Unable to remove");
		}
		using DecompressingWebClient client = new DecompressingWebClient
		{
			Headers = 
			{
				[HttpRequestHeader.UserAgent] = userAgent,
				["X-Data-Removal-Key"] = removalKey
			}
		};
		await client.UploadDataTaskAsync(ShareServerAddress + id + "/delete", new byte[0]);
	}

	public static async Task<ShareResult> ShareEntryAsync(string typeString, string name, string target, string author, byte[] data, string userAgent, string customId, CancellationToken cancellation = default(CancellationToken))
	{
		try
		{
			using DecompressingWebClient client = new DecompressingWebClient
			{
				Headers = 
				{
					[HttpRequestHeader.UserAgent] = userAgent,
					["X-Data-Content-Type"] = typeString,
					["X-Data-Content-Name"] = name,
					["X-Data-Content-Target"] = target,
					["X-Data-Author-Name"] = author,
					["X-Data-Checksum"] = ShareGetChecksum(data)
				}
			};
			Encoding uTF = Encoding.UTF8;
			string text = uTF.GetString(await client.UploadDataTaskAsync(ShareServerAddress + (customId ?? "0") + "/post", data));
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			ShareResult shareResult = JsonConvert.DeserializeObject<ShareResult>(text);
			Logging.Write((object)$"[I] Sent {data.Length} byte(s)", "ShareEntryAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 1055);
			Logging.Write((object)("[I] ID=" + shareResult.Id + ", removal key=" + shareResult.RemovalKey), "ShareEntryAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 1056);
			return shareResult;
		}
		catch (Exception ex)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			Logging.Warning((object)("[I] Cannot upload " + typeString + ": " + ex), "ShareEntryAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 1062);
			throw;
		}
	}

	public static bool VerifySignature(string filename)
	{
		if (!filename.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || !File.Exists(filename))
		{
			return false;
		}
		using (FileStream fileStream = File.OpenRead(filename))
		{
			fileStream.Seek(-1L, SeekOrigin.End);
			if (fileStream.ReadByte() != 61)
			{
				return false;
			}
			byte[] array = new byte[fileStream.Position];
			fileStream.Seek(0L, SeekOrigin.Begin);
			if (fileStream.Read(array, 0, array.Length) != array.Length)
			{
				return false;
			}
			for (int i = 2; i < 4096; i++)
			{
				int num = array.Length - i;
				if (num < 17)
				{
					return false;
				}
				if (BitConverter.ToInt16(array, num) == i - 2)
				{
					string text = Encoding.ASCII.GetString(array, array.Length - i + 2, i - 2);
					using SHA256 sHA = SHA256.Create();
					array[^i] = 0;
					array[array.Length - i + 1] = 0;
					string text2 = Convert.ToBase64String(sHA.ComputeHash(array, 0, array.Length - i + 2));
					string value = Convert.ToBase64String(sHA.ComputeHash(Encoding.ASCII.GetBytes("9w0XsRkwGPHaUJM1" + text2)));
					return text.Contains(value);
				}
			}
		}
		return false;
	}

	public static async Task<SharedEntryLoaded> GetSharedEntryAsync(string id, string userAgent, CancellationToken cancellation = default(CancellationToken))
	{
		using DecompressingWebClient client = new DecompressingWebClient
		{
			Headers = { [HttpRequestHeader.UserAgent] = userAgent }
		};
		byte[] array;
		try
		{
			array = await client.DownloadDataTaskAsyncSafe(ShareServerAddress + id + "/get");
		}
		catch (Exception ex)
		{
			if (cancellation.IsCancellationRequested)
			{
				return null;
			}
			Logging.Warning((object)("[I] Cannot get " + id + ": " + ex), "GetSharedEntryAsync", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 1117);
			throw;
		}
		if (cancellation.IsCancellationRequested)
		{
			return null;
		}
		string text = client.ResponseHeaders.Get("X-Data-Checksum");
		if (text == null)
		{
			throw new Exception("Checksum is missing");
		}
		if (!ShareTestChecksum(array, text))
		{
			throw new Exception("Checksum doesn't match");
		}
		return new SharedEntryLoaded
		{
			Data = array,
			EntryType = client.ResponseHeaders.Get("X-Data-Content-Type"),
			Name = client.ResponseHeaders.Get("X-Data-Content-Name"),
			Target = client.ResponseHeaders.Get("X-Data-Content-Target"),
			Author = client.ResponseHeaders.Get("X-Data-Author-Name")
		};
	}

	public static string GetEmojiUrl()
	{
		return MainApiDomain + "/u/emoji/{0}?format=png";
	}

	public static string GetValuesStorageEncryptionKey()
	{
		return string.Join(" ", _valuesStorageEncryptionKey);
	}

	public static string GetOnlineInviteEncryptionKey()
	{
		return string.Join(" ", _onlineInviteEncryptionKey);
	}

	public static Tuple<string> GetPositionStackApiKey()
	{
		return new Tuple<string>("a1ad9bd67a1ec1672e7604ac4e4e31fe");
	}

	public static Tuple<string> GetYouTubeApiKey()
	{
		return new Tuple<string>("AIzaSyDCeETjwmE4mARR-Br9n8LBla8YLVDsdgc");
	}

	public static Tuple<string> GetMegaAppKey()
	{
		return new Tuple<string>("87RCjZyB");
	}

	public static Tuple<string, string> GetGoogleDriveCredentials()
	{
		return new Tuple<string, string>("90387956117916-cpr63gug8hgn524p06k733kdn5ak6n2s.apps.googleusercontent.com", "76qX8ZKMe6CJOAWv5yEOBEjpbu");
	}

	public static Tuple<string, string> GetGoogleDriveAppFolderCredentials()
	{
		return new Tuple<string, string>("89607670794295-kr7nv0v9cdct0i8e8qhst1000b49n4jo.apps.googleusercontent.com", "q7_wmE7aZnE3z8Ml4DptRq1h34");
	}

	public static Tuple<string, string> GetOneDriveCredentials()
	{
		return new Tuple<string, string>("c05117c5-81fd-46a1-a158-548c2061dd0d", "Q5w-r5YDOt90H6lcD.ZcgReOF~-Wd~m2..lh");
	}

	public static Tuple<string, string> GetOneDriveAppFolderCredentials()
	{
		return new Tuple<string, string>("e28b9ce2-067d-4034-8814-7f1f131b7d11", "UijW0HRJq73y2K7UfJ6gRUVmO");
	}

	public static Tuple<string, string> GetYandexDiskAppFolderCredentials()
	{
		return new Tuple<string, string>("e643ef735338244b5db3396f4a360cf4ae", "3b2fcfc26a465c420480443227e7b43492");
	}

	public static Tuple<string, string> GetDropboxCredentials()
	{
		return new Tuple<string, string>("7u4akp7vod4g9oxk4", "4bwduu32d3w2iael6");
	}

	public static string GetRaceUPassword(string trackId, string ip, int port)
	{
		using SHA1 sHA = SHA1.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes("fec4k4rn1a7dt4vp" + ip + port + trackId)).ToHexString().ToLowerInvariant();
	}

	public static string GetRaceUChecksum(string steamId)
	{
		using SHA1 sHA = SHA1.Create();
		return sHA.ComputeHash(Encoding.UTF8.GetBytes(steamId + "ifWV3mLzxdCxOjgaQ7na")).ToHexString().ToLowerInvariant();
	}

	public static string GetPackedSteamId(string packed, string additionalSalt)
	{
		packed = packed?.Trim();
		if (packed == null || packed.Length < 10 || packed.Length > 20)
		{
			return null;
		}
		if (packed.Length % 4 != 0)
		{
			StringBuilder stringBuilder = new StringBuilder(packed, 4);
			for (int i = 0; i < packed.Length % 4; i++)
			{
				stringBuilder.Append('=');
			}
			packed = stringBuilder.ToString();
		}
		try
		{
			byte[] array = Convert.FromBase64String(packed);
			string text = BitConverter.ToInt64(array, 0).ToString();
			if (array.Length - 4 < 8)
			{
				throw new Exception("Checksum is too short");
			}
			using SHA1 sHA = SHA1.Create();
			return sHA.ComputeHash(Encoding.UTF8.GetBytes(text + PackedSteamIdChecksumSalt + additionalSalt)).Take(array.Length - 8).SequenceEqual(array.Skip(8)) ? text : null;
		}
		catch (Exception ex)
		{
			Logging.Warning((object)("[I] Cannot get Steam ID from " + packed + ": " + ex), "GetPackedSteamId", "C:\\Development\\actools-internal\\AcManager.Internal\\InternalUtils.cs", 1246);
			return null;
		}
	}

	public static Tuple<string, string> GetLibrariesOptimizationTweak(int buildVersion)
	{
		if (buildVersion <= 2144)
		{
			return null;
		}
		if (buildVersion <= 2744)
		{
			return Tuple.Create("b2c0123c230dbfe3abbf9fbc64be5c091b0c7bbd7af7f51e1297a7199f89e15e\r\n=-- A small tweak to slightly improve performance --=\r\n\r\njit.on()\r\nfor trace in load('\\6\\7\\8\\136\\8\\2\\64\\117\\146\\1\\2\\1\\5\\0\\4\\0\\20\\25\\2\\0\\54\\1\\0\\0\\41\\3\\1\\0\\71\\4\\1\\0\\65\\1\\1\\2\\6\\1\\1\\0\\88\\1\\10\\128\\54\\1\\0\\0\\41\\3\\1\\0\\71\\4\\1\\0\\65\\1\\1\\2\\18\\3\\1\\0\\57\\1\\2\\1\\39\\4\\3\\0\\66\\1\\3\\2\\15\\0\\1\\0\\88\\2\\3\\128\\18\\1\\0\\0\\71\\3\\1\\0\\65\\1\\0\\2\\76\\1\\2\\0\\17\\91\\125\\93\\95\\91\\95\\93\\101\\120\\91\\59\\93\\9\\102\\105\\110\\100\\10\\95\\95\\101\\120\\42\\11\\115\\101\\108\\101\\99\\116\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\0\\95\\0\\0\\21\\0\\129\\5\\3\\0\\14\\0\\12\\1\\107\\138\\1\\0\\2\\54\\0\\0\\0\\54\\2\\1\\0\\66\\0\\2\\4\\72\\3\\100\\128\\58\\5\\6\\3\\7\\5\\2\\0\\88\\5\\97\\128\\52\\5\\3\\0\\62\\3\\1\\5\\51\\6\\3\\0\\62\\6\\2\\5\\18\\3\\5\\0\\52\\5\\4\\0\\58\\6\\1\\3\\62\\6\\1\\5\\58\\6\\2\\3\\18\\8\\6\\0\\57\\6\\4\\6\\54\\9\\5\\0\\54\\10\\6\\0\\18\\12\\4\\0\\66\\10\\2\\2\\58\\10\\1\\10\\18\\12\\10\\0\\57\\10\\7\\10\\41\\13\\2\\0\\66\\10\\3\\2\\39\\11\\8\\0\\38\\10\\11\\10\\56\\9\\10\\9\\58\\10\\1\\3\\58\\10\\5\\10\\39\\11\\9\\0\\38\\10\\11\\10\\56\\9\\10\\9\\66\\6\\3\\2\\62\\6\\2\\5\\58\\6\\2\\3\\18\\8\\6\\0\\57\\6\\4\\6\\54\\9\\5\\0\\54\\10\\6\\0\\18\\12\\4\\0\\66\\10\\2\\2\\58\\10\\1\\10\\18\\12\\10\\0\\57\\10\\7\\10\\41\\13\\2\\0\\66\\10\\3\\2\\39\\11\\8\\0\\38\\10\\11\\10\\56\\9\\10\\9\\58\\10\\1\\3\\58\\10\\5\\10\\39\\11\\10\\0\\38\\10\\11\\10\\56\\9\\10\\9\\66\\6\\3\\0\\63\\6\\0\\0\\18\\3\\5\\0\\18\\5\\4\\0\\44\\6\\7\\0\\88\\8\\3\\128\\54\\9\\11\\0\\18\\11\\8\\0\\66\\9\\2\\1\\69\\8\\3\\2\\82\\8\\251\\127\\54\\5\\5\\0\\54\\6\\6\\0\\18\\8\\4\\0\\66\\6\\2\\2\\58\\6\\1\\6\\18\\8\\6\\0\\57\\6\\7\\6\\41\\9\\2\\0\\66\\6\\3\\2\\39\\7\\8\\0\\38\\6\\7\\6\\56\\5\\6\\5\\58\\6\\1\\3\\58\\6\\5\\6\\39\\7\\9\\0\\38\\6\\7\\6\\54\\7\\5\\0\\54\\8\\6\\0\\18\\10\\4\\0\\66\\8\\2\\2\\58\\8\\1\\8\\18\\10\\8\\0\\57\\8\\7\\8\\41\\11\\2\\0\\66\\8\\3\\2\\39\\9\\8\\0\\38\\8\\9\\8\\56\\7\\8\\7\\58\\8\\1\\3\\58\\8\\5\\8\\39\\9\\10\\0\\38\\8\\9\\8\\58\\9\\2\\3\\58\\10\\3\\3\\60\\10\\8\\7\\60\\9\\6\\5\\70\\3\\3\\3\\82\\3\\154\\127\\75\\0\\1\\0\\12\\114\\101\\113\\117\\105\\114\\101\\8\\97\\115\\116\\8\\100\\101\\102\\6\\105\\13\\109\\117\\108\\116\\105\\112\\108\\121\\9\\116\\121\\112\\101\\7\\95\\71\\9\\98\\105\\110\\100\\0\\6\\117\\13\\95\\95\\115\\99\\114\\105\\112\\116\\10\\112\\97\\105\\114\\115\\7\\128\\128\\192\\153\\4\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\2\\4\\4\\103\\5\\0\\103\\6\\0\\103\\107\\0\\1\\100\\118\\0\\0\\100\\4\\58\\6\\5\\0\\6\\6\\0\\6\\95\\0\\1\\3\\0\\0') do\r\n  jit.flush(trace)\r\nend".Replace("\r", ""), "user.lua");
		}
		return Tuple.Create("5871bb6dfdeb51700443437299c6c686809dd54869a488a936a555b60ce46954\r\n--[[ A small tweak to slightly improve performance (for newer CSPs) ]]\r\n\r\njit.on()\r\nfor trace in load('\\114\\101\\116\\117\\114\\110\\40\\102\\117\\110\\99\\116\\105\\111\\110\\40\\46\\46\\46\\41\\108\\111\\99\\97\\108\\32\\121\\61\\123\\34\\92\\48\\56\\54\\92\\48\\53\\50\\92\\49\\50\\50\\92\\49\\48\\54\\92\\49\\48\\51\\92\\49\\49\\52\\92\\49\\48\\55\\92\\48\\52\\56\\92\\48\\55\\54\\92\\48\\56\\50\\92\\48\\56\\54\\92\\49\\48\\57\\92\\49\\48\\49\\92\\49\\49\\49\\92\\48\\55\\57\\92\\48\\56\\48\\92\\48\\52\\51\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\55\\50\\92\\49\\49\\52\\92\\49\\49\\56\\92\\49\\49\\57\\92\\48\\55\\57\\92\\49\\48\\51\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\52\\51\\92\\48\\55\\52\\92\\48\\56\\53\\92\\49\\49\\57\\92\\48\\53\\53\\92\\49\\49\\48\\92\\48\\56\\54\\92\\49\\48\\54\\92\\48\\53\\50\\92\\48\\53\\55\\92\\48\\52\\55\\92\\48\\56\\48\\92\\48\\55\\50\\92\\49\\49\\49\\92\\48\\56\\54\\92\\48\\53\\49\\92\\48\\54\\57\\92\\48\\57\\57\\92\\49\\49\\50\\92\\48\\54\\49\\34\\44\\34\\92\\48\\55\\50\\92\\49\\50\\49\\92\\48\\57\\48\\92\\48\\52\\51\\92\\49\\48\\51\\92\\48\\56\\57\\92\\48\\53\\52\\92\\48\\57\\56\\92\\48\\55\\48\\92\\48\\53\\54\\92\\48\\56\\56\\92\\48\\54\\49\\34\\44\\34\\92\\48\\55\\50\\92\\49\\49\\57\\92\\48\\56\\53\\92\\48\\55\\55\\92\\48\\55\\49\\92\\49\\48\\51\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\56\\48\\92\\48\\54\\53\\92\\48\\55\\49\\92\\48\\53\\49\\92\\48\\55\\57\\92\\48\\53\\51\\92\\49\\48\\54\\92\\48\\54\\49\\34\\44\\34\\92\\48\\55\\57\\92\\49\\49\\57\\92\\48\\55\\56\\92\\49\\50\\48\\92\\49\\48\\55\\92\\48\\56\\48\\92\\48\\56\\57\\92\\49\\49\\49\\92\\49\\48\\52\\92\\48\\55\\52\\92\\48\\52\\55\\92\\48\\55\\50\\92\\48\\54\\57\\92\\48\\56\\50\\92\\48\\53\\50\\92\\48\\55\\51\\92\\48\\56\\55\\92\\48\\55\\48\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\55\\57\\92\\48\\54\\55\\92\\48\\54\\53\\92\\49\\50\\50\\92\\48\\56\\54\\92\\49\\48\\56\\92\\49\\48\\55\\92\\48\\57\\55\\34\\44\\34\\92\\48\\54\\57\\92\\48\\55\\52\\92\\49\\50\\50\\92\\48\\53\\48\\92\\48\\52\\51\\92\\49\\48\\52\\92\\48\\55\\56\\92\\49\\48\\54\\92\\48\\56\\55\\92\\49\\48\\53\\92\\49\\50\\50\\92\\48\\53\\50\\92\\48\\55\\57\\92\\49\\49\\52\\92\\49\\48\\55\\92\\49\\50\\48\\92\\48\\56\\54\\92\\48\\56\\50\\92\\48\\54\\57\\92\\48\\54\\49\\34\\59\\34\\92\\48\\56\\52\\92\\49\\48\\48\\92\\49\\49\\51\\92\\48\\54\\55\\92\\49\\49\\52\\92\\48\\55\\57\\92\\48\\53\\55\\92\\48\\56\\51\\92\\49\\48\\50\\92\\49\\49\\56\\92\\49\\49\\50\\92\\48\\54\\49\\34\\44\\34\\92\\48\\52\\51\\92\\49\\48\\56\\92\\48\\53\\50\\92\\48\\54\\54\\34\\59\\34\\92\\48\\54\\56\\92\\48\\56\\52\\92\\48\\53\\51\\92\\49\\48\\53\\92\\49\\48\\50\\92\\48\\56\\53\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\56\\48\\92\\48\\54\\53\\92\\48\\55\\49\\92\\48\\52\\56\\92\\48\\55\\57\\92\\48\\56\\48\\92\\48\\52\\55\\92\\49\\50\\50\\92\\48\\56\\54\\92\\49\\48\\56\\92\\49\\49\\51\\92\\48\\55\\55\\92\\48\\52\\51\\92\\49\\48\\56\\92\\49\\48\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\56\\48\\92\\48\\54\\53\\92\\48\\55\\49\\92\\49\\48\\50\\92\\48\\52\\51\\92\\49\\49\\49\\92\\48\\52\\55\\92\\49\\49\\56\\92\\48\\56\\51\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\56\\54\\92\\49\\48\\56\\92\\48\\55\\49\\92\\48\\53\\55\\92\\48\\56\\54\\92\\48\\56\\50\\92\\48\\52\\57\\92\\49\\48\\50\\92\\48\\52\\51\\92\\49\\49\\49\\92\\48\\54\\57\\92\\48\\54\\49\\34\\44\\34\\92\\48\\53\\49\\92\\48\\55\\51\\92\\49\\48\\48\\92\\48\\53\\48\\92\\49\\48\\50\\92\\48\\55\\48\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\49\\48\\49\\92\\48\\55\\48\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\54\\53\\92\\48\\57\\55\\92\\48\\53\\48\\92\\48\\55\\52\\92\\48\\56\\55\\92\\48\\53\\49\\92\\48\\55\\53\\92\\48\\57\\56\\92\\49\\49\\53\\92\\48\\55\\48\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\49\\48\\54\\92\\48\\53\\55\\92\\49\\48\\51\\92\\48\\55\\57\\92\\48\\55\\51\\92\\48\\56\\57\\92\\48\\55\\50\\92\\48\\54\\49\\34\\59\\34\\92\\48\\54\\56\\92\\49\\48\\51\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\56\\54\\92\\49\\48\\56\\92\\48\\55\\49\\92\\48\\54\\54\\92\\48\\56\\54\\92\\48\\53\\51\\92\\48\\54\\53\\92\\48\\55\\55\\92\\48\\55\\57\\92\\48\\56\\48\\92\\48\\55\\56\\92\\48\\54\\49\\34\\44\\34\\92\\48\\55\\48\\92\\48\\53\\48\\92\\48\\53\\53\\92\\48\\55\\54\\92\\49\\50\\49\\92\\48\\54\\53\\92\\48\\55\\50\\92\\48\\54\\49\\34\\44\\34\\92\\48\\52\\51\\92\\48\\54\\53\\92\\48\\56\\57\\92\\49\\48\\50\\92\\48\\55\\54\\92\\49\\49\\56\\92\\49\\49\\56\\92\\49\\48\\57\\92\\48\\55\\51\\92\\48\\54\\55\\92\\48\\54\\53\\92\\48\\56\\49\\92\\49\\48\\49\\92\\48\\57\\57\\92\\49\\48\\55\\92\\48\\55\\54\\92\\48\\56\\54\\92\\48\\55\\48\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\56\\54\\92\\49\\49\\57\\92\\48\\55\\49\\92\\49\\48\\51\\92\\48\\52\\51\\92\\48\\56\\48\\92\\48\\52\\57\\92\\49\\48\\51\\92\\48\\56\\54\\92\\48\\55\\52\\92\\48\\56\\54\\92\\49\\49\\57\\92\\48\\53\\51\\92\\49\\49\\49\\92\\48\\53\\48\\92\\49\\49\\54\\92\\49\\48\\49\\92\\48\\53\\51\\92\\49\\49\\48\\92\\48\\54\\49\\34\\59\\34\\92\\48\\55\\57\\92\\49\\49\\49\\92\\49\\50\\48\\92\\49\\48\\57\\92\\48\\52\\51\\92\\49\\48\\53\\92\\48\\55\\56\\92\\48\\54\\49\\34\\59\\34\\92\\48\\53\\53\\92\\48\\55\\55\\92\\48\\56\\53\\92\\49\\49\\56\\92\\48\\55\\57\\92\\48\\57\\56\\92\\48\\57\\55\\92\\49\\48\\50\\92\\48\\53\\53\\92\\48\\56\\53\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\55\\57\\92\\49\\48\\53\\92\\49\\48\\55\\92\\48\\54\\53\\92\\48\\55\\50\\92\\48\\56\\53\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\49\\48\\49\\92\\48\\55\\52\\92\\49\\49\\48\\92\\48\\55\\48\\92\\48\\53\\50\\92\\49\\49\\56\\92\\49\\48\\50\\92\\48\\56\\55\\92\\48\\56\\54\\92\\48\\53\\51\\92\\48\\55\\57\\92\\48\\54\\54\\92\\48\\55\\54\\92\\49\\49\\49\\92\\49\\48\\50\\92\\48\\55\\52\\92\\48\\56\\51\\92\\49\\49\\50\\92\\48\\55\\54\\92\\48\\54\\49\\34\\44\\34\\92\\48\\55\\57\\92\\49\\49\\51\\92\\48\\55\\56\\92\\48\\53\\55\\92\\48\\53\\51\\92\\48\\56\\50\\92\\49\\49\\48\\92\\49\\48\\53\\92\\48\\52\\55\\92\\48\\56\\53\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\55\\48\\92\\49\\48\\51\\92\\49\\49\\54\\92\\48\\53\\49\\34\\59\\34\\92\\49\\48\\52\\92\\48\\53\\50\\92\\48\\55\\54\\92\\48\\53\\55\\92\\48\\53\\50\\92\\48\\57\\57\\92\\49\\49\\56\\92\\48\\55\\55\\92\\48\\55\\50\\92\\49\\49\\57\\92\\48\\53\\50\\92\\48\\55\\55\\92\\48\\55\\57\\92\\49\\48\\53\\92\\49\\50\\50\\92\\49\\48\\56\\92\\49\\48\\49\\92\\49\\49\\48\\92\\48\\55\\54\\92\\48\\54\\49\\34\\59\\34\\92\\49\\48\\55\\92\\49\\48\\53\\92\\48\\52\\55\\92\\48\\54\\53\\92\\48\\52\\55\\92\\48\\55\\52\\92\\48\\55\\57\\92\\48\\53\\49\\92\\48\\55\\54\\92\\49\\49\\52\\92\\49\\49\\56\\92\\49\\48\\56\\92\\48\\56\\51\\92\\49\\48\\52\\92\\49\\49\\51\\92\\48\\55\\51\\34\\59\\34\\92\\49\\48\\49\\92\\49\\49\\52\\92\\48\\52\\57\\92\\49\\49\\57\\92\\48\\52\\51\\92\\49\\48\\53\\92\\49\\49\\51\\92\\48\\53\\55\\92\\48\\54\\57\\92\\48\\54\\55\\92\\48\\56\\54\\92\\49\\49\\54\\92\\48\\54\\57\\92\\48\\55\\52\\92\\48\\53\\50\\92\\48\\55\\57\\92\\48\\56\\55\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\49\\48\\51\\92\\48\\56\\51\\92\\48\\57\\57\\92\\49\\49\\57\\92\\48\\55\\53\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\49\\48\\52\\92\\49\\48\\56\\92\\48\\54\\57\\92\\48\\54\\55\\92\\49\\48\\55\\92\\48\\54\\53\\92\\49\\49\\56\\92\\48\\56\\52\\92\\48\\55\\57\\92\\48\\53\\51\\92\\49\\50\\48\\92\\48\\56\\48\\92\\48\\56\\55\\92\\48\\54\\55\\92\\49\\49\\51\\92\\48\\55\\48\\92\\48\\52\\55\\92\\48\\55\\48\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\53\\50\\92\\49\\48\\56\\92\\49\\49\\51\\92\\48\\52\\56\\92\\48\\54\\57\\92\\49\\48\\56\\92\\48\\53\\50\\92\\48\\56\\52\\92\\48\\55\\56\\92\\49\\49\\48\\92\\48\\52\\55\\92\\49\\49\\56\\92\\48\\56\\54\\92\\49\\48\\56\\92\\48\\53\\50\\92\\48\\55\\52\\92\\48\\56\\54\\92\\49\\48\\56\\92\\48\\53\\50\\92\\49\\49\\50\\92\\48\\55\\56\\92\\49\\48\\51\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\55\\50\\92\\48\\53\\55\\92\\48\\56\\57\\92\\48\\53\\50\\92\\49\\48\\52\\92\\48\\57\\57\\92\\48\\53\\50\\92\\49\\48\\57\\92\\49\\48\\51\\92\\48\\55\\52\\92\\48\\56\\54\\92\\49\\48\\49\\92\\48\\52\\55\\92\\48\\57\\57\\92\\48\\52\\55\\92\\49\\48\\57\\92\\49\\48\\52\\92\\49\\49\\50\\92\\49\\48\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\55\\57\\92\\48\\56\\48\\92\\48\\52\\57\\92\\48\\56\\52\\92\\48\\52\\51\\92\\49\\48\\53\\92\\48\\55\\56\\92\\48\\54\\49\\34\\59\\34\\92\\48\\56\\52\\92\\49\\49\\51\\92\\48\\53\\51\\92\\49\\49\\50\\92\\48\\53\\49\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\52\\51\\92\\48\\53\\51\\92\\49\\49\\51\\92\\49\\49\\57\\92\\48\\55\\51\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\56\\54\\92\\48\\53\\51\\92\\48\\53\\48\\92\\48\\55\\48\\92\\48\\55\\50\\92\\48\\53\\51\\92\\49\\48\\55\\92\\49\\49\\54\\34\\44\\34\\92\\48\\54\\57\\92\\48\\54\\55\\92\\48\\53\\50\\92\\49\\49\\57\\92\\48\\52\\51\\92\\48\\53\\51\\92\\48\\53\\50\\92\\49\\49\\57\\92\\48\\55\\50\\92\\48\\56\\48\\92\\48\\52\\55\\92\\49\\50\\50\\92\\48\\55\\50\\92\\49\\49\\49\\92\\49\\50\\48\\92\\49\\49\\56\\34\\44\\34\\34\\59\\34\\92\\48\\56\\52\\92\\48\\55\\51\\92\\48\\57\\48\\92\\48\\55\\51\\92\\49\\50\\48\\92\\49\\49\\56\\92\\49\\48\\57\\92\\48\\53\\48\\34\\59\\34\\92\\48\\54\\57\\92\\49\\48\\53\\92\\48\\52\\55\\92\\48\\56\\52\\92\\48\\55\\51\\92\\48\\53\\51\\92\\48\\53\\48\\92\\49\\49\\52\\34\\44\\34\\92\\48\\52\\51\\92\\48\\57\\57\\92\\49\\49\\48\\92\\48\\54\\49\\34\\59\\34\\92\\49\\48\\49\\92\\48\\54\\56\\92\\49\\49\\49\\92\\49\\48\\54\\92\\49\\49\\53\\92\\48\\53\\55\\92\\49\\50\\48\\92\\49\\48\\54\\34\\59\\34\\92\\49\\48\\52\\92\\49\\49\\50\\92\\48\\52\\56\\92\\48\\55\\50\\92\\49\\48\\49\\92\\49\\49\\52\\92\\48\\52\\55\\92\\49\\49\\55\\92\\48\\52\\55\\92\\48\\53\\51\\92\\49\\48\\55\\92\\48\\55\\50\\92\\48\\53\\51\\92\\48\\53\\51\\92\\48\\56\\54\\92\\49\\49\\57\\34\\44\\34\\92\\48\\53\\53\\92\\48\\56\\53\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\52\\51\\92\\48\\57\\57\\92\\48\\55\\56\\92\\48\\54\\49\\34\\59\\34\\92\\48\\56\\54\\92\\48\\56\\51\\92\\48\\55\\48\\92\\49\\49\\49\\92\\48\\55\\51\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\54\\57\\92\\49\\49\\49\\92\\48\\53\\50\\92\\48\\52\\56\\92\\48\\52\\51\\92\\49\\48\\53\\92\\48\\55\\57\\92\\49\\49\\56\\34\\59\\34\\92\\48\\55\\50\\92\\49\\49\\52\\92\\48\\55\\50\\92\\49\\50\\48\\92\\48\\52\\51\\92\\49\\48\\52\\92\\49\\50\\50\\92\\49\\49\\54\\92\\48\\55\\57\\92\\48\\57\\57\\92\\49\\49\\56\\92\\49\\49\\51\\92\\48\\53\\51\\92\\48\\53\\50\\92\\49\\49\\56\\92\\49\\49\\57\\34\\59\\34\\92\\48\\55\\52\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\54\\57\\92\\49\\49\\49\\92\\49\\49\\51\\92\\48\\54\\54\\92\\48\\55\\57\\92\\49\\48\\56\\92\\48\\55\\49\\92\\48\\52\\56\\34\\44\\34\\92\\48\\55\\50\\92\\48\\54\\55\\92\\48\\55\\49\\92\\48\\54\\54\\92\\48\\55\\50\\92\\48\\54\\55\\92\\49\\49\\51\\92\\49\\49\\57\\34\\59\\34\\92\\48\\52\\51\\92\\49\\49\\52\\92\\49\\49\\48\\92\\49\\49\\52\\92\\49\\48\\54\\92\\48\\56\\52\\92\\49\\48\\50\\92\\48\\52\\57\\92\\48\\54\\54\\92\\48\\52\\57\\92\\48\\56\\57\\92\\49\\49\\50\\92\\48\\57\\55\\92\\48\\53\\53\\92\\48\\54\\57\\92\\48\\54\\49\\34\\44\\34\\92\\48\\52\\51\\92\\48\\54\\55\\92\\48\\52\\56\\92\\48\\53\\51\\92\\48\\54\\57\\92\\48\\55\\52\\92\\49\\49\\48\\92\\48\\54\\49\\34\\59\\34\\92\\48\\54\\57\\92\\49\\48\\56\\92\\49\\49\\51\\92\\49\\48\\50\\92\\48\\54\\57\\92\\49\\49\\52\\92\\48\\55\\54\\92\\48\\54\\49\\34\\44\\34\\92\\48\\53\\50\\92\\48\\53\\51\\92\\48\\53\\48\\92\\48\\55\\52\\92\\48\\53\\51\\92\\49\\48\\52\\92\\48\\52\\55\\92\\49\\48\\54\\92\\48\\55\\51\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\49\\48\\55\\92\\49\\49\\51\\92\\48\\56\\57\\92\\49\\49\\55\\92\\49\\48\\55\\92\\48\\53\\55\\92\\49\\48\\55\\92\\48\\56\\52\\92\\48\\55\\50\\92\\49\\49\\52\\92\\49\\50\\50\\92\\48\\55\\50\\92\\48\\55\\54\\92\\48\\54\\55\\92\\49\\48\\50\\92\\48\\55\\54\\92\\48\\52\\55\\92\\48\\55\\48\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\49\\48\\55\\92\\49\\49\\49\\92\\49\\49\\51\\92\\48\\57\\57\\92\\48\\54\\57\\92\\49\\48\\56\\92\\49\\50\\50\\92\\49\\48\\53\\92\\48\\53\\51\\92\\49\\49\\52\\92\\49\\48\\55\\92\\48\\55\\48\\92\\48\\53\\50\\92\\49\\49\\52\\92\\48\\55\\57\\92\\48\\53\\54\\34\\59\\34\\92\\48\\56\\54\\92\\49\\48\\56\\92\\49\\49\\51\\92\\48\\55\\55\\92\\48\\52\\51\\92\\49\\48\\56\\92\\49\\48\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\54\\56\\92\\49\\49\\50\\92\\48\\53\\53\\92\\48\\55\\53\\92\\49\\48\\50\\92\\48\\55\\57\\92\\48\\55\\54\\92\\48\\54\\49\\34\\44\\34\\92\\48\\54\\54\\92\\49\\49\\51\\92\\48\\57\\57\\92\\48\\53\\51\\92\\49\\49\\57\\92\\48\\56\\56\\92\\49\\48\\53\\92\\48\\56\\48\\92\\48\\54\\54\\92\\48\\56\\53\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\56\\48\\92\\48\\54\\53\\92\\48\\55\\49\\92\\49\\49\\52\\92\\48\\55\\50\\92\\48\\55\\48\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\54\\57\\92\\49\\48\\53\\92\\48\\53\\50\\92\\48\\55\\55\\34\\44\\34\\92\\48\\56\\51\\92\\49\\48\\54\\92\\49\\48\\49\\92\\48\\55\\54\\92\\48\\55\\55\\92\\48\\52\\57\\92\\48\\56\\53\\92\\48\\55\\50\\92\\49\\49\\50\\92\\48\\54\\57\\92\\48\\55\\57\\92\\48\\52\\57\\92\\48\\54\\56\\92\\48\\56\\56\\92\\49\\50\\49\\92\\48\\52\\51\\92\\48\\53\\48\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\44\\34\\92\\48\\53\\53\\92\\48\\54\\56\\92\\48\\55\\54\\92\\48\\56\\54\\92\\48\\57\\48\\92\\48\\52\\57\\92\\49\\49\\57\\92\\48\\54\\49\\34\\59\\34\\92\\49\\49\\49\\92\\48\\52\\51\\92\\49\\50\\49\\92\\48\\54\\53\\92\\49\\49\\53\\92\\48\\56\\56\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\48\\56\\51\\92\\48\\53\\48\\92\\49\\49\\54\\92\\48\\55\\57\\92\\48\\53\\53\\92\\49\\49\\48\\92\\48\\55\\52\\92\\48\\52\\51\\92\\48\\53\\54\\92\\48\\56\\51\\92\\48\\55\\48\\92\\48\\53\\51\\34\\44\\34\\92\\48\\56\\48\\92\\49\\49\\57\\92\\48\\54\\57\\92\\48\\54\\49\\34\\59\\34\\92\\48\\55\\50\\92\\48\\53\\51\\92\\48\\55\\54\\92\\48\\54\\49\\34\\44\\34\\92\\48\\53\\53\\92\\48\\56\\50\\92\\48\\55\\56\\92\\49\\48\\53\\92\\48\\52\\51\\92\\49\\49\\52\\92\\49\\50\\50\\92\\48\\57\\56\\92\\48\\53\\53\\92\\49\\48\\52\\92\\48\\56\\57\\92\\49\\48\\51\\92\\48\\53\\50\\92\\48\\53\\51\\92\\48\\53\\48\\92\\48\\54\\54\\34\\44\\34\\92\\48\\55\\50\\92\\48\\54\\55\\92\\49\\50\\50\\92\\49\\50\\50\\92\\48\\54\\57\\92\\48\\56\\53\\92\\48\\54\\49\\92\\48\\54\\49\\34\\59\\34\\92\\49\\48\\49\\92\\48\\56\\51\\92\\49\\49\\57\\92\\49\\48\\57\\92\\49\\49\\51\\92\\49\\48\\53\\92\\48\\54\\57\\92\\49\\48\\55\\34\\59\\34\\92\\48\\54\\57\\92\\49\\48\\56\\92\\49\\48\\55\\92\\49\\50\\50\\92\\48\\52\\51\\92\\49\\48\\56\\92\\48\\55\\48\\92\\48\\54\\49\\34\\125\\102\\111\\114\\32\\98\\44\\113\\32\\105\\110\\32\\105\\112\\97\\105\\114\\115\\40\\123\\123\\53\\51\\51\\57\\53\\51\\45\\53\\51\\51\\57\\53\\50\\44\\45\\55\\57\\52\\53\\57\\49\\45\\40\\45\\55\\57\\52\\54\\54\\56\\41\\125\\44\\123\\53\\51\\50\\54\\49\\57\\45\\40\\45\\53\\50\\51\\50\\50\\56\\43\\49\\48\\53\\53\\56\\52\\54\\41\\59\\54\\50\\55\\51\\49\\54\\43\\45\\54\\50\\55\\50\\53\\57\\125\\44\\123\\45\\54\\49\\55\\54\\57\\49\\45\\40\\45\\54\\49\\55\\55\\52\\57\\41\\44\\45\\52\\52\\52\\53\\57\\57\\43\\52\\52\\52\\54\\55\\54\\125\\125\\41\\100\\111\\32\\119\\104\\105\\108\\101\\32\\113\\91\\53\\53\\49\\57\\55\\49\\43\\45\\53\\53\\49\\57\\55\\48\\93\\60\\113\\91\\50\\49\\51\\52\\57\\49\\43\\45\\50\\49\\51\\52\\56\\57\\93\\100\\111\\32\\121\\91\\113\\91\\51\\57\\56\\53\\49\\48\\43\\45\\51\\57\\56\\53\\48\\57\\93\\93\\44\\121\\91\\113\\91\\45\\50\\56\\55\\48\\57\\45\\40\\45\\50\\56\\55\\49\\49\\41\\93\\93\\44\\113\\91\\45\\51\\49\\52\\54\\49\\53\\43\\51\\49\\52\\54\\49\\54\\93\\44\\113\\91\\55\\54\\56\\51\\56\\55\\45\\55\\54\\56\\51\\56\\53\\93\\61\\121\\91\\113\\91\\45\\49\\48\\51\\56\\49\\53\\51\\45\\40\\45\\49\\48\\51\\56\\49\\53\\53\\41\\93\\93\\44\\121\\91\\113\\91\\40\\45\\51\\56\\54\\54\\48\\50\\45\\40\\45\\55\\55\\53\\55\\49\\55\\41\\41\\43\\45\\51\\56\\57\\49\\49\\52\\93\\93\\44\\113\\91\\45\\50\\57\\53\\51\\55\\49\\45\\40\\45\\50\\57\\53\\51\\55\\50\\41\\93\\43\\40\\56\\54\\55\\50\\53\\54\\45\\56\\54\\55\\50\\53\\53\\41\\44\\113\\91\\45\\50\\55\\49\\48\\53\\52\\43\\50\\55\\49\\48\\53\\54\\93\\45\\40\\54\\52\\48\\55\\54\\55\\43\\45\\54\\52\\48\\55\\54\\54\\41\\101\\110\\100\\32\\101\\110\\100\\32\\108\\111\\99\\97\\108\\32\\102\\117\\110\\99\\116\\105\\111\\110\\32\\98\\40\\98\\41\\114\\101\\116\\117\\114\\110\\32\\121\\91\\98\\45\\40\\45\\53\\49\\57\\57\\54\\50\\45\\40\\45\\53\\52\\52\\52\\48\\54\\41\\41\\93\\101\\110\\100\\32\\100\\111\\32\\108\\111\\99\\97\\108\\32\\98\\61\\116\\121\\112\\101\\32\\108\\111\\99\\97\\108\\32\\113\\61\\123\\81\\61\\40\\57\\55\\56\\53\\57\\50\\43\\45\\52\\48\\55\\55\\57\\55\\41\\43\\45\\53\\55\\48\\55\\56\\52\\59\\102\\61\\53\\51\\53\\49\\51\\54\\43\\45\\53\\51\\53\\48\\57\\53\\59\\106\\61\\51\\49\\55\\53\\54\\51\\45\\40\\52\\51\\50\\56\\49\\50\\43\\45\\49\\49\\53\\51\\48\\53\\41\\44\\71\\61\\57\\50\\52\\53\\51\\57\\45\\57\\50\\52\\52\\55\\56\\59\\91\\34\\92\\48\\52\\51\\34\\93\\61\\45\\53\\51\\50\\54\\56\\48\\45\\40\\45\\53\\51\\50\\55\\48\\55\\41\\44\\66\\61\\45\\51\\56\\54\\54\\52\\51\\43\\51\\56\\54\\54\\56\\57\\59\\107\\61\\40\\45\\54\\51\\53\\50\\49\\57\\45\\40\\45\\50\\48\\48\\48\\50\\50\\41\\41\\45\\40\\45\\52\\51\\53\\50\\49\\48\\41\\44\\98\\61\\54\\54\\56\\53\\50\\52\\43\\45\\54\\54\\56\\53\\50\\50\\59\\91\\34\\92\\48\\52\\56\\34\\93\\61\\40\\49\\48\\57\\55\\52\\57\\51\\45\\52\\51\\49\\54\\54\\54\\41\\45\\54\\54\\53\\55\\56\\50\\44\\91\\34\\92\\48\\53\\50\\34\\93\\61\\50\\52\\51\\50\\53\\43\\40\\45\\57\\55\\50\\48\\57\\49\\45\\40\\45\\57\\52\\55\\55\\56\\55\\41\\41\\59\\72\\61\\50\\48\\55\\49\\53\\48\\43\\45\\50\\48\\55\\49\\50\\54\\59\\118\\61\\57\\55\\48\\48\\57\\50\\43\\45\\57\\55\\48\\48\\53\\53\\59\\69\\61\\45\\57\\56\\48\\50\\52\\49\\43\\57\\56\\48\\50\\54\\57\\44\\104\\61\\50\\53\\56\\54\\48\\56\\45\\50\\53\\56\\53\\56\\57\\44\\111\\61\\49\\48\\50\\57\\54\\54\\57\\43\\45\\49\\48\\50\\57\\54\\51\\49\\44\\91\\34\\92\\48\\53\\54\\34\\93\\61\\40\\45\\49\\49\\51\\48\\49\\55\\45\\50\\55\\56\\56\\57\\52\\41\\43\\51\\57\\49\\57\\50\\54\\44\\108\\61\\49\\48\\56\\56\\51\\50\\43\\45\\49\\48\\56\\56\\50\\54\\59\\80\\61\\45\\50\\55\\51\\56\\51\\49\\43\\50\\55\\51\\56\\53\\52\\44\\112\\61\\49\\48\\51\\53\\57\\57\\56\\43\\40\\45\\54\\52\\50\\52\\55\\54\\45\\51\\57\\51\\52\\56\\54\\41\\59\\77\\61\\45\\55\\54\\54\\50\\55\\49\\45\\40\\45\\55\\54\\54\\51\\48\\53\\41\\59\\91\\34\\92\\48\\52\\55\\34\\93\\61\\49\\54\\51\\57\\53\\57\\43\\45\\49\\54\\51\\57\\52\\50\\44\\99\\61\\50\\49\\55\\56\\56\\50\\45\\50\\49\\55\\56\\55\\57\\44\\82\\61\\45\\49\\56\\54\\56\\51\\50\\43\\49\\56\\54\\56\\51\\57\\44\\91\\34\\92\\48\\53\\49\\34\\93\\61\\56\\54\\51\\48\\50\\57\\45\\56\\54\\50\\57\\56\\53\\44\\91\\34\\92\\48\\53\\48\\34\\93\\61\\52\\57\\53\\51\\48\\56\\43\\40\\50\\55\\49\\54\\55\\54\\45\\40\\40\\45\\52\\54\\51\\55\\52\\54\\43\\40\\40\\49\\54\\49\\56\\57\\51\\45\\53\\52\\48\\56\\49\\54\\41\\45\\49\\55\\51\\49\\53\\49\\41\\41\\43\\40\\45\\56\\56\\54\\56\\48\\52\\43\\50\\54\\54\\57\\53\\53\\49\\41\\41\\41\\44\\65\\61\\45\\53\\52\\48\\53\\50\\48\\45\\40\\45\\53\\52\\48\\53\\55\\51\\41\\59\\91\\34\\92\\48\\52\\57\\34\\93\\61\\52\\55\\51\\48\\53\\50\\43\\45\\52\\55\\51\\48\\52\\51\\59\\103\\61\\52\\55\\50\\49\\48\\55\\45\\52\\55\\50\\48\\57\\49\\59\\88\\61\\45\\55\\50\\50\\52\\50\\49\\43\\55\\50\\50\\52\\50\\49\\59\\75\\61\\40\\45\\51\\57\\54\\48\\57\\50\\45\\40\\45\\49\\49\\51\\54\\57\\48\\52\\45\\40\\45\\52\\52\\49\\49\\49\\51\\41\\41\\41\\43\\45\\50\\57\\57\\54\\51\\55\\44\\68\\61\\45\\56\\48\\54\\49\\55\\55\\43\\56\\48\\54\\50\\51\\53\\59\\74\\61\\51\\48\\50\\53\\51\\57\\43\\45\\51\\48\\50\\53\\48\\52\\44\\113\\61\\45\\57\\52\\52\\50\\55\\57\\43\\57\\52\\52\\50\\56\\52\\59\\119\\61\\50\\57\\54\\48\\55\\49\\45\\50\\57\\54\\48\\49\\57\\59\\73\\61\\45\\55\\53\\57\\53\\48\\45\\40\\45\\55\\53\\57\\55\\54\\41\\44\\117\\61\\50\\50\\54\\49\\54\\55\\45\\50\\50\\54\\49\\50\\53\\59\\79\\61\\53\\54\\48\\50\\56\\50\\45\\53\\54\\48\\50\\53\\55\\59\\70\\61\\40\\52\\53\\48\\52\\49\\54\\45\\40\\45\\50\\52\\57\\52\\51\\50\\41\\41\\43\\45\\54\\57\\57\\56\\48\\48\\44\\67\\61\\45\\51\\51\\49\\54\\51\\50\\43\\51\\51\\49\\54\\56\\54\\59\\120\\61\\40\\45\\53\\54\\53\\51\\57\\56\\43\\40\\45\\55\\53\\50\\51\\56\\56\\45\\40\\45\\51\\56\\50\\49\\50\\49\\41\\41\\41\\43\\40\\49\\57\\49\\56\\53\\48\\51\\45\\57\\56\\50\\55\\56\\57\\41\\44\\110\\61\\52\\54\\51\\57\\50\\51\\45\\52\\54\\51\\57\\49\\57\\44\\97\\61\\45\\49\\50\\51\\51\\52\\53\\43\\49\\50\\51\\51\\56\\53\\44\\91\\34\\92\\48\\53\\51\\34\\93\\61\\54\\56\\56\\48\\54\\55\\43\\45\\54\\56\\56\\48\\52\\53\\44\\85\\61\\45\\54\\54\\53\\50\\56\\57\\45\\40\\45\\54\\54\\53\\51\\50\\49\\41\\44\\101\\61\\49\\49\\55\\53\\57\\56\\45\\49\\49\\55\\53\\55\\56\\59\\89\\61\\40\\55\\55\\57\\52\\48\\48\\43\\45\\49\\49\\51\\55\\51\\48\\51\\41\\43\\51\\53\\55\\57\\48\\52\\44\\122\\61\\45\\53\\55\\56\\53\\52\\57\\43\\53\\55\\56\\53\\56\\50\\59\\90\\61\\45\\51\\57\\56\\50\\57\\49\\45\\40\\45\\51\\57\\56\\51\\50\\50\\41\\59\\109\\61\\45\\49\\52\\48\\57\\50\\50\\45\\40\\45\\49\\52\\48\\57\\54\\57\\41\\44\\114\\61\\52\\53\\49\\50\\52\\54\\43\\45\\52\\53\\49\\50\\48\\55\\59\\116\\61\\45\\57\\51\\55\\51\\51\\50\\43\\57\\51\\55\\51\\55\\53\\59\\115\\61\\40\\49\\48\\48\\48\\52\\56\\43\\50\\48\\55\\48\\52\\53\\41\\45\\51\\48\\55\\48\\51\\48\\59\\83\\61\\50\\57\\55\\55\\53\\49\\45\\50\\57\\55\\55\\50\\49\\59\\76\\61\\45\\53\\56\\56\\51\\57\\51\\45\\40\\45\\49\\57\\49\\53\\51\\55\\45\\51\\57\\54\\56\\54\\56\\41\\44\\84\\61\\45\\49\\48\\50\\49\\52\\49\\57\\45\\40\\45\\49\\48\\50\\49\\52\\54\\57\\41\\44\\121\\61\\45\\51\\51\\55\\53\\56\\51\\45\\40\\45\\51\\51\\55\\54\\52\\50\\41\\44\\100\\61\\40\\45\\49\\57\\54\\55\\51\\48\\43\\40\\53\\54\\54\\52\\49\\50\\43\\45\\49\\48\\57\\50\\50\\48\\48\\41\\41\\45\\40\\45\\49\\50\\55\\57\\49\\57\\49\\45\\40\\52\\54\\55\\56\\53\\56\\43\\45\\49\\48\\50\\52\\52\\55\\49\\41\\41\\44\\87\\61\\45\\57\\54\\49\\48\\50\\49\\43\\57\\54\\49\\48\\51\\57\\59\\91\\34\\92\\48\\53\\53\\34\\93\\61\\40\\49\\50\\49\\56\\55\\52\\56\\45\\57\\56\\49\\57\\55\\49\\41\\43\\45\\50\\51\\54\\55\\54\\51\\44\\105\\61\\45\\56\\55\\51\\53\\52\\49\\43\\40\\45\\56\\48\\51\\53\\54\\49\\43\\49\\54\\55\\55\\49\\53\\55\\41\\44\\91\\34\\92\\48\\53\\55\\34\\93\\61\\40\\45\\51\\49\\53\\48\\55\\48\\43\\45\\54\\52\\53\\54\\50\\48\\41\\45\\40\\45\\57\\54\\48\\55\\52\\49\\41\\59\\91\\34\\92\\48\\53\\52\\34\\93\\61\\49\\48\\50\\55\\43\\45\\49\\48\\49\\55\\59\\86\\61\\40\\45\\55\\51\\54\\49\\52\\49\\45\\40\\45\\53\\49\\53\\51\\49\\50\\45\\40\\45\\51\\50\\53\\52\\57\\50\\41\\41\\41\\45\\40\\45\\53\\52\\54\\51\\53\\48\\41\\44\\78\\61\\40\\57\\48\\55\\57\\52\\43\\55\\50\\54\\49\\49\\41\\45\\49\\54\\51\\51\\57\\55\\125\\108\\111\\99\\97\\108\\32\\89\\61\\116\\97\\98\\108\\101\\46\\105\\110\\115\\101\\114\\116\\32\\108\\111\\99\\97\\108\\32\\108\\61\\121\\32\\108\\111\\99\\97\\108\\32\\83\\61\\115\\116\\114\\105\\110\\103\\46\\115\\117\\98\\32\\108\\111\\99\\97\\108\\32\\122\\61\\115\\116\\114\\105\\110\\103\\46\\99\\104\\97\\114\\32\\108\\111\\99\\97\\108\\32\\102\\61\\116\\97\\98\\108\\101\\46\\99\\111\\110\\99\\97\\116\\32\\108\\111\\99\\97\\108\\32\\81\\61\\109\\97\\116\\104\\46\\102\\108\\111\\111\\114\\32\\108\\111\\99\\97\\108\\32\\100\\61\\115\\116\\114\\105\\110\\103\\46\\108\\101\\110\\32\\102\\111\\114\\32\\121\\61\\45\\55\\53\\50\\53\\49\\48\\45\\40\\45\\55\\53\\50\\53\\49\\49\\41\\44\\35\\108\\44\\51\\56\\51\\53\\55\\49\\45\\40\\55\\50\\54\\49\\56\\54\\43\\45\\51\\52\\50\\54\\49\\54\\41\\100\\111\\32\\108\\111\\99\\97\\108\\32\\72\\61\\108\\91\\121\\93\\105\\102\\32\\98\\40\\72\\41\\61\\61\\34\\92\\49\\49\\53\\92\\49\\49\\54\\92\\49\\49\\52\\92\\49\\48\\53\\92\\49\\49\\48\\92\\49\\48\\51\\34\\116\\104\\101\\110\\32\\108\\111\\99\\97\\108\\32\\98\\61\\100\\40\\72\\41\\108\\111\\99\\97\\108\\32\\75\\61\\123\\125\\108\\111\\99\\97\\108\\32\\84\\61\\40\\45\\55\\52\\53\\49\\54\\45\\55\\53\\56\\51\\56\\48\\41\\43\\56\\51\\50\\56\\57\\55\\32\\108\\111\\99\\97\\108\\32\\118\\61\\40\\56\\52\\54\\50\\56\\57\\43\\45\\49\\48\\48\\56\\55\\48\\55\\41\\43\\49\\54\\50\\52\\49\\56\\32\\108\\111\\99\\97\\108\\32\\82\\61\\45\\51\\50\\57\\54\\50\\45\\40\\45\\51\\50\\57\\54\\50\\41\\119\\104\\105\\108\\101\\32\\84\\60\\61\\98\\32\\100\\111\\32\\108\\111\\99\\97\\108\\32\\121\\61\\83\\40\\72\\44\\84\\44\\84\\41\\108\\111\\99\\97\\108\\32\\108\\61\\113\\91\\121\\93\\105\\102\\32\\108\\32\\116\\104\\101\\110\\32\\118\\61\\118\\43\\108\\42\\40\\40\\40\\45\\49\\52\\57\\55\\55\\53\\54\\45\\40\\45\\52\\53\\49\\56\\57\\52\\45\\53\\55\\48\\56\\56\\54\\41\\41\\43\\49\\49\\50\\51\\53\\51\\51\\41\\43\\45\\54\\52\\56\\52\\57\\51\\41\\94\\40\\40\\53\\56\\57\\56\\55\\54\\45\\40\\57\\52\\55\\57\\48\\50\\45\\51\\53\\56\\48\\50\\57\\41\\41\\45\\82\\41\\82\\61\\82\\43\\40\\51\\50\\52\\57\\52\\49\\43\\45\\51\\50\\52\\57\\52\\48\\41\\105\\102\\32\\82\\61\\61\\45\\54\\54\\49\\57\\56\\50\\45\\40\\45\\54\\54\\49\\57\\56\\54\\41\\116\\104\\101\\110\\32\\82\\61\\45\\53\\49\\50\\55\\55\\51\\45\\40\\45\\53\\49\\50\\55\\55\\51\\41\\108\\111\\99\\97\\108\\32\\121\\61\\81\\40\\118\\47\\40\\54\\53\\50\\52\\53\\57\\43\\45\\53\\56\\54\\57\\50\\51\\41\\41\\108\\111\\99\\97\\108\\32\\98\\61\\81\\40\\40\\118\\37\\40\\45\\52\\52\\55\\55\\53\\57\\43\\53\\49\\51\\50\\57\\53\\41\\41\\47\\40\\40\\40\\49\\48\\50\\53\\55\\51\\57\\45\\40\\45\\53\\49\\49\\53\\55\\54\\41\\41\\45\\55\\56\\56\\57\\50\\50\\41\\45\\55\\52\\56\\49\\51\\55\\41\\41\\108\\111\\99\\97\\108\\32\\113\\61\\118\\37\\40\\45\\52\\53\\56\\48\\50\\52\\45\\40\\45\\52\\53\\56\\50\\56\\48\\41\\41\\89\\40\\75\\44\\122\\40\\121\\44\\98\\44\\113\\41\\41\\118\\61\\45\\55\\56\\54\\57\\54\\45\\40\\45\\55\\56\\54\\57\\54\\41\\101\\110\\100\\32\\101\\108\\115\\101\\105\\102\\32\\121\\61\\61\\34\\92\\48\\54\\49\\34\\116\\104\\101\\110\\32\\89\\40\\75\\44\\122\\40\\81\\40\\118\\47\\40\\49\\48\\52\\57\\48\\55\\48\\45\\57\\56\\51\\53\\51\\52\\41\\41\\41\\41\\105\\102\\32\\84\\62\\61\\98\\32\\111\\114\\32\\83\\40\\72\\44\\84\\43\\40\\45\\49\\48\\50\\56\\57\\50\\49\\43\\49\\48\\50\\56\\57\\50\\50\\41\\44\\84\\43\\40\\51\\54\\52\\48\\56\\50\\45\\40\\40\\45\\49\\55\\55\\55\\51\\53\\43\\45\\54\\51\\51\\53\\51\\52\\41\\43\\40\\56\\55\\50\\48\\48\\50\\45\\40\\45\\51\\48\\51\\51\\52\\56\\41\\41\\41\\41\\41\\126\\61\\34\\92\\48\\54\\49\\34\\116\\104\\101\\110\\32\\89\\40\\75\\44\\122\\40\\81\\40\\40\\118\\37\\40\\49\\48\\50\\52\\53\\48\\57\\45\\57\\53\\56\\57\\55\\51\\41\\41\\47\\40\\40\\40\\52\\57\\57\\51\\55\\54\\43\\50\\51\\55\\55\\56\\49\\41\\43\\40\\55\\53\\51\\53\\49\\48\\43\\45\\55\\52\\48\\50\\52\\54\\41\\41\\45\\40\\40\\54\\51\\55\\57\\55\\43\\55\\55\\50\\48\\50\\41\\43\\54\\48\\57\\49\\54\\54\\41\\41\\41\\41\\41\\101\\110\\100\\32\\98\\114\\101\\97\\107\\32\\101\\110\\100\\32\\84\\61\\84\\43\\40\\53\\56\\49\\48\\48\\56\\43\\45\\53\\56\\49\\48\\48\\55\\41\\101\\110\\100\\32\\108\\91\\121\\93\\61\\102\\40\\75\\41\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\40\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\89\\44\\108\\44\\83\\44\\122\\44\\102\\44\\81\\44\\112\\44\\69\\44\\74\\44\\118\\44\\87\\44\\79\\44\\67\\44\\113\\44\\84\\44\\100\\44\\76\\44\\75\\44\\103\\44\\120\\44\\82\\44\\72\\41\\74\\44\\112\\44\\84\\44\\79\\44\\67\\44\\87\\44\\118\\44\\82\\44\\72\\44\\113\\44\\76\\44\\75\\44\\103\\44\\69\\44\\120\\44\\100\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\98\\41\\108\\111\\99\\97\\108\\32\\89\\61\\118\\40\\98\\41\\108\\111\\99\\97\\108\\32\\108\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\46\\46\\46\\41\\114\\101\\116\\117\\114\\110\\32\\113\\40\\121\\44\\123\\46\\46\\46\\125\\44\\98\\44\\89\\41\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\108\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\98\\41\\108\\111\\99\\97\\108\\32\\89\\61\\118\\40\\98\\41\\108\\111\\99\\97\\108\\32\\108\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\108\\44\\83\\41\\114\\101\\116\\117\\114\\110\\32\\113\\40\\121\\44\\123\\108\\44\\83\\125\\44\\98\\44\\89\\41\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\108\\32\\101\\110\\100\\44\\45\\57\\54\\50\\55\\54\\49\\43\\57\\54\\50\\55\\54\\49\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\41\\72\\91\\121\\93\\61\\72\\91\\121\\93\\45\\40\\40\\45\\51\\50\\49\\55\\51\\57\\45\\40\\45\\53\\54\\54\\49\\52\\53\\41\\41\\45\\50\\52\\52\\52\\48\\53\\41\\105\\102\\32\\72\\91\\121\\93\\61\\61\\45\\54\\56\\57\\56\\53\\51\\45\\40\\50\\53\\52\\57\\53\\56\\43\\40\\45\\49\\57\\52\\55\\52\\54\\52\\45\\40\\45\\49\\48\\48\\50\\54\\53\\51\\41\\41\\41\\116\\104\\101\\110\\32\\72\\91\\121\\93\\44\\100\\91\\121\\93\\61\\110\\105\\108\\44\\110\\105\\108\\32\\101\\110\\100\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\98\\41\\108\\111\\99\\97\\108\\32\\89\\61\\118\\40\\98\\41\\108\\111\\99\\97\\108\\32\\108\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\108\\44\\83\\44\\122\\44\\102\\44\\81\\44\\100\\41\\114\\101\\116\\117\\114\\110\\32\\113\\40\\121\\44\\123\\108\\44\\83\\44\\122\\59\\102\\59\\81\\44\\100\\125\\44\\98\\44\\89\\41\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\108\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\98\\41\\108\\111\\99\\97\\108\\32\\89\\61\\118\\40\\98\\41\\108\\111\\99\\97\\108\\32\\108\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\108\\44\\83\\44\\122\\44\\102\\44\\81\\41\\114\\101\\116\\117\\114\\110\\32\\113\\40\\121\\44\\123\\108\\44\\83\\44\\122\\44\\102\\59\\81\\125\\44\\98\\44\\89\\41\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\108\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\41\\102\\111\\114\\32\\98\\61\\52\\52\\51\\54\\48\\57\\45\\52\\52\\51\\54\\48\\56\\44\\35\\121\\44\\45\\56\\50\\52\\50\\51\\51\\45\\40\\45\\56\\50\\52\\50\\51\\52\\41\\100\\111\\32\\72\\91\\121\\91\\98\\93\\93\\61\\72\\91\\121\\91\\98\\93\\93\\43\\40\\40\\40\\49\\48\\55\\55\\51\\52\\49\\45\\49\\48\\50\\55\\48\\54\\49\\41\\45\\57\\48\\55\\50\\50\\56\\41\\43\\56\\53\\54\\57\\52\\57\\41\\101\\110\\100\\32\\105\\102\\32\\108\\32\\116\\104\\101\\110\\32\\108\\111\\99\\97\\108\\32\\113\\61\\108\\40\\116\\114\\117\\101\\41\\108\\111\\99\\97\\108\\32\\89\\61\\122\\40\\113\\41\\89\\91\\98\\40\\53\\49\\57\\48\\55\\52\\45\\52\\57\\52\\53\\53\\57\\41\\93\\44\\89\\91\\98\\40\\56\\49\\54\\50\\53\\53\\45\\55\\57\\49\\55\\54\\53\\41\\93\\44\\89\\91\\98\\40\\45\\49\\48\\49\\50\\56\\49\\53\\45\\40\\45\\49\\48\\51\\55\\51\\50\\50\\41\\41\\93\\61\\121\\44\\82\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\41\\114\\101\\116\\117\\114\\110\\32\\57\\52\\55\\50\\52\\43\\45\\50\\53\\50\\50\\57\\52\\49\\32\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\113\\32\\101\\108\\115\\101\\32\\114\\101\\116\\117\\114\\110\\32\\83\\40\\123\\125\\44\\123\\91\\98\\40\\45\\51\\53\\48\\53\\50\\52\\45\\40\\45\\51\\55\\53\\48\\49\\52\\41\\41\\93\\61\\82\\59\\91\\98\\40\\55\\50\\54\\53\\52\\56\\45\\55\\48\\50\\48\\51\\51\\41\\93\\61\\121\\44\\91\\98\\40\\45\\49\\54\\55\\52\\49\\49\\45\\40\\57\\49\\55\\48\\51\\53\\43\\45\\49\\49\\48\\56\\57\\53\\51\\41\\41\\93\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\41\\114\\101\\116\\117\\114\\110\\45\\51\\48\\53\\56\\56\\48\\57\\45\\40\\45\\54\\51\\48\\53\\57\\50\\41\\101\\110\\100\\125\\41\\101\\110\\100\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\41\\108\\111\\99\\97\\108\\32\\98\\44\\113\\61\\49\\48\\49\\56\\51\\51\\48\\43\\45\\49\\48\\49\\56\\51\\50\\57\\44\\121\\91\\40\\52\\55\\55\\52\\51\\50\\45\\40\\53\\53\\57\\53\\57\\51\\43\\45\\52\\56\\50\\48\\56\\48\\41\\41\\45\\51\\57\\57\\57\\49\\56\\93\\119\\104\\105\\108\\101\\32\\113\\32\\100\\111\\32\\72\\91\\113\\93\\44\\98\\61\\72\\91\\113\\93\\45\\40\\40\\45\\50\\50\\48\\52\\50\\52\\43\\45\\52\\53\\57\\55\\56\\53\\41\\45\\40\\45\\54\\56\\48\\50\\49\\48\\41\\41\\44\\40\\45\\49\\50\\49\\52\\57\\50\\45\\40\\45\\49\\50\\49\\52\\57\\51\\41\\41\\43\\98\\32\\105\\102\\32\\51\\49\\57\\50\\56\\51\\45\\51\\49\\57\\50\\56\\51\\61\\61\\72\\91\\113\\93\\116\\104\\101\\110\\32\\72\\91\\113\\93\\44\\100\\91\\113\\93\\61\\110\\105\\108\\44\\110\\105\\108\\32\\101\\110\\100\\32\\113\\61\\121\\91\\98\\93\\101\\110\\100\\32\\101\\110\\100\\44\\123\\125\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\113\\44\\108\\44\\83\\44\\122\\41\\108\\111\\99\\97\\108\\32\\106\\44\\88\\44\\114\\44\\99\\44\\71\\44\\105\\44\\86\\44\\97\\44\\78\\44\\118\\44\\116\\44\\84\\44\\110\\44\\73\\44\\66\\44\\74\\44\\65\\44\\109\\44\\72\\44\\115\\44\\90\\44\\107\\44\\82\\44\\101\\44\\119\\44\\68\\44\\77\\44\\111\\44\\85\\44\\117\\44\\81\\44\\104\\44\\80\\44\\70\\32\\119\\104\\105\\108\\101\\32\\113\\32\\100\\111\\32\\105\\102\\32\\113\\60\\49\\48\\54\\49\\51\\54\\51\\48\\45\\51\\48\\53\\49\\49\\51\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\53\\48\\48\\52\\50\\53\\49\\45\\40\\45\\56\\51\\51\\53\\48\\52\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\51\\52\\48\\54\\51\\54\\43\\52\\51\\52\\55\\57\\51\\57\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\49\\53\\51\\53\\53\\57\\43\\50\\53\\57\\55\\50\\50\\55\\41\\45\\51\\48\\50\\52\\52\\55\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\50\\55\\50\\52\\55\\53\\43\\50\\48\\49\\51\\56\\57\\51\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\45\\53\\50\\54\\57\\50\\53\\45\\40\\45\\54\\57\\53\\55\\50\\48\\41\\41\\45\\40\\45\\49\\48\\57\\52\\53\\49\\56\\45\\40\\45\\50\\56\\53\\53\\53\\57\\43\\40\\49\\48\\51\\53\\48\\48\\55\\43\\45\\57\\49\\54\\52\\56\\48\\41\\41\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\50\\53\\51\\54\\49\\53\\43\\40\\40\\57\\55\\55\\56\\55\\52\\45\\40\\54\\50\\50\\52\\50\\54\\43\\45\\52\\57\\55\\57\\41\\41\\43\\52\\53\\52\\53\\50\\57\\41\\116\\104\\101\\110\\32\\113\\61\\100\\91\\110\\93\\70\\61\\49\\55\\48\\57\\55\\57\\43\\45\\49\\55\\48\\57\\55\\51\\32\\71\\61\\56\\54\\50\\50\\54\\51\\45\\40\\45\\55\\52\\54\\52\\48\\51\\43\\49\\54\\48\\56\\54\\54\\53\\41\\90\\61\\113\\40\\71\\44\\70\\41\\70\\61\\98\\40\\45\\56\\55\\48\\48\\55\\54\\45\\40\\45\\49\\55\\57\\48\\53\\57\\48\\45\\40\\45\\54\\49\\54\\48\\54\\53\\45\\50\\55\\57\\57\\55\\53\\41\\41\\41\\113\\61\\98\\40\\45\\49\\48\\50\\53\\48\\51\\53\\43\\49\\48\\52\\57\\53\\48\\57\\41\\121\\91\\113\\93\\61\\90\\32\\71\\61\\121\\91\\70\\93\\70\\61\\40\\45\\51\\49\\50\\57\\52\\55\\45\\40\\45\\49\\56\\54\\53\\48\\56\\55\\45\\40\\45\\57\\53\\57\\57\\51\\52\\41\\41\\41\\45\\40\\49\\48\\49\\57\\56\\53\\53\\43\\40\\52\\52\\53\\56\\54\\51\\43\\45\\56\\55\\51\\53\\49\\52\\41\\41\\113\\61\\71\\62\\70\\32\\113\\61\\113\\32\\97\\110\\100\\32\\49\\52\\50\\52\\56\\53\\55\\49\\45\\40\\45\\51\\52\\50\\55\\53\\52\\41\\111\\114\\45\\54\\57\\53\\52\\51\\43\\40\\45\\53\\57\\54\\54\\56\\56\\43\\40\\49\\54\\52\\50\\57\\57\\54\\50\\45\\40\\45\\55\\51\\56\\52\\57\\48\\41\\41\\41\\101\\108\\115\\101\\32\\106\\61\\110\\105\\108\\32\\111\\61\\79\\40\\111\\41\\74\\61\\79\\40\\74\\41\\68\\61\\79\\40\\68\\41\\99\\61\\110\\105\\108\\32\\99\\61\\75\\40\\41\\116\\61\\110\\105\\108\\32\\107\\61\\110\\105\\108\\32\\111\\61\\45\\50\\57\\50\\54\\54\\45\\40\\57\\56\\51\\56\\57\\53\\43\\45\\49\\48\\49\\51\\52\\49\\55\\41\\84\\61\\79\\40\\84\\41\\115\\61\\110\\105\\108\\32\\82\\61\\79\\40\\82\\41\\85\\61\\110\\105\\108\\32\\84\\61\\110\\105\\108\\32\\110\\61\\79\\40\\110\\41\\66\\61\\79\\40\\66\\41\\118\\61\\79\\40\\118\\41\\118\\61\\110\\105\\108\\32\\82\\61\\75\\40\\41\\107\\61\\98\\40\\45\\51\\53\\49\\48\\56\\48\\43\\51\\55\\53\\53\\54\\55\\41\\85\\61\\98\\40\\52\\49\\55\\57\\50\\51\\45\\40\\56\\57\\52\\57\\50\\51\\43\\45\\53\\48\\49\\52\\54\\52\\41\\41\\70\\61\\111\\32\\68\\61\\75\\40\\41\\110\\61\\98\\40\\50\\55\\54\\51\\49\\50\\43\\45\\50\\53\\49\\56\\52\\56\\41\\100\\91\\82\\93\\61\\84\\32\\84\\61\\75\\40\\41\\100\\91\\84\\93\\61\\118\\32\\74\\61\\121\\91\\85\\93\\85\\61\\98\\40\\40\\49\\55\\51\\54\\54\\49\\43\\40\\49\\50\\48\\57\\54\\43\\45\\51\\52\\53\\53\\54\\49\\41\\41\\45\\40\\45\\49\\56\\52\\50\\53\\51\\41\\41\\118\\61\\74\\91\\85\\93\\74\\61\\75\\40\\41\\100\\91\\74\\93\\61\\118\\32\\85\\61\\121\\91\\110\\93\\113\\61\\45\\49\\51\\48\\50\\56\\49\\43\\52\\48\\54\\51\\56\\53\\54\\32\\110\\61\\98\\40\\45\\49\\57\\52\\48\\52\\57\\43\\50\\49\\56\\53\\50\\56\\41\\111\\61\\45\\50\\57\\56\\50\\49\\50\\45\\40\\45\\50\\57\\56\\50\\49\\51\\41\\118\\61\\85\\91\\110\\93\\110\\61\\121\\91\\107\\93\\115\\61\\123\\125\\107\\61\\98\\40\\45\\52\\56\\54\\48\\54\\43\\40\\45\\54\\52\\52\\54\\48\\48\\45\\40\\45\\55\\49\\55\\54\\56\\50\\41\\41\\41\\85\\61\\110\\91\\107\\93\\66\\61\\98\\40\\57\\50\\51\\49\\48\\43\\45\\54\\55\\56\\52\\49\\41\\109\\61\\111\\32\\107\\61\\121\\91\\66\\93\\66\\61\\98\\40\\55\\52\\52\\55\\52\\55\\45\\55\\50\\48\\50\\52\\56\\41\\110\\61\\107\\91\\66\\93\\106\\61\\123\\125\\107\\61\\45\\50\\57\\53\\51\\50\\52\\45\\40\\45\\50\\57\\53\\51\\50\\52\\41\\66\\61\\75\\40\\41\\100\\91\\66\\93\\61\\107\\32\\107\\61\\40\\45\\53\\53\\52\\56\\55\\49\\43\\49\\50\\56\\49\\57\\48\\52\\41\\45\\40\\40\\45\\53\\57\\55\\57\\53\\45\\40\\45\\55\\48\\56\\49\\50\\53\\41\\41\\45\\40\\45\\55\\56\\55\\48\\49\\41\\41\\100\\91\\68\\93\\61\\107\\32\\100\\91\\99\\93\\61\\115\\32\\107\\61\\123\\125\\115\\61\\45\\57\\50\\48\\54\\55\\55\\43\\57\\50\\48\\54\\55\\55\\32\\111\\61\\45\\57\\51\\53\\50\\49\\51\\43\\57\\51\\53\\50\\49\\51\\32\\116\\61\\45\\53\\48\\53\\52\\48\\54\\45\\40\\45\\53\\48\\53\\52\\48\\55\\41\\73\\61\\109\\60\\111\\32\\111\\61\\116\\45\\109\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\53\\51\\51\\53\\54\\50\\45\\40\\45\\49\\51\\50\\53\\52\\50\\49\\45\\40\\45\\49\\48\\48\\50\\55\\55\\55\\43\\51\\56\\56\\53\\57\\49\\41\\41\\116\\104\\101\\110\\32\\113\\61\\111\\32\\113\\61\\116\\32\\97\\110\\100\\32\\49\\52\\48\\56\\51\\57\\54\\57\\45\\51\\53\\54\\54\\53\\56\\32\\111\\114\\32\\49\\52\\53\\49\\50\\48\\51\\48\\45\\40\\45\\53\\55\\56\\50\\54\\56\\41\\101\\108\\115\\101\\32\\65\\61\\110\\105\\108\\32\\101\\61\\79\\40\\101\\41\\73\\61\\79\\40\\73\\41\\88\\61\\79\\40\\88\\41\\109\\61\\79\\40\\109\\41\\113\\61\\45\\57\\50\\55\\50\\52\\50\\43\\49\\53\\53\\52\\52\\57\\54\\53\\32\\80\\61\\79\\40\\80\\41\\86\\61\\79\\40\\86\\41\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\40\\45\\55\\55\\55\\54\\49\\52\\45\\40\\40\\52\\48\\48\\50\\52\\45\\40\\45\\57\\49\\57\\52\\48\\49\\41\\41\\45\\49\\48\\49\\53\\52\\56\\49\\41\\41\\43\\51\\49\\54\\49\\52\\55\\56\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\54\\57\\48\\51\\57\\48\\43\\49\\55\\52\\49\\55\\50\\48\\32\\116\\104\\101\\110\\32\\81\\61\\45\\53\\55\\54\\57\\51\\43\\40\\49\\48\\52\\49\\57\\48\\48\\48\\45\\50\\56\\53\\49\\53\\51\\41\\84\\61\\98\\40\\45\\50\\51\\52\\57\\55\\51\\43\\40\\45\\55\\48\\48\\54\\49\\52\\43\\57\\54\\48\\48\\57\\53\\41\\41\\118\\61\\51\\57\\49\\53\\57\\53\\43\\51\\57\\50\\51\\48\\50\\54\\32\\72\\61\\84\\94\\118\\32\\113\\61\\81\\45\\72\\32\\81\\61\\98\\40\\45\\57\\52\\50\\53\\50\\51\\45\\40\\45\\57\\54\\55\\48\\48\\53\\41\\41\\72\\61\\113\\32\\113\\61\\81\\47\\72\\32\\81\\61\\123\\113\\125\\113\\61\\121\\91\\98\\40\\45\\53\\50\\55\\52\\53\\52\\43\\53\\53\\49\\57\\51\\57\\41\\93\\101\\108\\115\\101\\32\\107\\61\\98\\40\\40\\57\\52\\55\\50\\43\\49\\57\\51\\52\\49\\53\\41\\43\\40\\51\\56\\49\\55\\48\\45\\50\\49\\54\\54\\49\\50\\41\\41\\110\\61\\121\\91\\107\\93\\85\\61\\113\\32\\68\\61\\50\\57\\53\\52\\50\\49\\43\\45\\50\\57\\53\\52\\49\\57\\32\\66\\61\\82\\91\\68\\93\\107\\61\\110\\40\\66\\41\\74\\61\\107\\32\\113\\61\\107\\32\\97\\110\\100\\32\\54\\54\\57\\51\\55\\52\\49\\45\\54\\51\\53\\50\\56\\49\\32\\111\\114\\32\\49\\49\\55\\53\\54\\56\\57\\57\\45\\40\\52\\52\\56\\49\\53\\50\\43\\49\\53\\57\\50\\49\\55\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\100\\91\\83\\91\\45\\57\\56\\57\\54\\49\\48\\45\\40\\45\\57\\56\\57\\54\\49\\55\\41\\93\\93\\113\\61\\113\\32\\97\\110\\100\\40\\40\\51\\50\\51\\54\\56\\57\\43\\45\\49\\48\\56\\51\\56\\56\\48\\41\\45\\40\\45\\51\\53\\56\\48\\52\\41\\41\\43\\56\\55\\48\\57\\54\\51\\51\\32\\111\\114\\32\\54\\51\\55\\54\\50\\56\\51\\45\\40\\45\\49\\52\\51\\55\\53\\53\\51\\45\\40\\45\\55\\52\\50\\56\\50\\57\\41\\41\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\54\\52\\49\\50\\55\\55\\43\\50\\51\\51\\56\\51\\56\\49\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\57\\57\\57\\52\\55\\48\\43\\45\\49\\48\\53\\51\\51\\52\\54\\41\\43\\50\\57\\48\\54\\54\\53\\52\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\50\\54\\51\\49\\50\\55\\50\\45\\40\\45\\53\\51\\51\\51\\55\\41\\116\\104\\101\\110\\32\\97\\61\\50\\57\\50\\48\\57\\57\\51\\49\\48\\52\\52\\51\\51\\54\\45\\40\\45\\49\\54\\51\\49\\50\\55\\41\\116\\61\\75\\40\\41\\66\\61\\99\\32\\65\\61\\98\\40\\45\\56\\57\\56\\50\\49\\51\\43\\57\\50\\50\\55\\51\\50\\41\\100\\91\\116\\93\\61\\66\\32\\66\\61\\116\\32\\116\\61\\75\\40\\41\\100\\91\\116\\93\\61\\106\\32\\109\\61\\100\\91\\66\\93\\111\\61\\45\\55\\48\\54\\55\\56\\55\\43\\40\\45\\56\\57\\53\\51\\51\\50\\43\\40\\54\\54\\49\\52\\49\\53\\45\\40\\45\\57\\52\\48\\55\\50\\53\\41\\41\\41\\101\\61\\100\\91\\84\\93\\106\\61\\116\\32\\80\\61\\100\\91\\82\\93\\86\\61\\80\\40\\65\\44\\97\\41\\88\\61\\101\\91\\86\\93\\70\\61\\109\\91\\88\\93\\88\\61\\100\\91\\118\\93\\109\\61\\70\\40\\88\\41\\116\\61\\111\\61\\61\\109\\32\\113\\61\\116\\32\\97\\110\\100\\32\\50\\49\\50\\56\\52\\50\\43\\54\\55\\49\\55\\56\\49\\52\\32\\111\\114\\40\\50\\51\\49\\54\\52\\43\\49\\48\\49\\56\\49\\49\\49\\49\\41\\45\\40\\45\\56\\51\\49\\55\\56\\49\\41\\101\\108\\115\\101\\32\\113\\61\\100\\91\\83\\91\\45\\49\\54\\54\\56\\52\\57\\45\\40\\54\\53\\53\\49\\52\\52\\45\\56\\50\\50\\48\\48\\48\\41\\93\\93\\81\\61\\123\\113\\40\\41\\125\\81\\61\\123\\89\\40\\81\\41\\125\\113\\61\\121\\91\\98\\40\\40\\40\\49\\50\\56\\52\\56\\55\\45\\54\\53\\54\\55\\55\\55\\41\\45\\40\\45\\50\\50\\53\\52\\56\\55\\45\\49\\54\\50\\50\\49\\56\\41\\41\\45\\40\\45\\49\\54\\53\\48\\57\\53\\41\\41\\93\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\81\\32\\97\\110\\100\\32\\52\\56\\56\\50\\53\\54\\43\\40\\45\\54\\57\\52\\49\\55\\51\\43\\55\\54\\55\\50\\55\\57\\57\\41\\111\\114\\32\\51\\49\\56\\57\\53\\56\\48\\45\\55\\52\\56\\55\\57\\54\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\49\\55\\57\\54\\48\\56\\43\\40\\50\\51\\57\\55\\49\\53\\55\\45\\40\\45\\56\\53\\49\\50\\48\\49\\41\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\50\\57\\51\\50\\48\\56\\43\\51\\52\\48\\53\\52\\50\\48\\32\\116\\104\\101\\110\\32\\113\\61\\52\\49\\51\\55\\49\\49\\50\\45\\50\\48\\51\\53\\51\\55\\32\\116\\61\\111\\32\\88\\61\\116\\32\\106\\91\\116\\93\\61\\88\\32\\116\\61\\110\\105\\108\\32\\101\\108\\115\\101\\32\\72\\61\\108\\91\\45\\57\\48\\54\\51\\49\\50\\43\\40\\54\\53\\50\\48\\52\\48\\43\\50\\53\\52\\50\\55\\51\\41\\93\\81\\61\\100\\91\\83\\91\\51\\50\\48\\57\\56\\57\\43\\40\\40\\49\\55\\49\\55\\49\\57\\43\\45\\53\\50\\56\\53\\56\\48\\41\\43\\51\\53\\56\\55\\51\\41\\93\\93\\84\\61\\100\\91\\83\\91\\55\\54\\50\\52\\51\\49\\45\\55\\54\\50\\52\\50\\57\\93\\93\\74\\61\\100\\91\\83\\91\\45\\49\\55\\48\\55\\49\\57\\43\\49\\55\\48\\55\\50\\50\\93\\93\\85\\61\\100\\91\\83\\91\\51\\56\\51\\54\\55\\50\\45\\51\\56\\51\\54\\54\\56\\93\\93\\107\\61\\98\\40\\40\\45\\49\\50\\55\\48\\51\\54\\57\\45\\40\\45\\56\\56\\52\\53\\54\\49\\41\\41\\45\\40\\45\\52\\49\\48\\50\\56\\57\\41\\41\\66\\61\\55\\52\\57\\56\\56\\51\\48\\55\\48\\56\\55\\57\\50\\45\\50\\51\\52\\56\\48\\32\\110\\61\\85\\40\\107\\44\\66\\41\\82\\61\\74\\91\\110\\93\\85\\61\\100\\91\\83\\91\\45\\49\\52\\54\\56\\54\\48\\45\\40\\45\\49\\52\\54\\56\\54\\53\\41\\93\\93\\66\\61\\100\\91\\83\\91\\45\\56\\48\\49\\50\\50\\43\\56\\48\\49\\50\\56\\93\\93\\107\\61\\35\\66\\32\\66\\61\\53\\50\\57\\55\\56\\49\\45\\53\\50\\57\\55\\55\\51\\32\\110\\61\\107\\45\\66\\32\\74\\61\\85\\91\\110\\93\\85\\61\\100\\91\\83\\91\\50\\53\\48\\51\\51\\50\\45\\50\\53\\48\\51\\50\\54\\93\\93\\66\\61\\45\\50\\56\\53\\52\\56\\54\\45\\40\\45\\50\\56\\53\\52\\57\\48\\41\\107\\61\\52\\54\\54\\55\\51\\48\\43\\45\\52\\54\\54\\55\\50\\57\\32\\110\\61\\98\\40\\55\\49\\50\\49\\48\\50\\45\\40\\40\\49\\53\\48\\50\\51\\53\\50\\45\\40\\49\\48\\51\\57\\50\\49\\52\\45\\57\\52\\49\\48\\56\\56\\41\\41\\45\\55\\49\\54\\54\\49\\53\\41\\41\\110\\61\\85\\91\\110\\93\\110\\61\\110\\40\\85\\44\\107\\44\\66\\41\\66\\61\\55\\56\\56\\50\\57\\49\\43\\45\\55\\56\\56\\50\\56\\57\\32\\107\\61\\100\\91\\83\\91\\45\\54\\55\\57\\52\\56\\48\\45\\40\\45\\54\\55\\57\\52\\56\\54\\41\\93\\93\\85\\61\\107\\91\\66\\93\\107\\61\\40\\53\\55\\52\\57\\50\\55\\43\\45\\49\\51\\55\\50\\57\\48\\55\\41\\43\\55\\52\\48\\50\\51\\56\\51\\49\\50\\56\\56\\56\\32\\118\\61\\84\\40\\82\\44\\74\\44\\110\\44\\85\\41\\113\\61\\81\\91\\118\\93\\118\\61\\45\\49\\50\\56\\57\\56\\52\\43\\49\\50\\56\\57\\56\\53\\32\\81\\61\\113\\40\\72\\41\\110\\61\\98\\40\\49\\54\\56\\51\\56\\54\\43\\45\\49\\52\\51\\57\\48\\56\\41\\84\\61\\81\\32\\81\\61\\84\\91\\118\\93\\82\\61\\100\\91\\83\\91\\55\\53\\50\\52\\55\\45\\40\\45\\53\\55\\56\\49\\51\\57\\43\\40\\49\\51\\50\\57\\55\\56\\48\\45\\54\\55\\54\\51\\57\\55\\41\\41\\93\\93\\74\\61\\100\\91\\83\\91\\50\\51\\52\\57\\54\\50\\45\\50\\51\\52\\57\\53\\56\\93\\93\\85\\61\\74\\40\\110\\44\\107\\41\\118\\61\\82\\91\\85\\93\\113\\61\\81\\61\\61\\118\\32\\113\\61\\113\\32\\97\\110\\100\\32\\49\\50\\55\\50\\50\\52\\48\\49\\45\\56\\57\\57\\51\\57\\51\\32\\111\\114\\32\\51\\56\\56\\51\\49\\54\\56\\45\\40\\45\\52\\53\\51\\51\\49\\56\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\88\\61\\110\\111\\116\\32\\73\\32\\111\\61\\111\\43\\109\\32\\116\\61\\111\\60\\61\\70\\32\\116\\61\\88\\32\\97\\110\\100\\32\\116\\32\\88\\61\\111\\62\\61\\70\\32\\88\\61\\73\\32\\97\\110\\100\\32\\88\\32\\116\\61\\88\\32\\111\\114\\32\\116\\32\\88\\61\\45\\54\\48\\54\\52\\53\\43\\51\\48\\53\\51\\52\\51\\52\\32\\113\\61\\116\\32\\97\\110\\100\\32\\88\\32\\116\\61\\45\\49\\48\\52\\52\\49\\51\\51\\43\\49\\49\\50\\56\\53\\57\\51\\53\\32\\113\\61\\113\\32\\111\\114\\32\\116\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\40\\50\\50\\54\\49\\51\\54\\45\\40\\45\\50\\53\\51\\53\\53\\48\\45\\40\\45\\54\\48\\57\\52\\48\\49\\41\\41\\41\\43\\52\\53\\52\\54\\48\\51\\52\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\51\\53\\48\\57\\55\\51\\51\\45\\40\\45\\55\\51\\51\\54\\52\\50\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\51\\57\\57\\56\\55\\53\\43\\52\\54\\49\\50\\48\\52\\53\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\50\\55\\54\\48\\56\\55\\43\\51\\56\\48\\51\\53\\55\\54\\32\\116\\104\\101\\110\\32\\97\\61\\40\\40\\57\\55\\50\\49\\55\\55\\45\\56\\49\\49\\53\\55\\53\\41\\45\\51\\53\\57\\53\\48\\53\\41\\43\\49\\57\\56\\57\\48\\52\\32\\78\\61\\65\\91\\97\\93\\113\\61\\54\\53\\52\\50\\54\\52\\50\\45\\55\\48\\48\\52\\50\\49\\32\\119\\61\\78\\32\\101\\108\\115\\101\\32\\113\\61\\116\\114\\117\\101\\32\\100\\91\\83\\91\\40\\40\\54\\55\\52\\50\\56\\50\\43\\56\\48\\51\\53\\52\\57\\41\\45\\52\\57\\54\\53\\51\\50\\41\\43\\40\\45\\49\\57\\55\\53\\52\\54\\54\\45\\40\\45\\57\\57\\52\\49\\54\\56\\41\\41\\93\\93\\61\\113\\32\\81\\61\\123\\125\\113\\61\\121\\91\\98\\40\\56\\54\\54\\50\\51\\54\\43\\45\\56\\52\\49\\55\\56\\52\\41\\93\\101\\110\\100\\32\\101\\108\\115\\101\\32\\118\\61\\118\\43\\74\\32\\84\\61\\118\\60\\61\\82\\32\\110\\61\\110\\111\\116\\32\\85\\32\\84\\61\\110\\32\\97\\110\\100\\32\\84\\32\\110\\61\\118\\62\\61\\82\\32\\110\\61\\85\\32\\97\\110\\100\\32\\110\\32\\84\\61\\110\\32\\111\\114\\32\\84\\32\\110\\61\\45\\56\\53\\57\\49\\52\\54\\43\\49\\53\\56\\57\\53\\55\\52\\48\\32\\113\\61\\84\\32\\97\\110\\100\\32\\110\\32\\84\\61\\45\\52\\57\\50\\56\\56\\52\\43\\49\\50\\55\\51\\51\\57\\51\\53\\32\\113\\61\\113\\32\\111\\114\\32\\84\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\51\\52\\56\\55\\48\\54\\56\\45\\40\\45\\56\\54\\49\\52\\55\\52\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\51\\57\\51\\51\\57\\48\\54\\45\\40\\45\\51\\54\\52\\51\\54\\48\\41\\116\\104\\101\\110\\32\\113\\61\\78\\32\\81\\61\\119\\32\\113\\61\\45\\49\\48\\51\\49\\56\\54\\54\\43\\49\\54\\57\\51\\52\\52\\56\\49\\32\\101\\108\\115\\101\\32\\113\\61\\100\\91\\83\\91\\45\\57\\57\\54\\55\\52\\53\\45\\40\\45\\57\\57\\54\\55\\53\\51\\41\\93\\93\\81\\61\\123\\113\\40\\72\\41\\125\\81\\61\\123\\89\\40\\81\\41\\125\\113\\61\\121\\91\\98\\40\\50\\56\\53\\56\\48\\45\\52\\49\\48\\56\\41\\93\\101\\110\\100\\32\\101\\108\\115\\101\\32\\118\\61\\49\\56\\54\\49\\50\\52\\43\\49\\49\\50\\53\\49\\55\\55\\56\\32\\81\\61\\40\\40\\53\\52\\48\\53\\53\\48\\45\\40\\45\\52\\57\\50\\50\\51\\53\\41\\41\\43\\51\\57\\53\\53\\51\\53\\56\\41\\45\\40\\45\\54\\50\\57\\52\\55\\53\\41\\84\\61\\98\\40\\49\\48\\50\\48\\55\\53\\43\\45\\55\\55\\53\\57\\49\\41\\72\\61\\84\\94\\118\\32\\113\\61\\81\\45\\72\\32\\72\\61\\113\\32\\81\\61\\98\\40\\57\\55\\51\\55\\57\\56\\43\\45\\57\\52\\57\\51\\52\\53\\41\\113\\61\\81\\47\\72\\32\\81\\61\\123\\113\\125\\113\\61\\121\\91\\98\\40\\40\\54\\49\\50\\57\\51\\55\\43\\40\\40\\45\\56\\55\\48\\55\\49\\57\\45\\40\\45\\49\\57\\48\\51\\52\\52\\41\\41\\45\\50\\52\\56\\49\\54\\48\\41\\41\\45\\40\\45\\51\\52\\48\\48\\53\\52\\41\\41\\93\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\52\\49\\54\\50\\48\\50\\51\\45\\40\\50\\50\\50\\48\\43\\45\\55\\57\\50\\54\\50\\48\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\49\\51\\57\\53\\56\\57\\43\\53\\48\\51\\56\\51\\54\\49\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\53\\55\\55\\57\\54\\49\\53\\45\\49\\48\\48\\51\\48\\54\\56\\32\\116\\104\\101\\110\\32\\99\\44\\106\\61\\68\\40\\115\\44\\99\\41\\113\\61\\99\\32\\97\\110\\100\\32\\51\\51\\56\\53\\56\\53\\49\\45\\56\\55\\51\\50\\49\\57\\32\\111\\114\\32\\57\\53\\49\\57\\55\\55\\43\\51\\56\\57\\57\\54\\48\\56\\32\\101\\108\\115\\101\\32\\113\\61\\45\\56\\57\\54\\57\\52\\55\\43\\49\\50\\53\\52\\55\\55\\50\\51\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\85\\61\\100\\91\\74\\93\\81\\61\\85\\32\\113\\61\\49\\50\\54\\49\\50\\50\\54\\55\\45\\40\\56\\54\\55\\57\\49\\57\\43\\45\\49\\49\\52\\49\\48\\48\\54\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\45\\50\\57\\51\\55\\55\\56\\43\\53\\57\\51\\49\\53\\52\\55\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\51\\52\\57\\50\\54\\57\\43\\52\\55\\49\\49\\57\\52\\57\\32\\116\\104\\101\\110\\32\\113\\61\\121\\91\\98\\40\\53\\55\\53\\56\\52\\53\\43\\45\\53\\53\\49\\51\\54\\56\\41\\93\\81\\61\\123\\84\\125\\101\\108\\115\\101\\32\\78\\61\\100\\91\\84\\93\\113\\61\\78\\32\\97\\110\\100\\32\\56\\56\\56\\57\\53\\48\\43\\51\\49\\49\\56\\57\\53\\50\\32\\111\\114\\40\\54\\51\\48\\56\\53\\49\\54\\45\\53\\57\\57\\50\\49\\56\\41\\45\\40\\45\\49\\51\\50\\57\\50\\51\\41\\119\\61\\78\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\121\\91\\98\\40\\40\\55\\51\\48\\54\\57\\48\\43\\45\\49\\49\\55\\51\\54\\51\\54\\41\\43\\52\\54\\55\\52\\48\\51\\41\\93\\81\\61\\123\\125\\82\\61\\79\\40\\82\\41\\84\\61\\79\\40\\84\\41\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\40\\45\\50\\55\\49\\49\\52\\52\\43\\45\\49\\57\\51\\52\\54\\52\\41\\43\\40\\55\\54\\53\\53\\51\\49\\54\\45\\40\\45\\57\\48\\56\\55\\48\\53\\41\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\55\\50\\49\\48\\55\\49\\57\\45\\51\\53\\51\\55\\49\\57\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\53\\53\\49\\54\\52\\55\\48\\45\\40\\45\\53\\51\\55\\51\\54\\52\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\54\\52\\50\\55\\50\\52\\52\\45\\53\\53\\52\\49\\50\\52\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\56\\57\\57\\48\\57\\50\\43\\54\\55\\52\\57\\53\\56\\49\\32\\116\\104\\101\\110\\32\\100\\91\\84\\93\\61\\119\\32\\77\\61\\100\\91\\101\\93\\105\\61\\45\\56\\55\\50\\54\\55\\51\\45\\40\\45\\56\\55\\50\\54\\55\\52\\41\\104\\61\\77\\43\\105\\32\\97\\61\\65\\91\\104\\93\\78\\61\\115\\43\\97\\32\\97\\61\\40\\40\\45\\53\\48\\49\\53\\54\\55\\43\\40\\45\\55\\51\\52\\50\\54\\54\\43\\49\\56\\51\\54\\51\\48\\57\\41\\41\\45\\40\\45\\50\\49\\52\\55\\54\\49\\41\\41\\45\\40\\57\\57\\48\\48\\55\\53\\43\\45\\49\\55\\53\\48\\57\\52\\41\\113\\61\\78\\37\\97\\32\\104\\61\\100\\91\\88\\93\\97\\61\\99\\43\\104\\32\\115\\61\\113\\32\\104\\61\\45\\49\\54\\54\\57\\50\\56\\43\\49\\54\\55\\49\\56\\52\\32\\78\\61\\97\\37\\104\\32\\99\\61\\78\\32\\113\\61\\49\\48\\52\\49\\57\\55\\51\\43\\50\\51\\50\\50\\51\\52\\32\\101\\108\\115\\101\\32\\118\\61\\52\\51\\50\\53\\55\\50\\45\\52\\51\\50\\53\\55\\49\\32\\84\\61\\100\\91\\83\\91\\45\\49\\48\\49\\56\\57\\48\\50\\43\\40\\52\\55\\51\\55\\55\\50\\43\\40\\40\\45\\54\\51\\49\\53\\54\\57\\43\\52\\51\\54\\57\\50\\54\\41\\45\\40\\45\\55\\51\\57\\55\\55\\54\\41\\41\\41\\93\\93\\72\\61\\84\\126\\61\\118\\32\\113\\61\\72\\32\\97\\110\\100\\45\\51\\53\\57\\54\\49\\49\\43\\40\\45\\53\\53\\48\\56\\53\\50\\43\\49\\49\\50\\53\\49\\56\\57\\54\\41\\111\\114\\40\\49\\50\\50\\51\\49\\57\\55\\51\\45\\40\\45\\50\\57\\53\\53\\56\\50\\41\\41\\45\\40\\52\\55\\48\\48\\52\\45\\40\\45\\49\\56\\57\\51\\49\\50\\41\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\49\\52\\49\\56\\49\\54\\43\\53\\56\\49\\55\\49\\57\\48\\32\\116\\104\\101\\110\\32\\84\\61\\100\\91\\83\\91\\52\\57\\49\\55\\53\\50\\43\\45\\52\\57\\49\\55\\53\\48\\93\\93\\118\\61\\100\\91\\83\\91\\45\\57\\51\\57\\50\\57\\52\\43\\57\\51\\57\\50\\57\\55\\93\\93\\72\\61\\84\\61\\61\\118\\32\\113\\61\\40\\57\\50\\50\\56\\50\\57\\43\\45\\54\\49\\54\\55\\57\\52\\41\\43\\50\\54\\53\\49\\49\\56\\57\\32\\81\\61\\72\\32\\101\\108\\115\\101\\32\\106\\61\\98\\40\\45\\49\\48\\50\\55\\56\\54\\55\\43\\49\\48\\53\\50\\51\\53\\52\\41\\99\\61\\121\\91\\106\\93\\113\\61\\57\\51\\49\\52\\51\\51\\52\\45\\40\\45\\51\\54\\48\\56\\41\\106\\61\\98\\40\\57\\56\\54\\57\\52\\56\\43\\45\\57\\54\\50\\52\\56\\51\\41\\115\\61\\99\\91\\106\\93\\66\\61\\115\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\52\\52\\49\\52\\55\\55\\43\\40\\40\\40\\51\\56\\53\\56\\57\\55\\43\\52\\57\\48\\55\\53\\56\\41\\43\\53\\53\\55\\52\\55\\50\\57\\41\\45\\50\\52\\52\\53\\52\\55\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\57\\56\\54\\53\\52\\53\\43\\40\\53\\53\\57\\51\\51\\48\\56\\45\\52\\57\\48\\55\\53\\51\\41\\116\\104\\101\\110\\32\\68\\61\\45\\53\\56\\52\\53\\50\\55\\43\\53\\56\\52\\53\\50\\56\\32\\66\\61\\82\\91\\68\\93\\115\\61\\100\\91\\83\\91\\45\\53\\52\\51\\51\\51\\55\\45\\40\\45\\53\\52\\51\\51\\52\\48\\41\\93\\93\\111\\61\\40\\40\\40\\49\\52\\52\\50\\53\\54\\54\\45\\40\\50\\48\\52\\56\\53\\53\\43\\53\\52\\56\\53\\56\\56\\41\\41\\43\\49\\53\\55\\55\\55\\56\\56\\52\\57\\53\\49\\54\\55\\41\\45\\40\\45\\51\\54\\57\\56\\54\\51\\41\\41\\45\\40\\49\\55\\54\\57\\52\\55\\43\\45\\57\\48\\51\\56\\50\\50\\41\\116\\61\\98\\40\\45\\56\\51\\55\\55\\56\\49\\43\\56\\54\\50\\50\\55\\53\\41\\99\\61\\100\\91\\83\\91\\52\\54\\56\\48\\53\\56\\45\\52\\54\\56\\48\\53\\52\\93\\93\\106\\61\\99\\40\\116\\44\\111\\41\\113\\61\\40\\49\\48\\53\\53\\54\\55\\51\\56\\45\\40\\45\\56\\48\\49\\52\\54\\51\\41\\41\\45\\50\\48\\56\\54\\55\\49\\32\\68\\61\\115\\91\\106\\93\\115\\61\\45\\53\\52\\55\\49\\50\\53\\43\\53\\52\\55\\49\\50\\54\\32\\107\\61\\66\\91\\68\\93\\68\\61\\82\\91\\115\\93\\99\\61\\100\\91\\83\\91\\40\\56\\55\\48\\48\\55\\54\\43\\45\\55\\48\\56\\55\\57\\51\\41\\43\\40\\45\\52\\54\\54\\50\\53\\48\\45\\40\\45\\51\\48\\52\\57\\55\\48\\41\\41\\93\\93\\114\\61\\40\\51\\55\\57\\56\\51\\43\\53\\53\\53\\48\\48\\55\\41\\43\\57\\57\\51\\49\\50\\57\\51\\48\\57\\52\\51\\55\\55\\32\\106\\61\\100\\91\\83\\91\\45\\56\\52\\57\\51\\48\\57\\45\\40\\45\\56\\52\\57\\51\\49\\51\\41\\93\\93\\111\\61\\98\\40\\57\\50\\54\\52\\49\\50\\45\\40\\52\\51\\51\\52\\54\\50\\45\\40\\45\\52\\54\\56\\52\\52\\53\\41\\41\\41\\116\\61\\106\\40\\111\\44\\114\\41\\115\\61\\99\\91\\116\\93\\66\\61\\107\\40\\68\\44\\115\\41\\110\\61\\110\\111\\116\\32\\66\\32\\74\\61\\110\\32\\101\\108\\115\\101\\32\\119\\61\\100\\91\\84\\93\\81\\61\\119\\32\\113\\61\\119\\32\\97\\110\\100\\40\\40\\45\\54\\56\\56\\56\\55\\57\\43\\49\\53\\54\\52\\56\\52\\52\\41\\43\\49\\49\\55\\55\\57\\49\\55\\51\\41\\45\\40\\49\\48\\52\\50\\56\\52\\51\\45\\40\\57\\55\\54\\55\\57\\48\\43\\45\\55\\55\\52\\57\\55\\54\\41\\41\\111\\114\\45\\56\\50\\48\\55\\54\\57\\43\\49\\54\\55\\50\\51\\51\\56\\52\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\116\\114\\117\\101\\32\\113\\61\\113\\32\\97\\110\\100\\32\\50\\54\\52\\57\\56\\50\\43\\49\\49\\55\\51\\51\\57\\52\\53\\32\\111\\114\\40\\49\\49\\53\\54\\51\\51\\50\\54\\45\\40\\40\\45\\49\\48\\48\\56\\50\\50\\50\\45\\40\\45\\57\\55\\50\\56\\48\\52\\41\\41\\43\\40\\45\\57\\48\\50\\55\\50\\52\\45\\40\\45\\54\\50\\51\\52\\56\\53\\41\\41\\41\\41\\45\\54\\50\\49\\56\\52\\51\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\55\\51\\56\\51\\53\\50\\57\\45\\49\\54\\50\\55\\54\\49\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\54\\48\\53\\56\\49\\53\\57\\45\\40\\45\\49\\48\\49\\54\\50\\53\\50\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\50\\55\\57\\49\\53\\50\\43\\55\\51\\48\\48\\50\\52\\50\\32\\116\\104\\101\\110\\32\\104\\61\\50\\57\\49\\57\\54\\51\\43\\40\\40\\40\\53\\52\\52\\56\\54\\56\\43\\40\\53\\48\\57\\48\\56\\57\\57\\52\\49\\49\\53\\52\\53\\45\\57\\52\\55\\49\\51\\57\\41\\41\\45\\55\\55\\48\\51\\57\\57\\41\\45\\54\\48\\50\\53\\57\\53\\41\\88\\61\\98\\40\\45\\50\\49\\50\\52\\50\\54\\43\\50\\51\\54\\57\\49\\51\\41\\65\\61\\98\\40\\54\\51\\49\\48\\57\\48\\45\\54\\48\\54\\54\\49\\57\\41\\111\\61\\100\\91\\118\\93\\97\\61\\49\\48\\52\\56\\49\\55\\56\\53\\55\\54\\53\\50\\57\\53\\45\\40\\45\\49\\54\\48\\50\\48\\55\\43\\49\\49\\49\\50\\49\\55\\49\\41\\109\\61\\121\\91\\88\\93\\101\\61\\100\\91\\84\\93\\80\\61\\100\\91\\82\\93\\86\\61\\80\\40\\65\\44\\97\\41\\88\\61\\101\\91\\86\\93\\97\\61\\98\\40\\45\\57\\53\\55\\55\\57\\49\\43\\57\\56\\50\\50\\57\\55\\41\\70\\61\\109\\91\\88\\93\\80\\61\\100\\91\\84\\93\\86\\61\\100\\91\\82\\93\\65\\61\\86\\40\\97\\44\\104\\41\\101\\61\\80\\91\\65\\93\\65\\61\\45\\49\\54\\55\\49\\50\\52\\43\\49\\54\\55\\49\\50\\53\\32\\86\\61\\100\\91\\118\\93\\97\\61\\45\\49\\48\\50\\57\\48\\57\\53\\43\\49\\48\\50\\57\\48\\57\\56\\32\\80\\61\\86\\91\\65\\93\\65\\61\\100\\91\\66\\93\\86\\61\\65\\91\\97\\93\\88\\61\\123\\101\\44\\80\\59\\86\\125\\109\\61\\70\\40\\88\\41\\65\\61\\45\\56\\50\\57\\52\\55\\56\\43\\40\\49\\49\\55\\56\\48\\48\\48\\54\\51\\53\\57\\52\\54\\56\\45\\55\\56\\53\\49\\57\\52\\41\\116\\61\\111\\91\\109\\93\\86\\61\\98\\40\\45\\50\\53\\56\\56\\50\\51\\45\\40\\45\\50\\56\\51\\50\\57\\56\\41\\41\\111\\61\\100\\91\\106\\93\\88\\61\\100\\91\\84\\93\\101\\61\\100\\91\\82\\93\\80\\61\\101\\40\\86\\44\\65\\41\\109\\61\\88\\91\\80\\93\\70\\61\\116\\40\\109\\41\\109\\61\\69\\40\\49\\49\\49\\48\\56\\50\\55\\54\\45\\57\\55\\52\\52\\51\\54\\44\\123\\125\\41\\111\\91\\70\\93\\61\\109\\32\\111\\61\\100\\91\\106\\93\\66\\61\\79\\40\\66\\41\\80\\61\\98\\40\\40\\49\\56\\57\\51\\48\\55\\45\\55\\55\\54\\49\\48\\41\\43\\40\\45\\49\\56\\50\\53\\57\\50\\43\\57\\53\\51\\57\\48\\41\\41\\86\\61\\45\\57\\49\\56\\57\\49\\56\\43\\49\\53\\53\\55\\55\\52\\48\\52\\57\\55\\50\\49\\52\\57\\32\\109\\61\\100\\91\\84\\93\\88\\61\\100\\91\\82\\93\\101\\61\\88\\40\\80\\44\\86\\41\\70\\61\\109\\91\\101\\93\\109\\61\\76\\40\\54\\56\\49\\50\\55\\57\\43\\55\\54\\55\\53\\57\\53\\55\\44\\123\\125\\41\\111\\91\\70\\93\\61\\109\\32\\106\\61\\79\\40\\106\\41\\116\\61\\110\\105\\108\\32\\113\\61\\52\\57\\51\\51\\57\\48\\53\\45\\56\\50\\51\\50\\48\\32\\101\\108\\115\\101\\32\\118\\61\\100\\91\\83\\91\\52\\53\\55\\54\\54\\49\\45\\40\\40\\45\\49\\48\\49\\49\\50\\49\\48\\43\\40\\49\\56\\49\\57\\48\\57\\45\\40\\40\\52\\57\\55\\51\\48\\49\\45\\40\\55\\57\\57\\48\\54\\50\\43\\40\\40\\40\\45\\52\\54\\55\\51\\50\\49\\45\\40\\45\\57\\52\\51\\50\\56\\49\\41\\41\\43\\54\\48\\50\\49\\49\\52\\41\\45\\56\\55\\48\\50\\49\\53\\41\\41\\41\\45\\52\\50\\52\\54\\48\\49\\41\\41\\41\\43\\51\\53\\50\\55\\51\\50\\41\\93\\93\\113\\61\\123\\125\\72\\61\\113\\32\\82\\61\\118\\32\\118\\61\\45\\51\\49\\49\\48\\50\\49\\45\\40\\45\\51\\49\\49\\48\\50\\50\\41\\84\\61\\45\\55\\48\\57\\53\\51\\50\\43\\55\\48\\57\\53\\51\\51\\32\\74\\61\\118\\32\\113\\61\\57\\52\\50\\52\\56\\50\\43\\51\\50\\56\\48\\52\\50\\54\\32\\118\\61\\45\\57\\51\\49\\51\\51\\53\\43\\57\\51\\49\\51\\51\\53\\32\\85\\61\\74\\60\\118\\32\\118\\61\\84\\45\\74\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\116\\114\\117\\101\\32\\113\\61\\113\\32\\97\\110\\100\\45\\55\\56\\48\\51\\52\\50\\43\\49\\55\\57\\52\\54\\50\\48\\32\\111\\114\\45\\52\\55\\52\\48\\54\\54\\43\\49\\49\\53\\48\\51\\52\\49\\52\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\56\\55\\52\\50\\56\\48\\52\\45\\57\\50\\54\\54\\56\\55\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\54\\54\\50\\55\\49\\54\\50\\45\\40\\45\\56\\56\\56\\55\\54\\52\\41\\116\\104\\101\\110\\32\\81\\61\\98\\40\\40\\45\\54\\57\\51\\49\\57\\56\\43\\49\\55\\53\\54\\48\\49\\55\\41\\45\\49\\48\\51\\56\\51\\55\\52\\41\\107\\61\\98\\40\\40\\45\\54\\53\\56\\50\\55\\49\\43\\54\\57\\50\\57\\49\\48\\41\\45\\40\\45\\55\\51\\52\\49\\54\\48\\45\\40\\45\\55\\52\\52\\50\\57\\56\\41\\41\\41\\82\\61\\98\\40\\51\\52\\48\\56\\48\\54\\43\\45\\51\\49\\54\\50\\57\\48\\41\\113\\61\\121\\91\\81\\93\\72\\61\\100\\91\\83\\91\\45\\53\\52\\49\\52\\52\\48\\45\\40\\45\\53\\52\\49\\52\\52\\52\\41\\93\\93\\118\\61\\121\\91\\82\\93\\66\\61\\103\\40\\45\\49\\48\\49\\48\\51\\54\\54\\43\\51\\50\\49\\48\\52\\50\\56\\44\\123\\125\\41\\110\\61\\121\\91\\107\\93\\107\\61\\123\\110\\40\\66\\41\\125\\85\\61\\123\\89\\40\\107\\41\\125\\110\\61\\40\\45\\56\\56\\51\\52\\51\\55\\43\\52\\53\\55\\57\\55\\57\\41\\43\\52\\50\\53\\52\\54\\48\\32\\74\\61\\85\\91\\110\\93\\82\\61\\118\\40\\74\\41\\118\\61\\98\\40\\56\\52\\56\\53\\56\\48\\43\\40\\45\\57\\57\\56\\48\\53\\48\\45\\40\\45\\49\\55\\51\\57\\50\\48\\41\\41\\41\\84\\61\\72\\40\\82\\44\\118\\41\\72\\61\\123\\84\\40\\41\\125\\81\\61\\113\\40\\89\\40\\72\\41\\41\\84\\61\\100\\91\\83\\91\\45\\49\\49\\52\\57\\55\\55\\45\\40\\45\\49\\49\\52\\57\\56\\50\\41\\93\\93\\113\\61\\84\\32\\97\\110\\100\\32\\49\\52\\51\\50\\54\\48\\48\\48\\45\\40\\45\\55\\56\\49\\56\\50\\54\\43\\49\\49\\50\\54\\50\\54\\54\\41\\111\\114\\45\\51\\49\\56\\52\\53\\49\\43\\49\\49\\55\\55\\54\\53\\54\\55\\32\\72\\61\\81\\32\\81\\61\\84\\32\\101\\108\\115\\101\\32\\113\\61\\50\\51\\52\\49\\57\\53\\45\\40\\45\\56\\51\\57\\57\\50\\55\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\118\\61\\45\\50\\55\\49\\49\\50\\45\\40\\45\\50\\55\\49\\49\\50\\41\\72\\61\\98\\40\\45\\55\\56\\56\\56\\53\\51\\45\\40\\40\\49\\54\\52\\54\\51\\56\\50\\45\\55\\50\\51\\54\\57\\57\\41\\43\\45\\49\\55\\51\\53\\57\\57\\56\\41\\41\\113\\61\\121\\91\\72\\93\\84\\61\\100\\91\\83\\91\\53\\56\\48\\53\\51\\54\\45\\40\\49\\52\\54\\54\\55\\50\\56\\45\\56\\56\\54\\50\\48\\48\\41\\93\\93\\72\\61\\113\\40\\84\\44\\118\\41\\113\\61\\40\\45\\57\\57\\53\\55\\48\\45\\40\\45\\55\\48\\50\\52\\53\\48\\41\\41\\43\\54\\52\\54\\56\\49\\50\\55\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\56\\57\\54\\49\\48\\49\\49\\45\\49\\48\\56\\55\\52\\50\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\52\\57\\53\\55\\55\\55\\43\\45\\52\\50\\52\\55\\48\\55\\41\\43\\56\\51\\55\\51\\48\\55\\49\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\51\\55\\50\\57\\51\\54\\43\\55\\57\\51\\55\\49\\55\\54\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\57\\48\\48\\48\\51\\49\\49\\45\\56\\48\\55\\55\\48\\53\\32\\116\\104\\101\\110\\32\\113\\61\\45\\50\\52\\48\\56\\57\\50\\43\\53\\50\\51\\51\\57\\55\\52\\32\\101\\108\\115\\101\\32\\104\\61\\98\\40\\51\\53\\49\\49\\56\\53\\45\\51\\50\\54\\54\\57\\54\\41\\113\\61\\40\\56\\56\\51\\53\\56\\50\\45\\54\\54\\50\\54\\56\\57\\41\\43\\57\\52\\56\\50\\53\\51\\32\\109\\61\\40\\45\\53\\52\\55\\52\\54\\54\\45\\40\\45\\54\\54\\53\\56\\48\\53\\45\\40\\45\\49\\57\\49\\57\\50\\52\\41\\41\\41\\45\\40\\45\\55\\51\\54\\48\\54\\41\\101\\61\\100\\91\\66\\93\\77\\61\\50\\56\\50\\51\\52\\50\\49\\57\\52\\56\\54\\49\\54\\51\\45\\40\\45\\51\\48\\49\\51\\51\\52\\41\\86\\61\\100\\91\\84\\93\\65\\61\\100\\91\\82\\93\\97\\61\\65\\40\\104\\44\\77\\41\\80\\61\\86\\91\\97\\93\\88\\61\\101\\91\\80\\93\\80\\61\\100\\91\\66\\93\\101\\61\\88\\40\\80\\41\\70\\61\\109\\61\\61\\101\\32\\116\\61\\70\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\72\\61\\108\\91\\45\\50\\50\\56\\48\\56\\50\\43\\50\\50\\56\\48\\56\\51\\93\\84\\61\\108\\91\\45\\56\\57\\53\\49\\52\\52\\43\\56\\57\\53\\49\\52\\54\\93\\113\\61\\110\\105\\108\\32\\81\\61\\123\\113\\125\\113\\61\\121\\91\\98\\40\\45\\57\\55\\51\\53\\50\\52\\43\\57\\57\\56\\48\\50\\56\\41\\93\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\55\\55\\54\\50\\50\\52\\43\\55\\55\\57\\54\\49\\53\\53\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\56\\48\\49\\50\\55\\52\\56\\45\\40\\45\\53\\53\\54\\53\\52\\52\\41\\116\\104\\101\\110\\32\\107\\61\\68\\32\\71\\61\\98\\40\\45\\56\\53\\48\\49\\54\\50\\43\\40\\54\\55\\53\\52\\51\\56\\45\\40\\45\\49\\57\\57\\49\\57\\51\\41\\41\\41\\90\\61\\121\\91\\71\\93\\71\\61\\98\\40\\40\\45\\57\\54\\57\\48\\51\\51\\45\\40\\45\\49\\48\\49\\48\\54\\49\\51\\41\\41\\43\\45\\49\\55\\48\\55\\55\\41\\114\\61\\90\\91\\71\\93\\90\\61\\114\\40\\72\\44\\107\\41\\114\\61\\100\\91\\83\\91\\45\\49\\48\\50\\55\\50\\48\\49\\43\\49\\48\\50\\55\\50\\48\\55\\93\\93\\71\\61\\114\\40\\41\\111\\61\\90\\43\\71\\32\\116\\61\\111\\43\\85\\32\\111\\61\\45\\56\\54\\50\\50\\56\\45\\40\\45\\56\\54\\52\\56\\52\\41\\106\\61\\116\\37\\111\\32\\71\\61\\54\\54\\49\\50\\53\\52\\43\\40\\40\\40\\45\\53\\50\\50\\49\\45\\40\\45\\54\\48\\50\\50\\49\\54\\41\\41\\43\\45\\56\\53\\54\\53\\48\\55\\41\\43\\45\\52\\48\\49\\55\\52\\49\\41\\113\\61\\56\\57\\57\\51\\57\\55\\55\\45\\40\\45\\49\\48\\50\\55\\57\\50\\49\\41\\85\\61\\106\\32\\111\\61\\118\\91\\84\\93\\90\\61\\85\\43\\71\\32\\114\\61\\82\\91\\90\\93\\107\\61\\110\\105\\108\\32\\116\\61\\111\\46\\46\\114\\32\\118\\91\\84\\93\\61\\116\\32\\101\\108\\115\\101\\32\\113\\61\\121\\91\\98\\40\\50\\48\\51\\54\\50\\55\\43\\45\\49\\55\\57\\49\\55\\57\\41\\93\\118\\61\\98\\40\\40\\56\\49\\52\\53\\49\\53\\43\\45\\52\\51\\55\\50\\55\\52\\41\\43\\40\\45\\56\\48\\57\\50\\51\\48\\45\\40\\51\\54\\55\\49\\48\\43\\45\\52\\57\\51\\49\\56\\54\\41\\41\\41\\84\\61\\121\\91\\118\\93\\118\\61\\98\\40\\53\\55\\48\\50\\54\\43\\45\\51\\50\\53\\53\\48\\41\\72\\61\\84\\91\\118\\93\\118\\61\\100\\91\\83\\91\\45\\55\\57\\55\\52\\54\\57\\45\\40\\45\\55\\57\\55\\52\\55\\48\\41\\93\\93\\84\\61\\123\\72\\40\\118\\41\\125\\81\\61\\123\\89\\40\\84\\41\\125\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\123\\125\\74\\61\\51\\53\\49\\56\\52\\51\\55\\50\\51\\57\\49\\52\\53\\53\\45\\40\\55\\55\\48\\48\\51\\54\\43\\40\\55\\51\\57\\52\\50\\48\\43\\45\\49\\50\\48\\54\\56\\51\\51\\41\\41\\100\\91\\83\\91\\45\\53\\57\\53\\57\\51\\55\\45\\40\\45\\53\\57\\53\\57\\51\\57\\41\\93\\93\\61\\113\\32\\81\\61\\100\\91\\83\\91\\55\\52\\54\\53\\49\\54\\43\\45\\55\\52\\54\\53\\49\\51\\93\\93\\110\\61\\51\\53\\50\\49\\53\\55\\45\\51\\53\\49\\57\\48\\50\\32\\82\\61\\81\\32\\81\\61\\84\\37\\74\\32\\68\\61\\49\\48\\51\\52\\52\\53\\54\\43\\45\\49\\48\\51\\52\\52\\53\\53\\32\\100\\91\\83\\91\\50\\54\\50\\50\\56\\56\\45\\50\\54\\50\\50\\56\\52\\93\\93\\61\\81\\32\\107\\61\\98\\40\\52\\57\\50\\55\\48\\52\\45\\52\\54\\56\\50\\51\\53\\41\\85\\61\\84\\37\\110\\32\\115\\61\\68\\32\\110\\61\\53\\56\\57\\49\\54\\52\\45\\53\\56\\57\\49\\54\\50\\32\\68\\61\\45\\56\\56\\53\\49\\53\\53\\43\\40\\49\\51\\48\\54\\52\\50\\48\\45\\40\\45\\56\\53\\50\\49\\50\\49\\43\\40\\49\\56\\50\\56\\49\\57\\48\\45\\53\\53\\52\\56\\48\\52\\41\\41\\41\\74\\61\\85\\43\\110\\32\\100\\91\\83\\91\\45\\56\\54\\53\\49\\49\\57\\45\\40\\45\\56\\54\\53\\49\\50\\52\\41\\93\\93\\61\\74\\32\\110\\61\\121\\91\\107\\93\\107\\61\\98\\40\\45\\53\\51\\54\\51\\54\\51\\43\\53\\54\\48\\56\\55\\53\\41\\85\\61\\110\\91\\107\\93\\110\\61\\85\\40\\72\\41\\85\\61\\98\\40\\49\\48\\53\\56\\49\\43\\49\\51\\56\\56\\54\\41\\107\\61\\45\\54\\48\\48\\48\\53\\43\\40\\45\\49\\50\\53\\57\\49\\56\\45\\40\\45\\49\\56\\53\\57\\50\\52\\41\\41\\118\\91\\84\\93\\61\\85\\32\\66\\61\\110\\32\\85\\61\\54\\56\\49\\57\\57\\50\\45\\54\\56\\49\\55\\56\\48\\32\\113\\61\\45\\55\\56\\56\\53\\54\\54\\43\\49\\48\\56\\49\\48\\52\\54\\52\\32\\99\\61\\115\\60\\68\\32\\68\\61\\107\\45\\115\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\56\\55\\55\\54\\48\\53\\54\\45\\40\\45\\55\\57\\57\\55\\54\\54\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\57\\51\\53\\51\\57\\51\\53\\45\\40\\45\\49\\48\\48\\56\\57\\52\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\57\\48\\57\\51\\53\\57\\56\\45\\40\\45\\54\\51\\49\\51\\53\\51\\45\\40\\49\\56\\51\\54\\57\\50\\45\\53\\54\\49\\48\\55\\55\\41\\41\\116\\104\\101\\110\\32\\81\\61\\66\\32\\113\\61\\68\\32\\113\\61\\66\\32\\97\\110\\100\\32\\49\\54\\48\\53\\52\\49\\48\\50\\45\\52\\53\\57\\53\\51\\54\\32\\111\\114\\32\\49\\50\\56\\56\\49\\49\\51\\51\\45\\50\\56\\56\\55\\48\\52\\32\\101\\108\\115\\101\\32\\100\\91\\84\\93\\61\\114\\32\\113\\61\\100\\91\\84\\93\\113\\61\\113\\32\\97\\110\\100\\32\\56\\53\\57\\56\\49\\54\\50\\45\\56\\52\\48\\53\\57\\53\\32\\111\\114\\45\\49\\56\\54\\52\\57\\51\\43\\40\\45\\57\\50\\54\\54\\50\\48\\43\\49\\54\\55\\49\\51\\52\\54\\53\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\111\\61\\35\\106\\32\\70\\61\\45\\56\\48\\52\\53\\51\\56\\45\\40\\45\\56\\48\\52\\53\\51\\56\\41\\116\\61\\111\\61\\61\\70\\32\\113\\61\\116\\32\\97\\110\\100\\40\\40\\45\\54\\56\\53\\48\\56\\54\\45\\40\\45\\56\\53\\57\\56\\48\\41\\41\\43\\40\\57\\54\\56\\51\\56\\50\\43\\49\\51\\48\\56\\49\\48\\55\\49\\41\\41\\45\\51\\56\\52\\52\\48\\50\\32\\111\\114\\32\\50\\48\\49\\52\\48\\54\\43\\49\\52\\54\\52\\52\\52\\52\\48\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\45\\56\\52\\54\\52\\56\\51\\43\\49\\48\\57\\57\\52\\57\\55\\56\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\57\\54\\50\\55\\55\\55\\43\\40\\45\\53\\50\\55\\55\\57\\54\\43\\57\\53\\57\\56\\53\\53\\49\\41\\116\\104\\101\\110\\32\\106\\61\\110\\111\\116\\32\\99\\32\\68\\61\\68\\43\\115\\32\\107\\61\\68\\60\\61\\66\\32\\107\\61\\106\\32\\97\\110\\100\\32\\107\\32\\106\\61\\68\\62\\61\\66\\32\\106\\61\\99\\32\\97\\110\\100\\32\\106\\32\\107\\61\\106\\32\\111\\114\\32\\107\\32\\106\\61\\52\\56\\50\\53\\52\\48\\43\\56\\48\\56\\54\\50\\57\\53\\32\\113\\61\\107\\32\\97\\110\\100\\32\\106\\32\\107\\61\\45\\52\\57\\49\\55\\52\\57\\43\\49\\52\\55\\55\\48\\49\\50\\54\\32\\113\\61\\113\\32\\111\\114\\32\\107\\32\\101\\108\\115\\101\\32\\113\\61\\110\\105\\108\\32\\81\\61\\123\\113\\125\\113\\61\\121\\91\\98\\40\\56\\54\\52\\53\\48\\55\\45\\56\\52\\48\\48\\52\\56\\41\\93\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\56\\53\\51\\56\\51\\54\\43\\40\\51\\51\\55\\48\\57\\55\\43\\49\\51\\54\\53\\52\\57\\49\\51\\41\\70\\61\\45\\49\\53\\49\\49\\56\\48\\43\\49\\53\\49\\49\\56\\48\\32\\111\\61\\35\\106\\32\\116\\61\\111\\61\\61\\70\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\45\\54\\48\\53\\57\\50\\49\\43\\49\\51\\54\\54\\48\\50\\49\\56\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\57\\49\\55\\56\\52\\55\\43\\49\\48\\57\\48\\52\\57\\48\\56\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\50\\49\\53\\56\\55\\56\\55\\45\\49\\48\\48\\57\\51\\55\\56\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\50\\51\\54\\55\\55\\43\\49\\49\\48\\48\\53\\56\\56\\54\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\48\\48\\54\\56\\54\\56\\50\\45\\40\\45\\57\\48\\52\\57\\48\\51\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\48\\48\\55\\50\\53\\57\\50\\45\\40\\45\\52\\57\\48\\52\\54\\51\\41\\116\\104\\101\\110\\32\\118\\61\\50\\48\\53\\48\\54\\50\\45\\50\\48\\53\\48\\51\\48\\32\\84\\61\\100\\91\\83\\91\\40\\53\\57\\49\\52\\50\\51\\43\\45\\53\\51\\54\\52\\53\\49\\41\\45\\53\\52\\57\\54\\57\\93\\93\\115\\61\\52\\54\\51\\48\\50\\49\\45\\52\\54\\51\\48\\48\\56\\32\\72\\61\\84\\37\\118\\32\\66\\61\\40\\45\\53\\56\\56\\54\\54\\50\\43\\49\\52\\53\\50\\52\\48\\51\\41\\43\\45\\56\\54\\51\\55\\51\\57\\32\\82\\61\\100\\91\\83\\91\\45\\50\\55\\54\\50\\50\\49\\43\\40\\55\\49\\48\\51\\53\\57\\43\\45\\52\\51\\52\\49\\51\\52\\41\\93\\93\\110\\61\\100\\91\\83\\91\\52\\56\\52\\53\\51\\49\\45\\52\\56\\52\\53\\50\\57\\93\\93\\116\\61\\100\\91\\83\\91\\51\\48\\51\\56\\52\\48\\45\\51\\48\\51\\56\\51\\55\\93\\93\\106\\61\\116\\45\\72\\32\\116\\61\\45\\57\\49\\48\\53\\55\\53\\43\\57\\49\\48\\54\\48\\55\\32\\99\\61\\106\\47\\116\\32\\68\\61\\115\\45\\99\\32\\107\\61\\66\\94\\68\\32\\85\\61\\110\\47\\107\\32\\74\\61\\82\\40\\85\\41\\66\\61\\50\\54\\52\\51\\57\\54\\45\\50\\54\\52\\49\\52\\48\\32\\107\\61\\40\\40\\40\\45\\57\\49\\55\\57\\56\\52\\43\\40\\49\\49\\51\\50\\54\\56\\54\\45\\40\\49\\49\\54\\56\\49\\49\\43\\55\\48\\54\\54\\50\\53\\41\\41\\41\\43\\49\\49\\55\\53\\49\\50\\51\\41\\45\\40\\45\\50\\48\\52\\56\\49\\51\\41\\41\\45\\55\\55\\49\\50\\48\\49\\32\\82\\61\\52\\50\\57\\53\\53\\52\\52\\52\\53\\51\\45\\53\\55\\55\\49\\53\\55\\32\\118\\61\\74\\37\\82\\32\\74\\61\\45\\54\\49\\51\\57\\52\\56\\43\\54\\49\\51\\57\\53\\48\\32\\82\\61\\74\\94\\72\\32\\84\\61\\118\\47\\82\\32\\82\\61\\100\\91\\83\\91\\57\\54\\52\\52\\53\\54\\45\\40\\49\\53\\52\\56\\57\\55\\49\\45\\53\\56\\52\\53\\49\\57\\41\\93\\93\\110\\61\\84\\37\\107\\32\\107\\61\\57\\57\\57\\48\\56\\52\\43\\52\\50\\57\\51\\57\\54\\56\\50\\49\\50\\32\\85\\61\\110\\42\\107\\32\\74\\61\\82\\40\\85\\41\\82\\61\\100\\91\\83\\91\\50\\55\\48\\56\\48\\54\\45\\50\\55\\48\\56\\48\\50\\93\\93\\110\\61\\40\\45\\50\\54\\49\\55\\53\\57\\43\\45\\51\\57\\53\\50\\50\\55\\41\\43\\40\\40\\52\\55\\55\\51\\53\\53\\45\\40\\49\\57\\56\\49\\54\\45\\51\\49\\48\\54\\49\\54\\41\\41\\43\\45\\52\\53\\54\\51\\51\\41\\85\\61\\82\\40\\84\\41\\84\\61\\110\\105\\108\\32\\118\\61\\74\\43\\85\\32\\74\\61\\49\\55\\50\\53\\50\\56\\45\\40\\52\\51\\48\\50\\52\\51\\43\\45\\51\\50\\51\\50\\53\\49\\41\\82\\61\\118\\37\\74\\32\\85\\61\\118\\45\\82\\32\\74\\61\\85\\47\\110\\32\\110\\61\\40\\50\\57\\55\\53\\49\\48\\43\\45\\51\\54\\52\\55\\53\\55\\41\\43\\54\\55\\53\\48\\51\\32\\85\\61\\82\\37\\110\\32\\107\\61\\82\\45\\85\\32\\110\\61\\107\\47\\66\\32\\118\\61\\110\\105\\108\\32\\66\\61\\45\\50\\52\\57\\48\\57\\50\\45\\40\\40\\45\\52\\54\\49\\56\\52\\55\\45\\40\\45\\53\\52\\50\\57\\49\\52\\41\\41\\43\\45\\51\\51\\48\\52\\49\\53\\41\\107\\61\\74\\37\\66\\32\\68\\61\\74\\45\\107\\32\\72\\61\\110\\105\\108\\32\\74\\61\\110\\105\\108\\32\\113\\61\\55\\56\\54\\54\\56\\49\\53\\45\\40\\45\\55\\48\\51\\55\\50\\49\\41\\115\\61\\50\\48\\54\\49\\48\\57\\43\\45\\50\\48\\53\\56\\53\\51\\32\\66\\61\\68\\47\\115\\32\\82\\61\\110\\105\\108\\32\\68\\61\\123\\85\\59\\110\\59\\107\\59\\66\\125\\110\\61\\110\\105\\108\\32\\100\\91\\83\\91\\45\\50\\53\\57\\48\\56\\48\\43\\50\\53\\57\\48\\56\\49\\93\\93\\61\\68\\32\\66\\61\\110\\105\\108\\32\\85\\61\\110\\105\\108\\32\\107\\61\\110\\105\\108\\32\\101\\108\\115\\101\\32\\81\\61\\98\\40\\50\\48\\50\\48\\54\\53\\43\\45\\49\\55\\55\\54\\48\\51\\41\\72\\61\\98\\40\\40\\52\\56\\56\\51\\57\\54\\45\\40\\49\\48\\51\\56\\57\\56\\48\\45\\49\\51\\49\\51\\54\\49\\41\\41\\45\\40\\45\\52\\52\\51\\54\\56\\51\\41\\41\\113\\61\\121\\91\\81\\93\\81\\61\\113\\40\\72\\41\\81\\61\\123\\125\\113\\61\\121\\91\\98\\40\\45\\56\\50\\49\\57\\51\\52\\43\\56\\52\\54\\52\\51\\54\\41\\93\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\49\\48\\51\\56\\49\\53\\48\\51\\45\\40\\45\\54\\50\\56\\57\\50\\55\\41\\116\\104\\101\\110\\32\\116\\61\\100\\91\\118\\93\\113\\61\\45\\54\\57\\52\\57\\54\\50\\43\\40\\40\\49\\49\\57\\50\\50\\52\\55\\53\\45\\40\\45\\55\\50\\49\\51\\52\\57\\41\\41\\45\\40\\45\\57\\52\\49\\52\\51\\55\\41\\41\\101\\61\\98\\40\\56\\54\\48\\53\\51\\55\\43\\45\\56\\51\\54\\48\\55\\52\\41\\70\\61\\100\\91\\84\\93\\109\\61\\100\\91\\82\\93\\80\\61\\49\\49\\55\\50\\52\\55\\53\\48\\57\\50\\50\\55\\57\\52\\45\\50\\53\\54\\55\\51\\32\\88\\61\\109\\40\\101\\44\\80\\41\\111\\61\\70\\91\\88\\93\\106\\61\\116\\91\\111\\93\\111\\61\\100\\91\\118\\93\\70\\61\\45\\52\\54\\49\\51\\51\\57\\43\\40\\45\\49\\52\\56\\57\\55\\52\\43\\54\\49\\48\\51\\49\\53\\41\\116\\61\\106\\40\\111\\44\\70\\41\\106\\61\\45\\56\\48\\51\\55\\52\\51\\45\\40\\45\\56\\48\\51\\55\\52\\52\\41\\99\\61\\116\\43\\106\\32\\106\\61\\53\\53\\56\\50\\50\\49\\45\\53\\53\\56\\49\\50\\49\\32\\115\\61\\99\\61\\61\\106\\32\\66\\61\\115\\32\\101\\108\\115\\101\\32\\113\\61\\121\\91\\98\\40\\45\\57\\50\\48\\51\\53\\48\\43\\57\\52\\52\\56\\52\\56\\41\\93\\81\\61\\123\\125\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\57\\52\\49\\56\\51\\49\\43\\49\\48\\49\\54\\49\\50\\50\\50\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\45\\57\\51\\57\\51\\51\\50\\43\\49\\49\\53\\54\\54\\52\\52\\55\\41\\45\\40\\51\\56\\49\\48\\55\\49\\43\\45\\55\\56\\56\\49\\56\\54\\41\\116\\104\\101\\110\\32\\77\\61\\40\\50\\57\\55\\55\\48\\50\\45\\40\\45\\52\\51\\54\\53\\49\\54\\41\\41\\43\\45\\55\\51\\52\\50\\49\\54\\32\\104\\61\\65\\91\\77\\93\\77\\61\\100\\91\\86\\93\\113\\61\\52\\55\\48\\51\\55\\49\\43\\51\\55\\55\\52\\49\\53\\56\\32\\97\\61\\104\\61\\61\\77\\32\\119\\61\\97\\32\\101\\108\\115\\101\\32\\88\\61\\100\\91\\66\\93\\109\\61\\35\\88\\32\\88\\61\\53\\48\\53\\55\\52\\49\\43\\45\\53\\48\\53\\55\\51\\50\\32\\111\\61\\113\\32\\70\\61\\109\\61\\61\\88\\32\\116\\61\\70\\32\\113\\61\\70\\32\\97\\110\\100\\32\\56\\48\\52\\48\\56\\49\\55\\45\\40\\49\\53\\50\\52\\55\\55\\43\\45\\51\\53\\51\\54\\48\\51\\41\\111\\114\\45\\51\\50\\52\\52\\50\\52\\43\\49\\52\\57\\51\\53\\55\\48\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\120\\40\\49\\51\\56\\48\\51\\53\\52\\55\\45\\55\\55\\52\\53\\51\\50\\44\\123\\82\\125\\41\\90\\61\\123\\113\\40\\41\\125\\113\\61\\121\\91\\98\\40\\45\\53\\51\\54\\57\\48\\57\\45\\40\\45\\53\\54\\49\\51\\57\\53\\41\\41\\93\\81\\61\\123\\89\\40\\90\\41\\125\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\45\\56\\53\\56\\52\\54\\54\\43\\49\\50\\51\\50\\48\\50\\52\\55\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\57\\53\\53\\54\\51\\54\\43\\45\\57\\56\\57\\49\\52\\50\\41\\43\\49\\49\\51\\56\\52\\51\\55\\51\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\49\\54\\48\\55\\49\\56\\43\\49\\49\\50\\52\\50\\52\\55\\52\\41\\45\\49\\53\\48\\48\\48\\55\\32\\116\\104\\101\\110\\32\\81\\61\\74\\32\\113\\61\\85\\32\\113\\61\\45\\53\\56\\48\\48\\51\\43\\49\\51\\49\\57\\52\\50\\50\\48\\32\\101\\108\\115\\101\\32\\113\\61\\121\\91\\98\\40\\57\\55\\56\\52\\53\\57\\43\\45\\57\\53\\52\\48\\48\\52\\41\\93\\81\\61\\123\\125\\101\\110\\100\\32\\101\\108\\115\\101\\32\\100\\91\\83\\91\\49\\48\\49\\48\\56\\57\\54\\43\\45\\49\\48\\49\\48\\56\\57\\49\\93\\93\\61\\81\\32\\72\\61\\110\\105\\108\\32\\113\\61\\40\\49\\48\\50\\51\\54\\48\\50\\45\\49\\53\\51\\51\\54\\48\\41\\43\\40\\50\\48\\50\\50\\55\\50\\52\\45\\52\\53\\50\\49\\56\\50\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\45\\53\\49\\56\\52\\48\\56\\43\\49\\50\\50\\57\\54\\56\\48\\51\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\50\\49\\56\\50\\52\\57\\43\\50\\56\\51\\48\\54\\51\\41\\43\\49\\49\\49\\52\\55\\51\\53\\56\\32\\116\\104\\101\\110\\32\\73\\61\\98\\40\\49\\57\\49\\51\\48\\48\\45\\49\\54\\54\\56\\51\\54\\41\\117\\61\\40\\45\\50\\55\\53\\49\\50\\48\\45\\54\\53\\56\\53\\48\\53\\41\\45\\40\\45\\49\\51\\49\\49\\57\\48\\57\\45\\40\\45\\51\\54\\56\\50\\56\\52\\41\\41\\109\\61\\75\\40\\41\\101\\61\\40\\40\\45\\49\\48\\50\\55\\57\\54\\57\\43\\49\\48\\54\\53\\56\\49\\51\\41\\43\\45\\57\\57\\53\\53\\56\\54\\41\\45\\40\\45\\49\\54\\53\\48\\52\\50\\43\\45\\55\\57\\50\\57\\53\\53\\41\\88\\61\\45\\51\\49\\52\\50\\43\\51\\50\\52\\50\\32\\100\\91\\109\\93\\61\\114\\32\\81\\61\\121\\91\\73\\93\\73\\61\\98\\40\\45\\54\\55\\52\\52\\52\\49\\43\\40\\45\\51\\54\\54\\53\\53\\56\\43\\40\\49\\52\\52\\56\\57\\52\\57\\45\\51\\56\\51\\52\\55\\49\\41\\41\\41\\113\\61\\81\\91\\73\\93\\73\\61\\45\\49\\48\\51\\55\\57\\54\\43\\49\\48\\51\\55\\57\\55\\32\\81\\61\\113\\40\\73\\44\\88\\41\\73\\61\\75\\40\\41\\100\\91\\73\\93\\61\\81\\32\\88\\61\\45\\51\\53\\51\\53\\48\\45\\40\\49\\48\\51\\49\\56\\51\\53\\43\\45\\49\\48\\54\\55\\49\\56\\53\\41\\113\\61\\100\\91\\110\\93\\81\\61\\113\\40\\88\\44\\101\\41\\101\\61\\45\\57\\56\\52\\51\\52\\48\\45\\40\\45\\57\\56\\52\\51\\52\\49\\41\\88\\61\\75\\40\\41\\100\\91\\88\\93\\61\\81\\32\\113\\61\\100\\91\\110\\93\\86\\61\\40\\45\\51\\51\\48\\50\\52\\54\\45\\51\\54\\56\\57\\53\\55\\41\\45\\40\\45\\54\\57\\57\\50\\48\\52\\41\\80\\61\\100\\91\\73\\93\\81\\61\\113\\40\\101\\44\\80\\41\\97\\61\\98\\40\\57\\57\\48\\51\\56\\53\\43\\40\\56\\49\\56\\52\\57\\50\\43\\45\\49\\55\\56\\52\\51\\54\\49\\41\\41\\101\\61\\75\\40\\41\\65\\61\\45\\57\\51\\48\\49\\43\\57\\51\\48\\51\\32\\100\\91\\101\\93\\61\\81\\32\\81\\61\\100\\91\\110\\93\\80\\61\\81\\40\\86\\44\\65\\41\\105\\61\\55\\56\\55\\51\\57\\51\\43\\45\\55\\56\\55\\51\\57\\51\\32\\81\\61\\45\\54\\55\\50\\52\\49\\50\\45\\40\\45\\54\\55\\50\\52\\49\\51\\41\\113\\61\\80\\61\\61\\81\\32\\65\\61\\98\\40\\57\\56\\52\\52\\52\\49\\43\\45\\57\\53\\57\\57\\54\\56\\41\\80\\61\\75\\40\\41\\81\\61\\98\\40\\45\\55\\53\\55\\50\\51\\55\\43\\55\\56\\49\\54\\56\\55\\41\\100\\91\\80\\93\\61\\113\\32\\113\\61\\98\\40\\40\\45\\53\\56\\51\\49\\57\\56\\43\\40\\45\\54\\50\\50\\57\\49\\57\\43\\49\\48\\53\\56\\54\\52\\56\\41\\41\\45\\40\\45\\49\\55\\49\\57\\50\\48\\41\\41\\78\\61\\121\\91\\97\\93\\104\\61\\100\\91\\110\\93\\113\\61\\116\\91\\113\\93\\77\\61\\123\\104\\40\\105\\44\\117\\41\\125\\97\\61\\78\\40\\89\\40\\77\\41\\41\\78\\61\\98\\40\\45\\56\\56\\48\\50\\57\\54\\45\\40\\45\\57\\48\\52\\55\\54\\57\\41\\41\\119\\61\\97\\46\\46\\78\\32\\86\\61\\65\\46\\46\\119\\32\\65\\61\\98\\40\\40\\40\\49\\54\\54\\57\\51\\49\\43\\45\\49\\48\\50\\48\\48\\48\\51\\41\\43\\50\\53\\55\\52\\50\\50\\41\\45\\40\\45\\54\\50\\48\\49\\53\\49\\41\\41\\113\\61\\113\\40\\116\\44\\81\\44\\86\\41\\86\\61\\75\\40\\41\\119\\61\\76\\40\\45\\53\\53\\57\\56\\54\\52\\43\\49\\53\\54\\53\\54\\52\\53\\51\\44\\123\\110\\44\\109\\59\\68\\59\\118\\44\\84\\44\\111\\44\\80\\59\\86\\59\\73\\59\\101\\44\\88\\59\\66\\125\\41\\100\\91\\86\\93\\61\\113\\32\\81\\61\\121\\91\\65\\93\\65\\61\\123\\81\\40\\119\\41\\125\\113\\61\\123\\89\\40\\65\\41\\125\\65\\61\\113\\32\\113\\61\\100\\91\\80\\93\\113\\61\\113\\32\\97\\110\\100\\40\\57\\52\\54\\54\\54\\51\\43\\45\\57\\49\\56\\50\\51\\55\\41\\43\\54\\53\\51\\57\\52\\49\\52\\32\\111\\114\\40\\54\\55\\52\\54\\52\\56\\43\\52\\52\\52\\52\\54\\49\\50\\41\\45\\40\\45\\52\\50\\56\\54\\51\\41\\101\\108\\115\\101\\32\\118\\61\\79\\40\\118\\41\\107\\61\\79\\40\\107\\41\\113\\61\\40\\45\\55\\53\\54\\57\\57\\54\\43\\40\\52\\56\\52\\50\\48\\50\\45\\40\\54\\57\\57\\57\\54\\57\\43\\40\\40\\40\\51\\56\\51\\52\\56\\43\\53\\57\\52\\55\\57\\48\\41\\43\\40\\50\\48\\53\\48\\54\\56\\45\\40\\49\\55\\54\\56\\55\\50\\57\\45\\57\\48\\54\\56\\50\\55\\41\\41\\41\\43\\45\\57\\49\\51\\57\\49\\52\\41\\41\\41\\41\\43\\49\\51\\48\\48\\54\\53\\57\\50\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\77\\61\\55\\55\\56\\55\\50\\51\\45\\55\\55\\56\\55\\50\\50\\32\\78\\61\\113\\32\\104\\61\\65\\91\\77\\93\\77\\61\\102\\97\\108\\115\\101\\32\\97\\61\\104\\61\\61\\77\\32\\113\\61\\97\\32\\97\\110\\100\\45\\53\\55\\55\\56\\50\\55\\43\\40\\40\\40\\49\\51\\49\\52\\54\\48\\54\\50\\45\\55\\49\\52\\56\\48\\49\\41\\45\\57\\57\\54\\53\\54\\51\\41\\45\\40\\45\\49\\55\\52\\48\\49\\48\\41\\41\\111\\114\\40\\40\\49\\48\\52\\53\\52\\55\\51\\43\\52\\51\\56\\56\\56\\49\\48\\41\\45\\51\\49\\53\\57\\49\\53\\41\\45\\56\\55\\51\\56\\51\\57\\32\\119\\61\\97\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\40\\45\\54\\51\\55\\56\\48\\45\\40\\45\\54\\49\\54\\52\\49\\57\\41\\41\\43\\40\\40\\52\\50\\53\\49\\52\\52\\43\\40\\53\\53\\55\\49\\51\\43\\40\\45\\49\\55\\48\\56\\50\\51\\52\\45\\40\\45\\56\\54\\55\\55\\57\\53\\45\\40\\45\\52\\56\\54\\48\\54\\51\\41\\41\\41\\41\\41\\43\\49\\50\\55\\54\\48\\52\\51\\55\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\49\\55\\54\\49\\57\\51\\57\\45\\40\\45\\51\\57\\52\\51\\49\\57\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\54\\48\\51\\48\\56\\43\\49\\49\\56\\52\\52\\55\\50\\57\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\49\\51\\49\\56\\54\\50\\55\\45\\40\\45\\53\\49\\55\\49\\57\\56\\41\\116\\104\\101\\110\\32\\118\\61\\100\\91\\83\\91\\40\\45\\49\\54\\49\\49\\48\\57\\45\\40\\45\\56\\48\\48\\56\\52\\54\\41\\41\\43\\45\\54\\51\\57\\55\\51\\52\\93\\93\\107\\61\\98\\40\\45\\52\\55\\52\\55\\54\\55\\45\\40\\45\\52\\57\\57\\50\\50\\49\\41\\41\\82\\61\\100\\91\\83\\91\\55\\55\\48\\54\\53\\52\\43\\45\\55\\55\\48\\54\\53\\48\\93\\93\\110\\61\\40\\49\\52\\55\\48\\51\\53\\55\\54\\57\\55\\53\\57\\48\\51\\45\\49\\50\\48\\57\\48\\49\\41\\45\\53\\48\\55\\53\\51\\54\\32\\66\\61\\50\\55\\51\\51\\57\\49\\43\\50\\55\\53\\48\\53\\56\\51\\48\\52\\55\\51\\48\\53\\48\\32\\85\\61\\98\\40\\56\\53\\55\\48\\52\\57\\45\\56\\51\\50\\53\\54\\49\\41\\74\\61\\82\\40\\85\\44\\110\\41\\81\\61\\118\\91\\74\\93\\113\\61\\84\\91\\81\\93\\74\\61\\100\\91\\83\\91\\49\\54\\56\\48\\56\\49\\45\\49\\54\\56\\48\\55\\56\\93\\93\\85\\61\\100\\91\\83\\91\\49\\56\\53\\51\\50\\55\\43\\45\\49\\56\\53\\51\\50\\51\\93\\93\\110\\61\\85\\40\\107\\44\\66\\41\\82\\61\\74\\91\\110\\93\\118\\61\\84\\91\\82\\93\\74\\61\\45\\51\\56\\57\\57\\55\\55\\43\\40\\51\\52\\50\\57\\54\\54\\45\\40\\40\\40\\45\\49\\53\\50\\55\\53\\56\\50\\45\\40\\45\\54\\57\\56\\51\\52\\57\\41\\41\\45\\40\\45\\51\\51\\56\\48\\54\\57\\41\\41\\45\\40\\45\\52\\52\\52\\49\\53\\49\\41\\41\\41\\82\\61\\118\\40\\84\\44\\74\\41\\74\\61\\100\\91\\83\\91\\45\\51\\48\\56\\54\\49\\53\\45\\40\\45\\51\\48\\56\\54\\49\\56\\41\\93\\93\\107\\61\\98\\40\\53\\49\\53\\54\\50\\54\\43\\45\\52\\57\\49\\49\\48\\56\\41\\85\\61\\100\\91\\83\\91\\45\\49\\54\\50\\48\\55\\54\\45\\40\\45\\49\\54\\50\\48\\56\\48\\41\\93\\93\\66\\61\\49\\54\\49\\56\\51\\55\\56\\57\\51\\51\\56\\52\\51\\56\\45\\40\\49\\51\\48\\51\\48\\54\\45\\40\\45\\53\\51\\49\\54\\57\\56\\41\\41\\110\\61\\85\\40\\107\\44\\66\\41\\118\\61\\74\\91\\110\\93\\85\\61\\116\\114\\117\\101\\32\\74\\61\\40\\45\\50\\54\\57\\48\\57\\57\\45\\40\\57\\48\\48\\52\\56\\55\\45\\52\\56\\48\\52\\52\\52\\41\\41\\45\\40\\45\\54\\56\\57\\49\\52\\52\\41\\81\\61\\113\\40\\82\\44\\118\\44\\74\\44\\85\\41\\82\\61\\45\\53\\52\\51\\54\\55\\56\\43\\40\\45\\51\\52\\57\\52\\48\\43\\40\\52\\48\\52\\57\\55\\43\\40\\40\\45\\53\\51\\48\\48\\50\\53\\43\\49\\57\\57\\48\\49\\55\\52\\41\\45\\57\\50\\50\\48\\50\\55\\41\\41\\41\\118\\61\\81\\32\\81\\61\\118\\91\\82\\93\\74\\61\\100\\91\\83\\91\\45\\54\\57\\52\\49\\51\\52\\43\\54\\57\\52\\49\\51\\55\\93\\93\\66\\61\\53\\54\\51\\50\\50\\53\\43\\52\\54\\56\\48\\57\\57\\51\\54\\54\\53\\52\\48\\55\\32\\68\\61\\55\\49\\56\\56\\53\\56\\57\\48\\54\\56\\54\\50\\48\\45\\40\\45\\49\\51\\49\\49\\48\\53\\41\\107\\61\\98\\40\\52\\54\\55\\52\\55\\55\\45\\52\\52\\50\\57\\53\\55\\41\\85\\61\\100\\91\\83\\91\\56\\49\\52\\56\\48\\57\\45\\56\\49\\52\\56\\48\\53\\93\\93\\110\\61\\85\\40\\107\\44\\66\\41\\82\\61\\74\\91\\110\\93\\113\\61\\81\\91\\82\\93\\74\\61\\40\\56\\57\\49\\54\\51\\50\\43\\51\\48\\54\\55\\41\\43\\45\\56\\57\\52\\54\\57\\56\\32\\82\\61\\118\\91\\74\\93\\66\\61\\98\\40\\50\\53\\50\\53\\55\\54\\43\\45\\50\\50\\56\\48\\53\\53\\41\\85\\61\\100\\91\\83\\91\\40\\45\\49\\54\\48\\48\\56\\48\\57\\45\\40\\45\\54\\49\\48\\48\\52\\49\\41\\41\\43\\40\\40\\49\\48\\49\\48\\55\\50\\53\\43\\45\\51\\49\\48\\51\\54\\49\\41\\45\\40\\45\\50\\57\\48\\52\\48\\55\\41\\41\\93\\93\\110\\61\\100\\91\\83\\91\\40\\40\\50\\57\\50\\53\\56\\52\\43\\45\\49\\49\\56\\48\\48\\52\\52\\41\\45\\40\\45\\49\\48\\54\\51\\51\\49\\41\\41\\43\\55\\56\\49\\49\\51\\51\\93\\93\\107\\61\\110\\40\\66\\44\\68\\41\\74\\61\\85\\91\\107\\93\\110\\61\\102\\97\\108\\115\\101\\32\\107\\61\\102\\97\\108\\115\\101\\32\\85\\61\\40\\45\\57\\53\\48\\56\\54\\48\\43\\40\\45\\49\\51\\51\\56\\48\\53\\43\\54\\48\\48\\56\\51\\41\\41\\43\\49\\48\\50\\52\\53\\56\\53\\32\\81\\61\\113\\40\\82\\44\\74\\44\\85\\44\\110\\44\\107\\41\\82\\61\\81\\32\\85\\61\\35\\82\\32\\110\\61\\45\\55\\53\\49\\49\\49\\51\\45\\40\\45\\55\\53\\49\\49\\49\\53\\41\\74\\61\\85\\61\\61\\110\\32\\81\\61\\74\\32\\113\\61\\74\\32\\97\\110\\100\\32\\50\\57\\53\\52\\50\\52\\51\\45\\53\\49\\52\\55\\54\\50\\32\\111\\114\\45\\53\\54\\49\\52\\54\\53\\43\\49\\51\\54\\57\\55\\54\\56\\50\\32\\101\\108\\115\\101\\32\\113\\61\\116\\114\\117\\101\\32\\84\\61\\75\\40\\41\\72\\61\\108\\32\\100\\91\\84\\93\\61\\113\\32\\118\\61\\98\\40\\53\\56\\57\\57\\50\\55\\45\\53\\54\\53\\52\\53\\56\\41\\82\\61\\75\\40\\41\\81\\61\\121\\91\\118\\93\\110\\61\\98\\40\\45\\51\\53\\56\\48\\51\\55\\45\\40\\45\\50\\51\\48\\54\\53\\57\\43\\45\\49\\53\\49\\56\\55\\57\\41\\41\\118\\61\\98\\40\\40\\40\\45\\55\\54\\53\\53\\50\\54\\43\\40\\40\\57\\57\\48\\54\\53\\50\\45\\40\\45\\52\\52\\51\\52\\51\\54\\43\\57\\48\\57\\48\\55\\49\\41\\41\\45\\40\\45\\51\\49\\49\\50\\48\\57\\41\\41\\41\\43\\57\\55\\52\\55\\49\\57\\41\\43\\45\\49\\48\\50\\48\\57\\49\\48\\41\\113\\61\\81\\91\\118\\93\\118\\61\\75\\40\\41\\74\\61\\75\\40\\41\\100\\91\\118\\93\\61\\113\\32\\113\\61\\87\\40\\45\\51\\51\\48\\48\\56\\57\\43\\49\\49\\50\\57\\50\\57\\56\\51\\44\\123\\125\\41\\107\\61\\103\\40\\51\\56\\55\\51\\49\\53\\43\\51\\55\\50\\51\\57\\57\\51\\44\\123\\74\\125\\41\\100\\91\\82\\93\\61\\113\\32\\113\\61\\102\\97\\108\\115\\101\\32\\100\\91\\74\\93\\61\\113\\32\\85\\61\\121\\91\\110\\93\\110\\61\\85\\40\\107\\41\\81\\61\\110\\32\\113\\61\\110\\32\\97\\110\\100\\32\\54\\48\\56\\55\\51\\57\\43\\40\\40\\51\\52\\53\\50\\54\\53\\52\\45\\40\\45\\50\\54\\51\\53\\57\\52\\41\\41\\45\\40\\45\\53\\55\\54\\49\\51\\57\\41\\41\\111\\114\\45\\52\\50\\54\\55\\48\\57\\43\\49\\51\\51\\49\\50\\48\\54\\51\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\81\\61\\98\\40\\45\\57\\48\\49\\52\\57\\56\\43\\57\\50\\53\\57\\55\\50\\41\\113\\61\\121\\91\\81\\93\\72\\61\\98\\40\\45\\56\\53\\55\\51\\55\\55\\45\\40\\45\\56\\56\\49\\56\\52\\55\\41\\41\\81\\61\\121\\91\\72\\93\\72\\61\\98\\40\\40\\45\\50\\51\\51\\55\\52\\55\\45\\40\\45\\55\\50\\55\\52\\54\\57\\41\\41\\43\\45\\52\\54\\57\\50\\53\\50\\41\\121\\91\\72\\93\\61\\113\\32\\72\\61\\98\\40\\55\\51\\55\\51\\56\\49\\45\\55\\49\\50\\57\\48\\55\\41\\121\\91\\72\\93\\61\\81\\32\\72\\61\\100\\91\\83\\91\\45\\56\\55\\49\\50\\50\\55\\43\\56\\55\\49\\50\\50\\56\\93\\93\\113\\61\\55\\54\\48\\54\\48\\54\\49\\45\\55\\53\\49\\56\\54\\55\\32\\84\\61\\72\\40\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\40\\50\\48\\53\\53\\56\\52\\45\\40\\45\\55\\50\\54\\49\\57\\54\\41\\41\\43\\49\\49\\51\\50\\53\\48\\48\\51\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\50\\54\\56\\56\\50\\48\\54\\45\\53\\48\\55\\52\\51\\56\\32\\116\\104\\101\\110\\32\\68\\61\\98\\40\\40\\40\\56\\53\\56\\50\\56\\55\\43\\45\\52\\56\\55\\56\\57\\50\\41\\43\\49\\50\\48\\49\\54\\54\\41\\43\\40\\52\\54\\51\\49\\53\\57\\43\\45\\57\\50\\57\\50\\51\\55\\41\\41\\113\\61\\53\\53\\48\\52\\55\\50\\43\\51\\56\\57\\52\\48\\51\\48\\32\\66\\61\\121\\91\\68\\93\\106\\61\\100\\91\\107\\93\\116\\61\\123\\66\\40\\106\\41\\125\\99\\61\\116\\91\\50\\57\\48\\48\\53\\56\\45\\40\\54\\49\\53\\52\\49\\54\\45\\51\\50\\53\\51\\54\\49\\41\\93\\68\\61\\116\\91\\49\\55\\48\\48\\56\\51\\43\\45\\49\\55\\48\\48\\56\\50\\93\\115\\61\\116\\91\\50\\52\\51\\51\\55\\55\\43\\45\\50\\52\\51\\51\\55\\53\\93\\101\\108\\115\\101\\32\\113\\61\\100\\91\\83\\91\\57\\50\\52\\56\\56\\54\\43\\45\\57\\50\\52\\56\\55\\54\\93\\93\\84\\61\\100\\91\\83\\91\\45\\57\\49\\51\\53\\55\\45\\40\\45\\57\\49\\51\\54\\56\\41\\93\\93\\72\\91\\113\\93\\61\\84\\32\\113\\61\\100\\91\\83\\91\\45\\54\\52\\50\\50\\55\\57\\45\\40\\45\\54\\52\\50\\50\\57\\49\\41\\93\\93\\84\\61\\123\\113\\40\\72\\41\\125\\81\\61\\123\\89\\40\\84\\41\\125\\113\\61\\121\\91\\98\\40\\52\\48\\54\\50\\50\\52\\43\\45\\51\\56\\49\\55\\54\\51\\41\\93\\101\\110\\100\\32\\101\\108\\115\\101\\32\\118\\61\\45\\51\\49\\49\\48\\53\\49\\43\\51\\49\\49\\48\\53\\54\\32\\113\\61\\40\\51\\54\\54\\49\\53\\49\\45\\40\\45\\49\\56\\57\\52\\53\\57\\41\\41\\43\\53\\50\\57\\54\\56\\51\\57\\32\\84\\61\\100\\91\\83\\91\\45\\55\\57\\48\\52\\56\\56\\43\\55\\57\\48\\52\\57\\49\\93\\93\\72\\61\\84\\42\\118\\32\\84\\61\\45\\50\\48\\54\\51\\52\\54\\45\\40\\45\\50\\48\\54\\54\\48\\51\\41\\81\\61\\72\\37\\84\\32\\100\\91\\83\\91\\52\\55\\48\\53\\45\\52\\55\\48\\50\\93\\93\\61\\81\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\45\\49\\48\\49\\48\\51\\55\\49\\43\\49\\51\\56\\57\\55\\56\\53\\57\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\51\\48\\49\\52\\53\\51\\43\\49\\50\\53\\52\\52\\52\\52\\49\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\52\\51\\48\\55\\51\\57\\43\\49\\50\\51\\51\\55\\57\\54\\51\\41\\45\\49\\55\\53\\53\\48\\54\\32\\116\\104\\101\\110\\32\\68\\61\\98\\40\\40\\45\\57\\53\\51\\49\\57\\50\\43\\57\\50\\49\\55\\51\\53\\41\\45\\40\\45\\53\\53\\57\\50\\50\\41\\41\\66\\61\\121\\91\\68\\93\\81\\61\\66\\32\\113\\61\\45\\56\\53\\52\\51\\51\\54\\43\\49\\54\\52\\52\\56\\57\\48\\50\\32\\101\\108\\115\\101\\32\\113\\61\\45\\54\\48\\50\\50\\57\\54\\43\\55\\54\\56\\52\\53\\50\\54\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\107\\61\\98\\40\\54\\51\\54\\57\\57\\55\\43\\45\\54\\49\\50\\53\\49\\48\\41\\110\\61\\98\\40\\40\\49\\55\\54\\48\\50\\57\\45\\40\\45\\56\\57\\53\\55\\50\\41\\41\\43\\45\\50\\52\\49\\49\\51\\55\\41\\85\\61\\81\\32\\81\\61\\121\\91\\110\\93\\110\\61\\98\\40\\45\\53\\48\\51\\49\\57\\53\\45\\40\\50\\55\\55\\57\\53\\56\\45\\56\\48\\53\\54\\51\\50\\41\\41\\99\\61\\98\\40\\49\\49\\49\\52\\55\\45\\40\\45\\49\\51\\51\\52\\48\\41\\41\\113\\61\\81\\91\\110\\93\\110\\61\\75\\40\\41\\100\\91\\110\\93\\61\\113\\32\\81\\61\\121\\91\\107\\93\\107\\61\\98\\40\\55\\52\\54\\57\\53\\51\\43\\40\\40\\45\\56\\53\\48\\57\\49\\52\\43\\45\\49\\55\\49\\52\\57\\49\\41\\43\\50\\57\\57\\57\\51\\50\\41\\41\\113\\61\\81\\91\\107\\93\\107\\61\\113\\32\\115\\61\\121\\91\\99\\93\\68\\61\\113\\32\\113\\61\\115\\32\\97\\110\\100\\45\\55\\51\\51\\51\\48\\43\\54\\48\\57\\57\\52\\57\\55\\32\\111\\114\\32\\52\\48\\51\\53\\55\\43\\57\\50\\55\\55\\53\\56\\53\\32\\66\\61\\115\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\55\\55\\49\\53\\52\\49\\43\\49\\50\\50\\49\\54\\53\\49\\51\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\55\\55\\50\\55\\57\\54\\43\\49\\51\\55\\50\\57\\53\\57\\54\\32\\116\\104\\101\\110\\32\\113\\61\\68\\32\\113\\61\\66\\32\\97\\110\\100\\45\\51\\48\\52\\55\\50\\43\\49\\50\\49\\56\\56\\48\\51\\50\\32\\111\\114\\40\\45\\56\\53\\51\\49\\57\\56\\43\\49\\50\\57\\54\\54\\48\\51\\52\\41\\45\\52\\54\\50\\48\\54\\48\\32\\101\\108\\115\\101\\32\\110\\44\\107\\61\\74\\40\\85\\44\\110\\41\\113\\61\\110\\32\\97\\110\\100\\40\\49\\52\\51\\51\\53\\56\\54\\50\\45\\49\\54\\53\\49\\49\\41\\45\\40\\45\\54\\50\\53\\54\\57\\55\\43\\49\\52\\48\\56\\49\\57\\41\\111\\114\\32\\53\\53\\52\\51\\48\\50\\48\\45\\40\\45\\50\\57\\49\\51\\49\\54\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\45\\55\\53\\50\\52\\51\\52\\43\\55\\54\\48\\54\\54\\50\\56\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\45\\56\\53\\49\\56\\56\\56\\43\\49\\53\\54\\56\\49\\56\\52\\53\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\52\\50\\52\\57\\54\\49\\51\\45\\50\\54\\49\\53\\57\\53\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\52\\48\\51\\56\\43\\49\\51\\54\\52\\53\\49\\48\\49\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\40\\45\\52\\52\\50\\55\\52\\55\\43\\53\\53\\56\\52\\53\\57\\41\\43\\49\\50\\54\\56\\48\\52\\41\\43\\49\\50\\56\\57\\49\\51\\48\\49\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\45\\57\\53\\55\\51\\54\\51\\43\\40\\51\\53\\51\\51\\55\\54\\45\\49\\57\\50\\50\\48\\56\\41\\41\\43\\40\\45\\56\\53\\53\\55\\48\\54\\43\\49\\52\\55\\51\\54\\57\\49\\50\\41\\116\\104\\101\\110\\32\\116\\61\\123\\125\\85\\61\\110\\105\\108\\32\\73\\61\\98\\40\\45\\56\\48\\55\\53\\53\\45\\40\\45\\49\\48\\53\\50\\50\\49\\41\\41\\97\\61\\110\\105\\108\\32\\111\\61\\75\\40\\41\\100\\91\\111\\93\\61\\116\\32\\118\\61\\110\\105\\108\\32\\80\\61\\98\\40\\49\\48\\49\\53\\57\\49\\53\\43\\45\\57\\57\\49\\52\\48\\48\\41\\116\\61\\75\\40\\41\\107\\61\\110\\105\\108\\32\\70\\61\\69\\40\\40\\45\\50\\53\\49\\56\\56\\57\\43\\49\\52\\52\\55\\49\\49\\51\\54\\41\\45\\57\\54\\48\\54\\49\\57\\44\\123\\111\\59\\66\\44\\68\\59\\74\\125\\41\\115\\61\\110\\105\\108\\32\\109\\61\\75\\40\\41\\100\\91\\116\\93\\61\\70\\32\\106\\61\\110\\105\\108\\32\\70\\61\\123\\125\\100\\91\\109\\93\\61\\70\\32\\110\\61\\110\\105\\108\\32\\88\\61\\123\\125\\65\\61\\98\\40\\45\\49\\57\\52\\50\\57\\53\\45\\40\\45\\50\\49\\56\\56\\48\\57\\41\\41\\113\\61\\49\\51\\50\\48\\51\\52\\57\\56\\45\\50\\51\\50\\48\\53\\57\\32\\70\\61\\121\\91\\73\\93\\86\\61\\100\\91\\109\\93\\101\\61\\123\\91\\80\\93\\61\\86\\44\\91\\65\\93\\61\\97\\125\\73\\61\\70\\40\\88\\44\\101\\41\\100\\91\\84\\93\\61\\73\\32\\70\\61\\87\\40\\49\\51\\56\\54\\53\\57\\49\\55\\45\\55\\55\\48\\56\\49\\53\\44\\123\\109\\44\\111\\44\\99\\59\\66\\44\\68\\44\\116\\125\\41\\116\\61\\79\\40\\116\\41\\109\\61\\79\\40\\109\\41\\100\\91\\82\\93\\61\\70\\32\\66\\61\\79\\40\\66\\41\\74\\61\\79\\40\\74\\41\\66\\61\\98\\40\\45\\50\\56\\56\\50\\54\\51\\43\\51\\49\\50\\55\\53\\57\\41\\74\\61\\98\\40\\45\\50\\49\\51\\55\\52\\57\\45\\40\\45\\50\\51\\56\\50\\51\\50\\41\\41\\68\\61\\79\\40\\68\\41\\118\\61\\121\\91\\74\\93\\99\\61\\79\\40\\99\\41\\107\\61\\121\\91\\66\\93\\111\\61\\79\\40\\111\\41\\66\\61\\123\\118\\40\\107\\41\\125\\85\\61\\66\\91\\57\\53\\48\\52\\51\\57\\43\\45\\57\\53\\48\\52\\51\\55\\93\\74\\61\\66\\91\\45\\49\\48\\49\\53\\50\\48\\52\\45\\40\\45\\49\\48\\49\\53\\50\\48\\53\\41\\93\\110\\61\\66\\91\\45\\50\\55\\53\\57\\54\\56\\43\\50\\55\\53\\57\\55\\49\\93\\101\\108\\115\\101\\32\\72\\61\\108\\91\\55\\54\\48\\51\\52\\53\\43\\45\\55\\54\\48\\51\\52\\52\\93\\113\\61\\100\\91\\83\\91\\55\\54\\54\\49\\51\\51\\43\\45\\55\\54\\54\\49\\51\\50\\93\\93\\118\\61\\113\\32\\84\\61\\108\\91\\45\\56\\48\\55\\53\\52\\52\\43\\56\\48\\55\\53\\52\\54\\93\\113\\61\\118\\91\\84\\93\\113\\61\\113\\32\\97\\110\\100\\40\\40\\52\\48\\50\\51\\55\\55\\43\\40\\45\\49\\48\\50\\57\\54\\49\\49\\43\\56\\54\\54\\49\\56\\49\\49\\41\\41\\45\\40\\45\\53\\54\\57\\57\\53\\41\\41\\45\\40\\45\\57\\51\\51\\50\\55\\41\\111\\114\\40\\54\\57\\49\\51\\56\\55\\49\\45\\40\\45\\55\\56\\57\\52\\53\\52\\41\\41\\45\\40\\45\\56\\55\\50\\53\\51\\49\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\56\\48\\48\\56\\53\\54\\43\\49\\50\\52\\48\\48\\50\\49\\54\\32\\116\\104\\101\\110\\32\\113\\61\\81\\32\\97\\110\\100\\45\\53\\53\\54\\48\\48\\56\\43\\40\\54\\49\\48\\52\\56\\48\\43\\40\\40\\40\\49\\56\\53\\52\\56\\56\\49\\45\\56\\54\\50\\54\\53\\55\\41\\43\\45\\51\\57\\54\\57\\50\\41\\43\\40\\50\\48\\52\\57\\49\\49\\57\\45\\51\\50\\51\\56\\56\\48\\41\\41\\41\\111\\114\\32\\55\\54\\48\\50\\53\\57\\43\\49\\51\\56\\53\\48\\54\\48\\56\\32\\101\\108\\115\\101\\32\\72\\61\\100\\91\\83\\91\\45\\51\\55\\48\\50\\51\\50\\45\\40\\45\\51\\55\\48\\50\\51\\51\\41\\93\\93\\81\\61\\35\\72\\32\\72\\61\\52\\55\\48\\55\\57\\51\\45\\52\\55\\48\\55\\57\\51\\32\\113\\61\\81\\61\\61\\72\\32\\113\\61\\113\\32\\97\\110\\100\\45\\55\\51\\52\\55\\54\\55\\43\\40\\50\\54\\54\\51\\51\\49\\43\\49\\52\\53\\48\\48\\53\\49\\53\\41\\111\\114\\45\\51\\53\\51\\56\\53\\57\\43\\56\\57\\50\\52\\51\\57\\53\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\40\\52\\54\\51\\55\\53\\53\\43\\49\\52\\48\\51\\49\\48\\52\\55\\41\\45\\53\\49\\55\\48\\54\\55\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\45\\51\\50\\52\\50\\57\\43\\49\\51\\55\\54\\57\\48\\56\\52\\41\\45\\40\\40\\45\\49\\53\\49\\53\\48\\55\\43\\54\\57\\49\\48\\56\\50\\41\\45\\53\\54\\48\\56\\56\\48\\41\\116\\104\\101\\110\\32\\104\\61\\52\\49\\53\\53\\52\\54\\55\\56\\57\\57\\54\\48\\55\\45\\40\\45\\52\\54\\56\\49\\55\\49\\41\\88\\61\\98\\40\\45\\49\\51\\50\\50\\57\\45\\40\\57\\56\\56\\49\\57\\48\\45\\49\\48\\50\\53\\57\\48\\54\\41\\41\\111\\61\\100\\91\\118\\93\\109\\61\\121\\91\\88\\93\\97\\61\\50\\52\\54\\53\\55\\50\\53\\56\\49\\49\\56\\54\\57\\52\\45\\53\\57\\57\\51\\49\\54\\32\\101\\61\\100\\91\\84\\93\\80\\61\\100\\91\\82\\93\\65\\61\\98\\40\\49\\50\\49\\52\\49\\48\\45\\57\\54\\57\\49\\48\\41\\86\\61\\80\\40\\65\\44\\97\\41\\88\\61\\101\\91\\86\\93\\97\\61\\98\\40\\45\\55\\48\\55\\52\\57\\54\\43\\55\\51\\49\\57\\53\\52\\41\\70\\61\\109\\91\\88\\93\\80\\61\\100\\91\\84\\93\\86\\61\\100\\91\\82\\93\\65\\61\\86\\40\\97\\44\\104\\41\\101\\61\\80\\91\\65\\93\\65\\61\\45\\56\\49\\54\\52\\49\\52\\45\\40\\45\\56\\49\\54\\52\\49\\53\\41\\86\\61\\100\\91\\118\\93\\80\\61\\86\\91\\65\\93\\65\\61\\100\\91\\66\\93\\97\\61\\45\\56\\48\\48\\49\\52\\43\\56\\48\\48\\49\\55\\32\\86\\61\\65\\91\\97\\93\\88\\61\\123\\101\\44\\80\\59\\86\\125\\109\\61\\70\\40\\88\\41\\116\\61\\111\\91\\109\\93\\111\\61\\75\\40\\41\\65\\61\\40\\40\\40\\45\\49\\51\\48\\50\\48\\48\\48\\45\\40\\45\\50\\53\\50\\57\\56\\53\\45\\40\\45\\49\\48\\55\\56\\51\\51\\41\\41\\41\\45\\40\\57\\48\\57\\48\\54\\53\\43\\45\\49\\50\\51\\49\\56\\57\\50\\41\\41\\43\\49\\54\\52\\50\\52\\51\\57\\56\\52\\50\\51\\55\\54\\50\\41\\45\\57\\52\\52\\53\\49\\51\\32\\100\\91\\111\\93\\61\\116\\32\\86\\61\\98\\40\\40\\40\\49\\48\\49\\51\\48\\57\\43\\45\\49\\56\\48\\56\\57\\50\\41\\43\\54\\56\\52\\57\\53\\48\\41\\43\\45\\53\\56\\48\\56\\53\\48\\41\\70\\61\\100\\91\\106\\93\\97\\61\\98\\40\\40\\50\\57\\49\\52\\53\\55\\45\\53\\48\\51\\56\\49\\54\\41\\45\\40\\45\\50\\51\\54\\56\\53\\50\\41\\41\\88\\61\\100\\91\\84\\93\\101\\61\\100\\91\\82\\93\\80\\61\\101\\40\\86\\44\\65\\41\\109\\61\\88\\91\\80\\93\\116\\61\\70\\91\\109\\93\\104\\61\\40\\40\\40\\45\\53\\51\\57\\50\\49\\52\\45\\40\\45\\55\\54\\57\\48\\54\\52\\41\\41\\43\\45\\49\\48\\48\\49\\50\\53\\54\\41\\43\\55\\56\\57\\54\\48\\41\\43\\51\\52\\54\\56\\56\\50\\52\\54\\51\\48\\52\\54\\54\\56\\32\\113\\61\\52\\57\\49\\48\\50\\57\\54\\45\\53\\56\\55\\49\\49\\32\\70\\61\\75\\40\\41\\100\\91\\70\\93\\61\\116\\32\\116\\61\\100\\91\\106\\93\\109\\61\\100\\91\\111\\93\\80\\61\\100\\91\\84\\93\\86\\61\\100\\91\\82\\93\\65\\61\\86\\40\\97\\44\\104\\41\\101\\61\\80\\91\\65\\93\\86\\61\\100\\91\\66\\93\\65\\61\\56\\48\\55\\49\\54\\51\\43\\45\\56\\48\\55\\49\\54\\49\\32\\80\\61\\86\\91\\65\\93\\88\\61\\109\\40\\101\\44\\80\\41\\109\\61\\67\\40\\52\\51\\49\\55\\53\\49\\48\\45\\49\\48\\48\\48\\55\\51\\56\\44\\123\\107\\59\\111\\44\\84\\44\\82\\59\\118\\44\\66\\59\\106\\59\\70\\125\\41\\116\\91\\88\\93\\61\\109\\32\\116\\61\\100\\91\\106\\93\\104\\61\\51\\56\\56\\53\\48\\53\\51\\55\\56\\54\\55\\54\\53\\45\\40\\45\\52\\55\\49\\55\\55\\55\\41\\109\\61\\100\\91\\111\\93\\80\\61\\100\\91\\84\\93\\97\\61\\98\\40\\45\\49\\57\\56\\51\\52\\50\\43\\50\\50\\50\\55\\56\\56\\41\\86\\61\\100\\91\\82\\93\\65\\61\\86\\40\\97\\44\\104\\41\\70\\61\\79\\40\\70\\41\\101\\61\\80\\91\\65\\93\\97\\61\\57\\50\\52\\48\\54\\49\\45\\57\\50\\52\\48\\53\\55\\32\\104\\61\\98\\40\\49\\56\\51\\52\\49\\52\\45\\49\\53\\56\\57\\50\\51\\41\\80\\61\\100\\91\\66\\93\\65\\61\\40\\56\\53\\51\\49\\55\\52\\43\\45\\49\\56\\54\\50\\48\\50\\56\\41\\45\\40\\45\\49\\48\\48\\56\\56\\53\\53\\41\\104\\61\\80\\91\\104\\93\\111\\61\\79\\40\\111\\41\\66\\61\\79\\40\\66\\41\\86\\61\\123\\104\\40\\80\\44\\65\\44\\97\\41\\125\\88\\61\\109\\40\\101\\44\\89\\40\\86\\41\\41\\109\\61\\76\\40\\40\\45\\50\\50\\57\\53\\55\\52\\43\\49\\52\\51\\57\\52\\57\\56\\49\\41\\45\\40\\45\\56\\51\\55\\50\\53\\53\\41\\44\\123\\106\\59\\84\\59\\82\\125\\41\\116\\91\\88\\93\\61\\109\\32\\106\\61\\79\\40\\106\\41\\101\\108\\115\\101\\32\\90\\61\\100\\91\\84\\93\\114\\61\\90\\32\\113\\61\\90\\32\\97\\110\\100\\32\\49\\54\\48\\55\\57\\48\\51\\49\\45\\40\\45\\49\\49\\56\\54\\51\\48\\41\\111\\114\\32\\53\\51\\53\\49\\53\\53\\43\\56\\56\\49\\53\\52\\53\\54\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\118\\61\\100\\91\\83\\91\\45\\49\\52\\52\\55\\54\\51\\43\\49\\52\\52\\55\\54\\57\\93\\93\\84\\61\\118\\61\\61\\72\\32\\81\\61\\84\\32\\113\\61\\40\\53\\52\\48\\57\\48\\57\\43\\40\\45\\49\\50\\52\\52\\53\\48\\48\\45\\40\\45\\56\\57\\52\\53\\56\\41\\41\\41\\43\\49\\50\\48\\55\\50\\50\\52\\57\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\49\\52\\51\\57\\55\\49\\54\\50\\45\\40\\45\\50\\48\\55\\56\\50\\54\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\56\\52\\54\\56\\53\\55\\43\\49\\51\\54\\50\\54\\51\\52\\48\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\54\\54\\50\\57\\51\\52\\43\\45\\49\\53\\53\\52\\52\\50\\48\\41\\43\\49\\53\\48\\56\\53\\48\\50\\56\\32\\116\\104\\101\\110\\32\\118\\61\\45\\52\\49\\48\\57\\55\\52\\45\\40\\45\\52\\49\\48\\57\\55\\53\\41\\84\\61\\100\\91\\83\\91\\52\\56\\54\\53\\49\\52\\43\\45\\52\\56\\54\\53\\49\\50\\93\\93\\72\\61\\84\\42\\118\\32\\84\\61\\45\\56\\53\\53\\50\\48\\48\\43\\51\\49\\51\\56\\55\\54\\52\\54\\53\\54\\52\\51\\49\\32\\81\\61\\72\\43\\84\\32\\72\\61\\51\\53\\49\\56\\52\\51\\55\\51\\48\\53\\50\\49\\50\\48\\45\\57\\54\\51\\50\\56\\56\\32\\113\\61\\81\\37\\72\\32\\84\\61\\45\\51\\51\\51\\53\\50\\53\\45\\40\\45\\49\\51\\52\\53\\57\\55\\50\\45\\40\\45\\56\\50\\48\\57\\55\\50\\45\\49\\57\\49\\52\\55\\52\\41\\41\\100\\91\\83\\91\\53\\50\\53\\50\\55\\43\\45\\53\\50\\53\\50\\53\\93\\93\\61\\113\\32\\72\\61\\100\\91\\83\\91\\49\\48\\51\\48\\49\\52\\50\\45\\49\\48\\51\\48\\49\\51\\57\\93\\93\\81\\61\\72\\126\\61\\84\\32\\113\\61\\45\\55\\54\\56\\55\\56\\50\\43\\49\\51\\48\\54\\48\\48\\50\\49\\32\\101\\108\\115\\101\\32\\110\\61\\110\\105\\108\\32\\113\\61\\55\\50\\56\\51\\52\\57\\43\\52\\50\\54\\52\\55\\51\\51\\32\\85\\61\\110\\105\\108\\32\\82\\61\\110\\105\\108\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\109\\61\\98\\40\\51\\50\\55\\54\\50\\49\\45\\51\\48\\51\\49\\53\\49\\41\\71\\61\\98\\40\\54\\51\\56\\52\\54\\51\\45\\54\\49\\51\\57\\52\\55\\41\\113\\61\\121\\91\\71\\93\\70\\61\\121\\91\\109\\93\\71\\61\\113\\40\\70\\41\\113\\61\\98\\40\\45\\50\\50\\55\\53\\49\\53\\43\\50\\53\\49\\57\\56\\57\\41\\121\\91\\113\\93\\61\\71\\32\\113\\61\\40\\40\\40\\45\\55\\54\\53\\48\\51\\54\\45\\40\\45\\50\\56\\54\\57\\49\\51\\41\\41\\43\\40\\40\\45\\56\\51\\54\\50\\48\\49\\43\\45\\50\\49\\56\\53\\57\\55\\41\\45\\40\\45\\49\\53\\55\\54\\57\\52\\45\\40\\40\\57\\53\\53\\49\\50\\55\\45\\40\\53\\52\\51\\48\\55\\53\\43\\49\\55\\52\\57\\56\\57\\41\\41\\45\\40\\45\\57\\56\\50\\56\\48\\50\\45\\40\\45\\53\\50\\49\\52\\51\\53\\41\\41\\41\\41\\41\\41\\45\\40\\49\\48\\50\\49\\51\\52\\45\\49\\53\\52\\49\\53\\41\\41\\43\\40\\40\\40\\45\\53\\55\\53\\54\\56\\57\\45\\40\\45\\56\\57\\55\\48\\54\\55\\41\\41\\43\\45\\56\\50\\50\\52\\48\\54\\41\\43\\49\\55\\57\\48\\52\\52\\52\\48\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\54\\52\\48\\50\\49\\50\\43\\49\\51\\57\\57\\49\\57\\50\\57\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\51\\50\\51\\56\\56\\55\\43\\45\\52\\51\\52\\50\\51\\57\\41\\43\\40\\45\\51\\56\\49\\48\\55\\57\\43\\49\\53\\49\\48\\52\\54\\50\\53\\41\\116\\104\\101\\110\\32\\118\\61\\110\\105\\108\\32\\82\\61\\110\\105\\108\\32\\113\\61\\52\\53\\56\\56\\50\\51\\51\\45\\50\\53\\49\\55\\52\\55\\32\\101\\108\\115\\101\\32\\109\\61\\110\\111\\116\\32\\70\\32\\114\\61\\114\\43\\71\\32\\81\\61\\114\\60\\61\\90\\32\\81\\61\\109\\32\\97\\110\\100\\32\\81\\32\\109\\61\\114\\62\\61\\90\\32\\109\\61\\70\\32\\97\\110\\100\\32\\109\\32\\81\\61\\109\\32\\111\\114\\32\\81\\32\\109\\61\\49\\49\\52\\54\\49\\48\\57\\56\\45\\40\\45\\55\\54\\51\\56\\54\\52\\45\\40\\45\\54\\55\\52\\48\\53\\56\\41\\41\\113\\61\\81\\32\\97\\110\\100\\32\\109\\32\\81\\61\\49\\52\\48\\52\\54\\50\\43\\49\\51\\54\\57\\51\\54\\55\\50\\32\\113\\61\\113\\32\\111\\114\\32\\81\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\66\\61\\75\\40\\41\\118\\61\\110\\32\\100\\91\\66\\93\\61\\118\\32\\118\\61\\66\\32\\66\\61\\75\\40\\41\\68\\61\\113\\32\\100\\91\\66\\93\\61\\107\\32\\107\\61\\66\\32\\106\\61\\100\\91\\118\\93\\99\\61\\35\\106\\32\\106\\61\\45\\52\\50\\55\\49\\57\\51\\43\\52\\50\\55\\49\\57\\53\\32\\115\\61\\99\\61\\61\\106\\32\\113\\61\\115\\32\\97\\110\\100\\32\\49\\52\\56\\52\\48\\52\\43\\49\\48\\56\\54\\49\\50\\52\\51\\32\\111\\114\\40\\45\\49\\54\\49\\48\\48\\49\\45\\40\\45\\51\\52\\52\\50\\49\\55\\41\\41\\43\\49\\50\\55\\48\\55\\48\\56\\51\\32\\66\\61\\115\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\40\\57\\49\\49\\56\\52\\49\\43\\49\\52\\48\\49\\55\\57\\50\\54\\41\\45\\40\\45\\54\\54\\57\\53\\52\\57\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\45\\50\\50\\49\\54\\55\\54\\43\\49\\53\\51\\48\\53\\55\\56\\51\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\53\\56\\54\\52\\51\\51\\43\\49\\52\\52\\52\\49\\56\\56\\54\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\45\\49\\48\\52\\48\\48\\57\\55\\43\\49\\53\\50\\50\\50\\56\\51\\41\\43\\49\\52\\51\\54\\55\\54\\51\\53\\32\\116\\104\\101\\110\\32\\113\\61\\55\\49\\49\\51\\49\\49\\43\\56\\55\\54\\57\\50\\50\\49\\32\\70\\61\\35\\106\\32\\111\\61\\52\\51\\56\\51\\56\\55\\43\\45\\52\\51\\56\\51\\56\\54\\32\\88\\61\\40\\55\\51\\50\\54\\56\\55\\43\\50\\53\\56\\55\\51\\48\\41\\43\\45\\57\\57\\49\\52\\49\\54\\32\\116\\61\\118\\40\\111\\44\\70\\41\\111\\61\\85\\40\\106\\44\\116\\41\\70\\61\\100\\91\\99\\93\\73\\61\\111\\45\\88\\32\\116\\61\\110\\105\\108\\32\\109\\61\\110\\40\\73\\41\\70\\91\\111\\93\\61\\109\\32\\111\\61\\110\\105\\108\\32\\101\\108\\115\\101\\32\\84\\61\\108\\91\\56\\51\\54\\51\\56\\51\\43\\45\\56\\51\\54\\51\\56\\49\\93\\72\\61\\108\\91\\45\\54\\48\\55\\56\\51\\51\\43\\54\\48\\55\\56\\51\\52\\93\\110\\61\\98\\40\\40\\56\\51\\49\\52\\57\\54\\43\\45\\51\\55\\54\\54\\48\\41\\43\\45\\55\\54\\57\\51\\50\\51\\41\\81\\61\\100\\91\\83\\91\\40\\56\\49\\55\\50\\57\\49\\43\\45\\53\\49\\53\\50\\54\\50\\41\\45\\51\\48\\50\\48\\50\\56\\93\\93\\82\\61\\100\\91\\83\\91\\45\\55\\57\\53\\56\\52\\57\\43\\40\\45\\56\\54\\54\\53\\53\\56\\43\\40\\49\\49\\54\\55\\51\\48\\48\\45\\40\\45\\52\\57\\53\\49\\48\\57\\41\\41\\41\\93\\93\\66\\61\\40\\50\\48\\50\\51\\57\\53\\45\\49\\48\\49\\52\\50\\54\\41\\43\\54\\56\\50\\53\\49\\52\\48\\53\\52\\53\\56\\55\\52\\32\\107\\61\\45\\49\\53\\49\\57\\50\\51\\43\\50\\54\\50\\53\\51\\50\\51\\54\\54\\51\\52\\54\\52\\52\\32\\74\\61\\100\\91\\83\\91\\45\\52\\52\\52\\50\\57\\55\\45\\40\\40\\45\\49\\51\\55\\51\\50\\52\\45\\54\\55\\55\\54\\52\\50\\41\\43\\51\\55\\48\\54\\54\\54\\41\\93\\93\\85\\61\\74\\40\\110\\44\\107\\41\\106\\61\\40\\40\\49\\57\\51\\57\\48\\55\\50\\45\\40\\50\\54\\48\\49\\51\\55\\43\\52\\52\\52\\51\\55\\49\\41\\41\\45\\54\\53\\56\\51\\49\\57\\41\\43\\51\\51\\53\\56\\52\\52\\57\\50\\55\\52\\48\\57\\51\\57\\32\\107\\61\\98\\40\\56\\53\\53\\48\\51\\50\\43\\45\\56\\51\\48\\53\\52\\48\\41\\118\\61\\82\\91\\85\\93\\113\\61\\81\\91\\118\\93\\74\\61\\100\\91\\83\\91\\40\\40\\49\\48\\49\\51\\57\\55\\43\\45\\49\\48\\57\\54\\50\\53\\50\\41\\43\\53\\52\\52\\48\\48\\41\\45\\40\\45\\57\\52\\48\\52\\53\\55\\41\\93\\93\\85\\61\\100\\91\\83\\91\\45\\49\\55\\53\\56\\53\\55\\45\\40\\45\\49\\55\\53\\56\\54\\48\\41\\93\\93\\110\\61\\85\\40\\107\\44\\66\\41\\99\\61\\98\\40\\52\\57\\54\\51\\53\\52\\43\\40\\45\\52\\50\\56\\49\\52\\56\\43\\45\\52\\51\\54\\57\\53\\41\\41\\82\\61\\74\\91\\110\\93\\107\\61\\98\\40\\51\\52\\48\\49\\57\\49\\43\\45\\51\\49\\53\\54\\57\\52\\41\\110\\61\\121\\91\\107\\93\\66\\61\\100\\91\\83\\91\\45\\51\\54\\48\\50\\56\\52\\45\\40\\45\\55\\50\\51\\53\\53\\54\\45\\40\\45\\51\\54\\51\\50\\55\\48\\41\\41\\93\\93\\68\\61\\100\\91\\83\\91\\45\\49\\55\\55\\52\\57\\48\\45\\40\\45\\49\\55\\55\\52\\57\\51\\41\\93\\93\\115\\61\\68\\40\\99\\44\\106\\41\\107\\61\\66\\91\\115\\93\\66\\61\\98\\40\\45\\51\\56\\56\\52\\45\\40\\45\\49\\52\\57\\49\\57\\43\\45\\49\\51\\52\\49\\48\\41\\41\\85\\61\\110\\91\\107\\93\\106\\61\\45\\55\\56\\56\\54\\55\\56\\43\\49\\55\\57\\49\\49\\51\\52\\54\\48\\57\\52\\50\\50\\52\\32\\99\\61\\98\\40\\49\\52\\52\\56\\48\\57\\45\\49\\50\\48\\51\\52\\49\\41\\107\\61\\121\\91\\66\\93\\66\\61\\123\\107\\40\\72\\41\\125\\110\\61\\85\\40\\89\\40\\66\\41\\41\\66\\61\\100\\91\\83\\91\\45\\53\\56\\53\\54\\53\\56\\43\\53\\56\\53\\54\\54\\48\\93\\93\\68\\61\\100\\91\\83\\91\\45\\57\\54\\50\\56\\56\\43\\57\\54\\50\\57\\49\\93\\93\\115\\61\\68\\40\\99\\44\\106\\41\\107\\61\\66\\91\\115\\93\\68\\61\\98\\40\\45\\56\\56\\53\\53\\50\\51\\45\\40\\45\\57\\49\\48\\48\\51\\57\\41\\41\\66\\61\\121\\91\\68\\93\\68\\61\\66\\40\\84\\41\\85\\61\\107\\46\\46\\68\\32\\74\\61\\110\\46\\46\\85\\32\\118\\61\\82\\46\\46\\74\\32\\81\\61\\123\\113\\40\\118\\41\\125\\113\\61\\121\\91\\98\\40\\45\\56\\49\\53\\53\\55\\53\\43\\56\\52\\48\\48\\50\\50\\41\\93\\81\\61\\123\\89\\40\\81\\41\\125\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\100\\91\\83\\91\\52\\50\\51\\49\\48\\49\\43\\45\\52\\50\\51\\49\\48\\48\\93\\93\\84\\61\\118\\32\\107\\61\\49\\52\\53\\55\\53\\52\\45\\49\\52\\53\\55\\53\\52\\32\\66\\61\\45\\53\\50\\49\\57\\51\\52\\45\\40\\45\\53\\50\\50\\49\\56\\57\\41\\110\\61\\113\\40\\107\\44\\66\\41\\72\\91\\84\\93\\61\\110\\32\\84\\61\\110\\105\\108\\32\\113\\61\\40\\49\\48\\48\\53\\54\\48\\55\\43\\45\\53\\57\\49\\54\\48\\57\\41\\43\\40\\49\\55\\54\\56\\53\\53\\43\\51\\54\\51\\50\\48\\53\\53\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\49\\53\\57\\51\\57\\57\\48\\51\\45\\54\\49\\55\\53\\50\\51\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\40\\49\\52\\52\\48\\52\\56\\52\\55\\45\\55\\55\\57\\50\\50\\41\\45\\40\\45\\55\\54\\53\\57\\55\\50\\41\\116\\104\\101\\110\\32\\113\\61\\51\\53\\49\\53\\50\\56\\56\\45\\40\\55\\56\\55\\55\\50\\53\\43\\45\\49\\55\\49\\54\\57\\51\\57\\41\\106\\61\\79\\40\\106\\41\\66\\61\\79\\40\\66\\41\\101\\108\\115\\101\\32\\74\\61\\57\\52\\53\\49\\52\\56\\43\\45\\57\\52\\53\\49\\52\\54\\32\\84\\61\\100\\91\\83\\91\\40\\45\\57\\54\\52\\52\\57\\45\\55\\48\\51\\53\\56\\41\\43\\49\\54\\54\\56\\48\\56\\93\\93\\82\\61\\45\\52\\55\\53\\53\\51\\51\\43\\52\\55\\53\\53\\51\\52\\32\\118\\61\\84\\40\\82\\44\\74\\41\\84\\61\\40\\40\\45\\57\\51\\55\\53\\57\\57\\43\\40\\40\\45\\49\\52\\52\\48\\48\\54\\43\\45\\55\\51\\49\\49\\56\\50\\41\\43\\49\\48\\56\\50\\51\\55\\55\\41\\41\\43\\55\\54\\51\\57\\56\\51\\41\\43\\40\\51\\53\\50\\54\\54\\57\\43\\45\\51\\56\\54\\50\\52\\49\\41\\72\\61\\118\\61\\61\\84\\32\\113\\61\\72\\32\\97\\110\\100\\40\\52\\56\\54\\48\\56\\51\\56\\45\\57\\49\\50\\50\\54\\54\\41\\45\\40\\49\\48\\55\\51\\49\\53\\54\\45\\56\\49\\56\\48\\56\\41\\111\\114\\40\\56\\54\\49\\48\\54\\55\\43\\45\\54\\53\\51\\48\\57\\53\\41\\43\\53\\54\\54\\57\\50\\48\\56\\32\\81\\61\\72\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\66\\61\\75\\40\\41\\116\\61\\112\\40\\52\\54\\50\\53\\56\\53\\51\\45\\50\\54\\53\\51\\50\\57\\44\\123\\125\\41\\106\\61\\98\\40\\40\\40\\40\\45\\49\\48\\50\\51\\56\\49\\50\\43\\40\\49\\56\\52\\56\\53\\51\\57\\45\\40\\45\\50\\56\\51\\51\\49\\54\\43\\49\\51\\50\\52\\48\\48\\49\\41\\41\\41\\45\\52\\56\\49\\51\\50\\50\\41\\43\\50\\48\\56\\51\\48\\54\\41\\43\\53\\49\\51\\52\\55\\53\\41\\100\\91\\66\\93\\61\\81\\32\\115\\61\\49\\48\\57\\52\\54\\53\\45\\49\\48\\57\\52\\48\\48\\32\\68\\61\\40\\49\\49\\55\\53\\55\\55\\49\\45\\51\\52\\50\\51\\49\\54\\41\\43\\45\\56\\51\\51\\52\\53\\50\\32\\113\\61\\100\\91\\110\\93\\81\\61\\113\\40\\68\\44\\115\\41\\68\\61\\75\\40\\41\\100\\91\\68\\93\\61\\81\\32\\113\\61\\40\\53\\49\\57\\54\\52\\52\\43\\53\\54\\48\\55\\52\\41\\45\\53\\55\\53\\55\\49\\56\\32\\115\\61\\113\\32\\81\\61\\121\\91\\106\\93\\113\\61\\45\\55\\51\\48\\54\\51\\49\\45\\40\\45\\55\\51\\48\\54\\51\\49\\41\\99\\61\\113\\32\\106\\61\\123\\81\\40\\116\\41\\125\\81\\61\\40\\40\\40\\49\\57\\53\\50\\57\\50\\43\\45\\49\\48\\54\\55\\48\\51\\52\\41\\45\\40\\45\\50\\49\\50\\54\\56\\41\\41\\45\\40\\45\\49\\50\\55\\50\\52\\51\\41\\41\\43\\55\\50\\51\\50\\51\\51\\32\\71\\61\\98\\40\\45\\54\\52\\49\\51\\57\\53\\43\\54\\54\\53\\57\\49\\49\\41\\113\\61\\123\\89\\40\\106\\41\\125\\106\\61\\113\\32\\113\\61\\106\\91\\81\\93\\81\\61\\98\\40\\53\\53\\57\\52\\53\\55\\43\\45\\53\\51\\53\\48\\49\\50\\41\\116\\61\\113\\32\\113\\61\\121\\91\\81\\93\\111\\61\\100\\91\\118\\93\\90\\61\\121\\91\\71\\93\\71\\61\\90\\40\\116\\41\\90\\61\\98\\40\\50\\56\\53\\53\\52\\57\\43\\45\\50\\54\\49\\48\\57\\57\\41\\114\\61\\111\\40\\71\\44\\90\\41\\111\\61\\123\\114\\40\\41\\125\\81\\61\\113\\40\\89\\40\\111\\41\\41\\111\\61\\75\\40\\41\\100\\91\\111\\93\\61\\81\\32\\81\\61\\45\\50\\53\\48\\48\\48\\48\\43\\50\\53\\48\\48\\48\\49\\32\\114\\61\\100\\91\\68\\93\\90\\61\\114\\32\\114\\61\\45\\49\\48\\49\\53\\50\\48\\45\\40\\45\\49\\48\\49\\53\\50\\49\\41\\71\\61\\114\\32\\113\\61\\49\\53\\49\\52\\50\\55\\49\\52\\45\\40\\49\\48\\48\\56\\53\\51\\45\\40\\45\\52\\50\\52\\49\\51\\56\\41\\41\\114\\61\\54\\55\\49\\53\\53\\45\\40\\40\\45\\49\\48\\51\\57\\55\\52\\54\\43\\49\\49\\53\\49\\57\\54\\52\\41\\45\\52\\53\\48\\54\\51\\41\\70\\61\\71\\60\\114\\32\\114\\61\\81\\45\\71\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\49\\55\\48\\55\\55\\49\\48\\57\\45\\57\\54\\57\\55\\50\\48\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\53\\54\\48\\50\\49\\56\\54\\45\\40\\45\\57\\52\\55\\55\\56\\41\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\53\\50\\49\\50\\56\\55\\57\\45\\40\\45\\52\\49\\48\\50\\53\\57\\41\\116\\104\\101\\110\\32\\113\\61\\116\\114\\117\\101\\32\\113\\61\\45\\53\\55\\52\\50\\55\\57\\43\\49\\49\\55\\48\\57\\50\\48\\56\\32\\101\\108\\115\\101\\32\\113\\61\\116\\114\\117\\101\\32\\113\\61\\113\\32\\97\\110\\100\\45\\50\\49\\51\\56\\54\\57\\43\\49\\51\\48\\48\\53\\54\\52\\56\\32\\111\\114\\40\\40\\45\\56\\54\\53\\49\\52\\55\\45\\40\\45\\52\\50\\56\\56\\56\\54\\41\\41\\45\\40\\45\\56\\54\\48\\53\\50\\49\\41\\41\\43\\49\\48\\55\\49\\48\\54\\54\\57\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\100\\91\\84\\93\\61\\81\\32\\113\\61\\56\\54\\52\\48\\49\\50\\45\\40\\45\\52\\49\\48\\49\\57\\53\\41\\101\\110\\100\\32\\101\\108\\115\\101\\32\\105\\102\\32\\113\\60\\49\\55\\50\\48\\48\\50\\51\\50\\45\\54\\54\\54\\50\\52\\50\\32\\116\\104\\101\\110\\32\\105\\102\\32\\113\\60\\49\\54\\51\\57\\54\\49\\52\\49\\45\\40\\40\\40\\45\\53\\52\\49\\53\\49\\50\\45\\40\\45\\51\\57\\53\\49\\55\\41\\41\\43\\40\\53\\57\\49\\56\\50\\54\\45\\40\\45\\50\\53\\55\\57\\52\\56\\41\\41\\41\\45\\49\\52\\57\\51\\55\\55\\41\\116\\104\\101\\110\\32\\113\\61\\56\\55\\54\\48\\50\\52\\51\\45\\40\\45\\53\\57\\48\\51\\54\\56\\41\\90\\61\\115\\61\\61\\99\\32\\114\\61\\90\\32\\101\\108\\115\\101\\32\\70\\61\\98\\40\\49\\55\\49\\52\\55\\49\\43\\45\\49\\52\\54\\57\\57\\55\\41\\113\\61\\121\\91\\70\\93\\70\\61\\98\\40\\55\\57\\55\\48\\52\\49\\43\\45\\55\\55\\50\\53\\55\\49\\41\\121\\91\\70\\93\\61\\113\\32\\113\\61\\51\\51\\55\\56\\50\\51\\43\\49\\54\\51\\48\\50\\48\\55\\51\\32\\101\\110\\100\\32\\101\\108\\115\\101\\32\\113\\61\\52\\49\\56\\54\\48\\50\\43\\54\\54\\54\\51\\54\\50\\56\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\101\\110\\100\\32\\113\\61\\35\\122\\32\\114\\101\\116\\117\\114\\110\\32\\89\\40\\81\\41\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\98\\41\\108\\111\\99\\97\\108\\32\\89\\61\\118\\40\\98\\41\\108\\111\\99\\97\\108\\32\\108\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\108\\44\\83\\44\\122\\41\\114\\101\\116\\117\\114\\110\\32\\113\\40\\121\\44\\123\\108\\44\\83\\44\\122\\125\\44\\98\\44\\89\\41\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\108\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\41\\84\\61\\40\\50\\56\\49\\53\\52\\54\\43\\40\\45\\49\\51\\48\\57\\49\\50\\57\\45\\40\\45\\52\\50\\48\\56\\48\\50\\43\\45\\54\\48\\54\\55\\56\\50\\41\\41\\41\\43\\84\\32\\72\\91\\84\\93\\61\\45\\53\\51\\56\\56\\52\\49\\45\\40\\40\\45\\49\\49\\57\\51\\52\\56\\43\\45\\51\\49\\50\\54\\54\\54\\41\\45\\49\\48\\54\\56\\50\\56\\41\\114\\101\\116\\117\\114\\110\\32\\84\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\98\\41\\108\\111\\99\\97\\108\\32\\89\\61\\118\\40\\98\\41\\108\\111\\99\\97\\108\\32\\108\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\108\\44\\83\\44\\122\\44\\102\\41\\114\\101\\116\\117\\114\\110\\32\\113\\40\\121\\44\\123\\108\\59\\83\\44\\122\\59\\102\\125\\44\\98\\44\\89\\41\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\108\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\98\\41\\108\\111\\99\\97\\108\\32\\89\\61\\118\\40\\98\\41\\108\\111\\99\\97\\108\\32\\108\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\108\\41\\114\\101\\116\\117\\114\\110\\32\\113\\40\\121\\44\\123\\108\\125\\44\\98\\44\\89\\41\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\108\\32\\101\\110\\100\\44\\102\\117\\110\\99\\116\\105\\111\\110\\40\\121\\44\\98\\41\\108\\111\\99\\97\\108\\32\\89\\61\\118\\40\\98\\41\\108\\111\\99\\97\\108\\32\\108\\61\\102\\117\\110\\99\\116\\105\\111\\110\\40\\41\\114\\101\\116\\117\\114\\110\\32\\113\\40\\121\\44\\123\\125\\44\\98\\44\\89\\41\\101\\110\\100\\32\\114\\101\\116\\117\\114\\110\\32\\108\\32\\101\\110\\100\\44\\123\\125\\114\\101\\116\\117\\114\\110\\40\\74\\40\\40\\51\\54\\56\\53\\52\\49\\43\\40\\45\\55\\50\\53\\52\\51\\57\\43\\49\\50\\57\\56\\51\\48\\48\\48\\41\\41\\45\\55\\51\\57\\50\\49\\48\\44\\123\\125\\41\\41\\40\\89\\40\\81\\41\\41\\101\\110\\100\\41\\40\\103\\101\\116\\102\\101\\110\\118\\32\\97\\110\\100\\32\\103\\101\\116\\102\\101\\110\\118\\40\\41\\111\\114\\32\\95\\69\\78\\86\\44\\117\\110\\112\\97\\99\\107\\32\\111\\114\\32\\116\\97\\98\\108\\101\\91\\98\\40\\49\\48\\49\\56\\52\\49\\45\\40\\49\\57\\55\\53\\56\\56\\45\\49\\50\\48\\50\\49\\50\\41\\41\\93\\44\\110\\101\\119\\112\\114\\111\\120\\121\\44\\115\\101\\116\\109\\101\\116\\97\\116\\97\\98\\108\\101\\44\\103\\101\\116\\109\\101\\116\\97\\116\\97\\98\\108\\101\\44\\115\\101\\108\\101\\99\\116\\44\\123\\46\\46\\46\\125\\41\\101\\110\\100\\41\\40\\46\\46\\46\\41') do\r\n  jit.flush(trace)\r\nend".Replace("\r", ""), "patch.lua");
	}

	private static string ReplaceKeySafe(string k)
	{
		if (k == null)
		{
			return null;
		}
		char[] array = new char[k.Length];
		for (int i = 0; i < k.Length; i++)
		{
			array[i] = ((k[i] == '-') ? '-' : '•');
		}
		return new string(array);
	}

	public static void Initialize(string filename, string privateValue)
	{
		_filename = filename;
		Key = ((_filename != null && File.Exists(_filename)) ? CleanUpKey(File.ReadAllText(_filename)) : ValuesStorage.GetEncrypted<string>(KeyStorage, (string)null));
		bool encrypted = ValuesStorage.GetEncrypted<bool>(KeyValidFlag, false);
		IsAllRight = CheckKey(Key, privateValue);
		if (encrypted && !IsAllRight)
		{
			Revoked = Key;
		}
		ValuesStorage.SetEncrypted(KeyValidFlag, (object)SimpleSerialization.AsShort<string>((object)IsAllRight, (string)null));
		LimitedService.OptionNonLimited = IsAllRight;
	}

	public static void SetKey(string value, string privateValue)
	{
		value = CleanUpKey(value);
		if (_filename != null && File.Exists(_filename))
		{
			File.WriteAllText(_filename, value);
		}
		ValuesStorage.SetEncrypted(KeyStorage, (object)value);
		ValuesStorage.SetEncrypted(KeyValidFlag, (object)SimpleSerialization.AsShort<string>((object)CheckKey(value, privateValue), (string)null));
	}

	public static void Launch(ModernWindow mainWindow)
	{
		if (IsAllRight)
		{
			((DpiAwareWindow)mainWindow).Title = Regex.Replace(((DpiAwareWindow)mainWindow).Title, "\\s*\\(.+", "");
			foreach (LinkGroupFilterable item in ((IEnumerable)mainWindow.MenuLinkGroups).OfType<LinkGroupFilterable>())
			{
				item.IsEnabled = true;
			}
			foreach (Link item2 in ((IEnumerable<LinkGroup>)mainWindow.MenuLinkGroups).SelectMany((LinkGroup x) => (IEnumerable<Link>)x.Links))
			{
				item2.IsEnabled = true;
			}
			foreach (TitleLink item3 in (from x in ((IEnumerable)mainWindow.TitleLinks).OfType<TitleLink>()
				where x.IsAccented
				select x).ToList())
			{
				((Collection<Link>)(object)mainWindow.TitleLinks).Remove((Link)(object)item3);
			}
			{
				foreach (Link item4 in (Collection<Link>)(object)mainWindow.TitleLinks)
				{
					item4.IsEnabled = true;
				}
				return;
			}
		}
		foreach (LinkGroupFilterable item5 in from x in ((IEnumerable)mainWindow.MenuLinkGroups).OfType<LinkGroupFilterable>()
			where x.Source.OriginalString.EndsWith("/Pages/Drive/SpecialEvents.xaml") || x.Source.OriginalString.EndsWith("/Pages/Lists/TracksListPage.xaml") || x.Source.OriginalString.EndsWith("/Pages/Lists/GenericModsListPage.xaml")
			select x)
		{
			item5.IsEnabled = false;
		}
		foreach (Link item6 in from x in ((IEnumerable<LinkGroup>)mainWindow.MenuLinkGroups).SelectMany((LinkGroup x) => (IEnumerable<Link>)x.Links)
			where x.Key.EndsWith("/Pages/Drive/UserChampionships.xaml")
			select x)
		{
			item6.IsEnabled = false;
		}
	}

	private static string CleanUpKey(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return Regex.Replace(Regex.Replace(value, "[\\s\\W]+", "").ToUpperInvariant(), "(\\w{4})(?!$)", "$1-");
		}
		return string.Empty;
	}

	private static bool Manage(string s)
	{
		return new string[41]
		{
			"5758257503E28D628E6943DC7D214A5B1BC9652B", "88DC1A32AA3B1202B2C74E917815FDDA63E8FF78", "A995C4C6284E37E037EA89C8243A028080C93EFE", "6D053B87D96FA7A170ABD4003BBE042DAF04A7E1", "AE0050C25646AF00E97D20E66DCD35A0241F076D", "44261952E14AEB5E3E159E083D732CDA6C5DD049", "AA04FF4349362CB6A59A986535F63BC69AB1AB82", "BB010D6D0F1735D64FD6F4D327853154424471C8", "C2C89442C85746FB341D1F04A81405C553634818", "C7A7B4A58CDEDECB5E428ACFDB7A07DD872CCCD5",
			"732CCE056C150BA35BC1F30C0277A03B330ACC6B", "B73A65A53E3A5EED8CC4B1EB85C2527BA301CCC3", "DCA0F0C993F30D081E3955800076E07EA8C4B97A", "C79A89C467AEB265D71ACBB744E4A0A5C2B8E8A2", "5D2D20C218825906E3DE6C0B92912C238E33F12A", "BCCFF9904D756D8386AD4BA8ECC2906A0A98343C", "5CC8064B03B914EBEE695D71148F09B2E5842A61", "11F7208230C221A36B05DB639EAD9B2B58075F6B", "338A7F5F0A085B6DDD2336333A28A6374F3DB98C", "7C6DC7178EE0677D1710E53D9D6675FE97B5A55A",
			"E166C56B43AB7DAD573B31AB9436CCF1DBEACB2D", "0E4353313A57E2F35B45E5C32FC3BCBDC1E33314", "A4C753227F2D38B96D9A9069817D8B87A22F0013", "661B87E875726AC1DE4D025623626253D135A9F5", "6C6EB4064C4081D25843E686D8558FE5E76D2B2D", "EBBA7073E82165DBB326FE702FA68E445EFB354D", "F22CAD7BA69AB9CA3D5C53F76D736D920B5FAFC8", "8C235F0361E9F627D0232ECF4DBA088E5BB76DA0", "93232A092CBBAE98C2DAF0806A294C397C62734A", "4AC5975BF2BEF8004CFC5A718E3A12029B722255",
			"952B5AF8EC08D7AEF9AE01AD035DABB879B74BC5", "23CE1EEB24875D71C1D8A28627D9AC7661BCB8BA", "DC1D79582CEF278B2C76E1723736AB1235D91991", "5AEF3C4FF278930197F8957055E53F0DED08B6DC", "2BD023503193681963595391682BA59ABD8A3DDB", "30F18D9C2255483C2CFA2F4FDA246D35100D0B97", "3F5E3CDBCB2E7C17B9A2A11513BC2E4DED03375C", "8851E7AE78AA9574D93F6DA2BE44AE48214ED940", "AC210134F4468267C10DAE267BE8D6AA94FFE1EF", "B6DBDFCA8F4222137C9E1F8CDE219CA3CD4E2CA9",
			"9D98438EF116C220B34F5ABC406B0CEA1AB9D046"
		}.Contains(s);
	}
}
