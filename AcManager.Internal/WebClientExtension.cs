using System.Net;
using System.Threading.Tasks;

namespace AcManager.Internal;

public static class WebClientExtension
{
	public static Task<byte[]> DownloadDataTaskAsyncSafe(this WebClient c, string url)
	{
		return c.DownloadDataTaskAsync(url);
	}
}
