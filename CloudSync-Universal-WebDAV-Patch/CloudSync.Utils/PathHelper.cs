using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StardewModdingAPI;

namespace CloudSync.Utils;

internal class PathHelper
{
	private readonly IModHelper Helper;

	public PathHelper(IModHelper helper)
	{
		Helper = helper;
	}

	public string GetCurrentSavePath()
	{
		return Path.Combine(Constants.SavesPath, Constants.SaveFolderName ?? "");
	}

	public List<string> GetSaveFiles(string saveFolderPath)
	{
		if (!Directory.Exists(saveFolderPath))
		{
			return new List<string>();
		}
		return Directory.GetFiles(saveFolderPath).Where(delegate(string f)
		{
			string fileName = Path.GetFileName(f);
			return !fileName.StartsWith(".") && !fileName.StartsWith("_cloudsync_");
		}).ToList();
	}

	public DateTime GetSaveLastModified(string saveFolderPath)
	{
		List<string> saveFiles = GetSaveFiles(saveFolderPath);
		if (saveFiles.Count == 0)
		{
			return DateTime.MinValue;
		}
		return saveFiles.Max((string f) => File.GetLastWriteTimeUtc(f));
	}

	public Dictionary<string, string> GetModConfigs()
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string path = Path.GetDirectoryName(Helper.DirectoryPath) ?? "";
		if (!Directory.Exists(path))
		{
			return dictionary;
		}
		string[] directories = Directory.GetDirectories(path);
		foreach (string text in directories)
		{
			string fileName = Path.GetFileName(text);
			if (!(fileName == "CloudSync"))
			{
				string text2 = Path.Combine(text, "config.json");
				if (File.Exists(text2))
				{
					dictionary[fileName] = text2;
				}
			}
		}
		return dictionary;
	}
}
