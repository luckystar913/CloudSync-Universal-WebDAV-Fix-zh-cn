using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;

namespace CloudSync.Sync;

internal class ConflictResolver
{
	private readonly int MaxBackups;

	private readonly IMonitor Monitor;

	public ConflictResolver(int maxBackups, IMonitor monitor)
	{
		MaxBackups = maxBackups;
		Monitor = monitor;
	}

	public SyncDirection Resolve(DateTime lastSync, DateTime localModified, DateTime remoteModified)
	{
		if (lastSync == DateTime.MinValue)
		{
			if (remoteModified == DateTime.MinValue)
			{
				return SyncDirection.Upload;
			}
			if (localModified == DateTime.MinValue)
			{
				return SyncDirection.Download;
			}
			if (!(localModified >= remoteModified))
			{
				return SyncDirection.Download;
			}
			return SyncDirection.Upload;
		}
		bool flag = localModified > lastSync;
		bool flag2 = remoteModified > lastSync;
		if (flag && flag2)
		{
			return SyncDirection.Conflict;
		}
		if (flag)
		{
			return SyncDirection.Upload;
		}
		if (flag2)
		{
			return SyncDirection.Download;
		}
		return SyncDirection.None;
	}

	public SyncDirection ResolveConflict(DateTime localModified, DateTime remoteModified)
	{
		if (!(localModified >= remoteModified))
		{
			return SyncDirection.Download;
		}
		return SyncDirection.Upload;
	}

	public void BackupLocal(string saveFolderPath)
	{
		if (!Directory.Exists(saveFolderPath))
		{
			return;
		}
		string text = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string text2 = "_cloudsync_backup_" + text;
		string text3 = Path.Combine(saveFolderPath, text2);
		try
		{
			Directory.CreateDirectory(text3);
			string[] files = Directory.GetFiles(saveFolderPath);
			foreach (string text4 in files)
			{
				string destFileName = Path.Combine(text3, Path.GetFileName(text4));
				File.Copy(text4, destFileName, overwrite: true);
			}
			Monitor.Log("Backup created: " + text2, (LogLevel)2);
			CleanupOldBackups(saveFolderPath);
		}
		catch (Exception ex)
		{
			Monitor.Log("Failed to create backup: " + ex.Message, (LogLevel)4);
		}
	}

	private void CleanupOldBackups(string saveFolderPath)
	{
		List<string> list = (from d in Directory.GetDirectories(saveFolderPath)
			where Path.GetFileName(d).StartsWith("_cloudsync_backup_")
			orderby d descending
			select d).ToList();
		while (list.Count > MaxBackups)
		{
			string path = list.Last();
			try
			{
				Directory.Delete(path, recursive: true);
				Monitor.Log("Removed old backup: " + Path.GetFileName(path), (LogLevel)1);
			}
			catch (Exception ex)
			{
				Monitor.Log("Failed to remove backup: " + ex.Message, (LogLevel)3);
			}
			list.RemoveAt(list.Count - 1);
		}
	}

	private static void CopyDirectory(string source, string destination)
	{
		Directory.CreateDirectory(destination);
		string[] files = Directory.GetFiles(source);
		foreach (string text in files)
		{
			string destFileName = Path.Combine(destination, Path.GetFileName(text));
			File.Copy(text, destFileName, overwrite: true);
		}
		string[] directories = Directory.GetDirectories(source);
		foreach (string text2 in directories)
		{
			string destination2 = Path.Combine(destination, Path.GetFileName(text2));
			CopyDirectory(text2, destination2);
		}
	}
}
