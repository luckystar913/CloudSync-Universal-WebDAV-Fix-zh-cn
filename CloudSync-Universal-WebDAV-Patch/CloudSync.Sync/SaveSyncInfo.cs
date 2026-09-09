using System;

namespace CloudSync.Sync;

internal class SaveSyncInfo
{
	public DateTime LastSync { get; set; }

	public DateTime LastLocalModified { get; set; }

	public DateTime LastRemoteModified { get; set; }
}
