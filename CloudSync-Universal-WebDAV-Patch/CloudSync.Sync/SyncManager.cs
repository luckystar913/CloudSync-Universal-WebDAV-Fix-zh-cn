using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudSync.Utils;
using CloudSync.WebDav;
using StardewModdingAPI;

namespace CloudSync.Sync;

internal class SyncManager
{
	private readonly WebDavClient Client;

	private readonly SyncState State;

	private readonly ConflictResolver Conflicts;

	private readonly PathHelper Paths;

	private readonly ModConfig Config;

	private readonly IMonitor Monitor;

	private int _syncing;

	public SyncManager(WebDavClient client, SyncState state, ConflictResolver conflicts, PathHelper paths, ModConfig config, IMonitor monitor)
	{
		Client = client;
		State = state;
		Conflicts = conflicts;
		Paths = paths;
		Config = config;
		Monitor = monitor;
	}

	public async Task<SyncResult> UploadSave()
	{
		if (Interlocked.CompareExchange(ref _syncing, 1, 0) != 0)
		{
			return new SyncResult
			{
				Message = "Sync already in progress"
			};
		}
		try
		{
			return await UploadSaveInternal();
		}
		finally
		{
			Interlocked.Exchange(ref _syncing, 0);
		}
	}

	private async Task<SyncResult> UploadSaveInternal()
	{
		SyncResult result = new SyncResult();
		try
		{
			string saveName = Constants.SaveFolderName;
			if (string.IsNullOrEmpty(saveName))
			{
				result.Message = "No save loaded";
				return result;
			}
			string savePath = Paths.GetCurrentSavePath();
			string remoteSavePath = Config.RemotePath + "/saves/" + saveName;
			await Client.CreateDirectory(remoteSavePath);
			if (Config.SyncSaves)
			{
				List<string> saveFiles = Paths.GetSaveFiles(savePath);
				foreach (string item in saveFiles)
				{
					string fileName = Path.GetFileName(item);
					string remotePath = remoteSavePath + "/" + fileName;
					if (await Client.UploadFile(remotePath, item))
					{
						result.FilesUploaded++;
					}
				}
			}
			if (Config.SyncModConfigs)
			{
				Dictionary<string, string> modConfigs = Paths.GetModConfigs();
				foreach (KeyValuePair<string, string> item2 in modConfigs)
				{
					item2.Deconstruct(out var key, out var value);
					string modName = key;
					string localPath = value;
					string remotePath2 = Config.RemotePath + "/mod-configs/" + modName + "/config.json";
					if (await Client.UploadFile(remotePath2, localPath))
					{
						result.FilesUploaded++;
						State.UpdateModConfigSync(modName);
					}
				}
			}
			DateTime saveLastModified = Paths.GetSaveLastModified(savePath);
			State.UpdateSaveSync(saveName, saveLastModified, DateTime.UtcNow);
			result.Message = $"已上传 {result.FilesUploaded} 个文件";
				Monitor.Log("CloudSync: upload complete (" + result.Message + ")", (LogLevel)2);
		}
		catch (Exception ex)
		{
			result.Failed = true;
			result.Message = ex.Message;
			Monitor.Log("CloudSync: upload failed: " + ex.Message, (LogLevel)4);
		}
		return result;
	}

	public async Task<SyncResult> DownloadSaveIfNewer()
	{
		if (Interlocked.CompareExchange(ref _syncing, 1, 0) != 0)
		{
			return new SyncResult
			{
				Message = "Sync already in progress"
			};
		}
		try
		{
			return await DownloadSaveIfNewerInternal();
		}
		finally
		{
			Interlocked.Exchange(ref _syncing, 0);
		}
	}

	private async Task<SyncResult> DownloadSaveIfNewerInternal()
	{
		SyncResult result = new SyncResult();
		try
		{
			string saveName = Constants.SaveFolderName;
			if (string.IsNullOrEmpty(saveName))
			{
				result.Message = "No save loaded";
				return result;
			}
			string savePath = Paths.GetCurrentSavePath();
			string remoteSavePath = Config.RemotePath + "/saves/" + saveName;
			List<WebDavResource> list = await Client.PropFind(remoteSavePath);
			if (list == null || list.Count == 0)
			{
				result.Message = "No remote save found";
				Monitor.Log("CloudSync: no remote save for " + saveName, (LogLevel)1);
				return result;
			}
			DateTime remoteMod = DateTime.MinValue;
			foreach (WebDavResource item in list)
			{
				if (!item.IsCollection && item.LastModified > remoteMod)
				{
					remoteMod = item.LastModified;
				}
			}
			DateTime localMod = Paths.GetSaveLastModified(savePath);
			SaveSyncInfo saveInfo = State.GetSaveInfo(saveName);
			switch (Conflicts.Resolve(saveInfo.LastSync, localMod, remoteMod))
			{
			case SyncDirection.None:
				result.Message = "Already up to date";
				Monitor.Log("CloudSync: up to date", (LogLevel)1);
				break;
			case SyncDirection.Upload:
				result.Message = "Local is newer, will upload on save";
				Monitor.Log("CloudSync: local is newer", (LogLevel)1);
				break;
			case SyncDirection.Download:
				result = await DownloadSave(saveName, savePath, remoteSavePath);
				State.UpdateSaveSync(saveName, localMod, remoteMod);
				break;
			case SyncDirection.Conflict:
			{
				Monitor.Log("CloudSync: conflict detected, backing up local", (LogLevel)3);
				Conflicts.BackupLocal(savePath);
				result.HadConflict = true;
				SyncDirection syncDirection = Conflicts.ResolveConflict(localMod, remoteMod);
				if (syncDirection == SyncDirection.Download)
				{
					result = await DownloadSave(saveName, savePath, remoteSavePath);
					result.HadConflict = true;
				}
				State.UpdateSaveSync(saveName, localMod, remoteMod);
				break;
			}
			}
			if (Config.SyncModConfigs)
			{
				await SyncModConfigsDown();
			}
		}
		catch (Exception ex)
		{
			result.Failed = true;
			result.Message = ex.Message;
			Monitor.Log("CloudSync: download check failed: " + ex.Message, (LogLevel)4);
		}
		return result;
	}

