using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace AcManager.Internal;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class WebDownloadRule
{
	[JsonProperty("siteUrl")]
	public string SiteUrl { get; }

	[JsonProperty("rule")]
	public string Rule { get; }

	[JsonProperty("data")]
	public string[] Data { get; }

	public string Domain { get; }

	[JsonConstructor]
	public WebDownloadRule(string siteUrl, string rule, string[] data)
	{
		SiteUrl = siteUrl;
		Rule = rule;
		Data = data;
		Domain = GetDomainNameFromUrl(siteUrl);
	}

	private static string GetDomainNameFromUrl(string value)
	{
		return Regex.Replace(value, "^(?:(?:https?)?://)?(?:www\\.)?|(?<=\\w)/.*$", "", RegexOptions.IgnoreCase);
	}
}
