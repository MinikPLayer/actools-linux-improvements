using System;

namespace AcManager.Internal;

public class ChangelogEntry
{
	public string Version { get; }

	public string Changes { get; }

	public DateTime Date { get; }

	public ChangelogEntry(string version, string changes, DateTime date)
	{
		Version = version;
		Changes = changes;
		Date = date;
	}
}
