using System;
using System.IO;
using System.Text.Json;
using StardewModdingAPI;

namespace CloudSync.Sync;

internal class SyncState
{
	private readonly string FilePath;

	private readonly IMonitor Monitor;

	private SyncStateData Data;

	public SyncState(string modFolderPath, IMonitor monitor)
	{
		FilePath = Path.Combine(modFolderPath, "sync-state.json");
		Monitor = monitor;
		Data = Load();
	}

	private SyncStateData Load()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				string json = File.ReadAllText(FilePath);
				return JsonSerializer.Deserialize<SyncStateData>(json) ?? new SyncStateData();
			}
		}
		catch (Exception ex)
		{
			Monitor.Log("Failed to load sync state: " + ex.Message, (LogLevel)3);
		}
		return new SyncStateData();
	}

	public void Save()
	{
		try
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				WriteIndented = true
			};
			string contents = JsonSerializer.Serialize(Data, options);
			File.WriteAllText(FilePath, contents);
		}
		catch (Exception ex)
		{
			Monitor.Log("Failed to save sync state: " + ex.Message, (LogLevel)4);
		}
	}

	public SaveSyncInfo GetSaveInfo(string saveName)
	{
		if (!Data.Saves.ContainsKey(saveName))
		{
			Data.Saves[saveName] = new SaveSyncInfo();
		}
		return Data.Saves[saveName];
	}

	public void UpdateSaveSync(string saveName, DateTime localModified, DateTime remoteModified)
	{
		SaveSyncInfo saveInfo = GetSaveInfo(saveName);
		saveInfo.LastSync = DateTime.UtcNow;
		saveInfo.LastLocalModified = localModified;
		saveInfo.LastRemoteModified = remoteModified;
		Save();
	}

	public ModConfigSyncInfo GetModConfigInfo(string modId)
	{
		if (!Data.ModConfigs.ContainsKey(modId))
		{
			Data.ModConfigs[modId] = new ModConfigSyncInfo();
		}
		return Data.ModConfigs[modId];
	}

	public void UpdateModConfigSync(string modId)
	{
		ModConfigSyncInfo modConfigInfo = GetModConfigInfo(modId);
		modConfigInfo.LastSync = DateTime.UtcNow;
		Save();
	}
}