	public async Task<SyncResult> ForceDownloadSave()
	{
		if (Interlocked.CompareExchange(ref _syncing, 1, 0) != 0)
		{
			return new SyncResult
			{
				Message = "Sync already in progress"
			};
		}
		try
		{
			return await ForceDownloadSaveInternal();
		}
		finally
		{
			Interlocked.Exchange(ref _syncing, 0);
		}
	}

	private async Task<SyncResult> ForceDownloadSaveInternal()
	{
		SyncResult result = new SyncResult();
		try
		{
			string saveName = Constants.SaveFolderName;
			if (string.IsNullOrEmpty(saveName))
			{
				result.Message = "No save loaded";
				return result;
			}
			string savePath = Paths.GetCurrentSavePath();
			string remoteSavePath = Config.RemotePath + "/saves/" + saveName;
			List<WebDavResource> remoteResources = await Client.PropFind(remoteSavePath);
			if (remoteResources == null || remoteResources.Count == 0)
			{
				result.Message = "No remote save found";
				return result;
			}
			Conflicts.BackupLocal(savePath);
			result = await DownloadSave(saveName, savePath, remoteSavePath);
			DateTime saveLastModified = Paths.GetSaveLastModified(savePath);
			DateTime dateTime = DateTime.MinValue;
			foreach (WebDavResource item in remoteResources)
			{
				if (!item.IsCollection && item.LastModified > dateTime)
				{
					dateTime = item.LastModified;
				}
			}
			State.UpdateSaveSync(saveName, saveLastModified, dateTime);
			Monitor.Log("CloudSync: force download complete (" + result.Message + ")", (LogLevel)2);
		}
		catch (Exception ex)
		{
			result.Failed = true;
			result.Message = ex.Message;
			Monitor.Log("CloudSync: force download failed: " + ex.Message, (LogLevel)4);
		}
		return result;
	}

	public async Task<SyncResult> FullSync()
	{
		if (Interlocked.CompareExchange(ref _syncing, 1, 0) != 0)
		{
			return new SyncResult
			{
				Message = "Sync already in progress"
			};
		}
		try
		{
			SyncResult syncResult = await DownloadSaveIfNewerInternal();
			if (syncResult.Failed)
			{
				return syncResult;
			}
			if (syncResult.FilesDownloaded > 0)
			{
				return syncResult;
			}
			return await UploadSaveInternal();
		}
		finally
		{
			Interlocked.Exchange(ref _syncing, 0);
		}
	}

	private async Task<SyncResult> DownloadSave(string saveName, string savePath, string remoteSavePath)
	{
		SyncResult result = new SyncResult();
		List<WebDavResource> list = await Client.PropFind(remoteSavePath);
		if (list == null)
		{
			result.Failed = true;
			result.Message = "列出云端文件失败";
			return result;
		}
		foreach (WebDavResource item in list)
		{
			if (item.IsCollection)
			{
				continue;
			}
			string text = Uri.UnescapeDataString(item.Href.TrimEnd('/').Split('/').Last());
			if (!string.IsNullOrEmpty(text))
			{
				string remotePath = remoteSavePath + "/" + text;
				string localPath = Path.Combine(savePath, text);
				if (await Client.DownloadFile(remotePath, localPath))
				{
					result.FilesDownloaded++;
				}
			}
		}
		result.Message = $"已下载 {result.FilesDownloaded} 个文件";
			Monitor.Log("CloudSync: download complete (" + result.Message + ")", (LogLevel)2);
		return result;
	}

	private async Task SyncModConfigsDown()
	{
		Dictionary<string, string> modConfigs = Paths.GetModConfigs();
		foreach (KeyValuePair<string, string> item in modConfigs)
		{
			item.Deconstruct(out var key, out var value);
			string modName = key;
			string localPath = value;
			string remotePath = Config.RemotePath + "/mod-configs/" + modName + "/config.json";
			DateTime? dateTime = await Client.GetLastModified(remotePath);
			if (dateTime.HasValue)
			{
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(localPath);
				ModConfigSyncInfo modConfigInfo = State.GetModConfigInfo(modName);
				SyncDirection syncDirection = Conflicts.Resolve(modConfigInfo.LastSync, lastWriteTimeUtc, dateTime.Value);
				if (syncDirection == SyncDirection.Download || syncDirection == SyncDirection.Conflict)
				{
					await Client.DownloadFile(remotePath, localPath);
					State.UpdateModConfigSync(modName);
				}
			}
		}
	}
}
