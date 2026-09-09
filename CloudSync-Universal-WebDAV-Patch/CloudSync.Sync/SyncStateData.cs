using System.Collections.Generic;

namespace CloudSync.Sync;

internal class SyncStateData
{
	public int Version { get; set; } = 1;

	public Dictionary<string, SaveSyncInfo> Saves { get; set; } = new Dictionary<string, SaveSyncInfo>();

	public Dictionary<string, ModConfigSyncInfo> ModConfigs { get; set; } = new Dictionary<string, ModConfigSyncInfo>();
}
