using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CloudSync.WebDav;
using StardewModdingAPI;

namespace CloudSync.Sync;

internal class CloudSaveChecker
{
	private readonly WebDavClient Client;

	private readonly ModConfig Config;

	private readonly IMonitor Monitor;

	private HashSet<string> CloudSaves = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private HashSet<long> CloudFarmerIds = new HashSet<long>();

	private bool Loaded;

	private static readonly Regex MultiplayerIdRegex = new Regex("<UniqueMultiplayerID>(-?\\d+)</UniqueMultiplayerID>", RegexOptions.Compiled);

	public bool IsLoaded => Loaded;

	public CloudSaveChecker(WebDavClient client, ModConfig config, IMonitor monitor)
	{
		Client = client;
		Config = config;
		Monitor = monitor;
	}

	public bool HasCloudSave(string saveFolderName)
	{
		return CloudSaves.Contains(saveFolderName);
	}

	public bool HasCloudSaveForFarmer(long uniqueMultiplayerId)
	{
		return CloudFarmerIds.Contains(uniqueMultiplayerId);
	}

	public IEnumerable<string> GetAllSaves()
	{
		return CloudSaves;
	}

	public async Task Refresh()
	{
		try
		{
			string remotePath = Config.RemotePath + "/saves";
			List<WebDavResource> list = await Client.PropFind(remotePath);
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (list != null)
			{
				foreach (WebDavResource item in list)
				{
					if (item.IsCollection && !item.Href.TrimEnd('/').EndsWith("saves"))
					{
						string text = Uri.UnescapeDataString(item.Href.TrimEnd('/').Split('/').Last());
						if (!string.IsNullOrEmpty(text))
						{
							hashSet.Add(text);
						}
					}
				}
			}
			CloudSaves = hashSet;
			HashSet<long> hashSet2 = new HashSet<long>();
			string savesPath = Constants.SavesPath;
			if (Directory.Exists(savesPath))
			{
				string[] directories = Directory.GetDirectories(savesPath);
				foreach (string text2 in directories)
				{
					string fileName = Path.GetFileName(text2);
					if (!hashSet.Contains(fileName))
					{
						continue;
					}
					string path = Path.Combine(text2, "SaveGameInfo");
					if (!File.Exists(path))
					{
						continue;
					}
					try
					{
						string input = File.ReadAllText(path);
						Match match = MultiplayerIdRegex.Match(input);
						if (match.Success && long.TryParse(match.Groups[1].Value, out var result))
						{
							hashSet2.Add(result);
							Monitor.Log($"CloudSync: mapped cloud save '{fileName}' -> farmerId {result}", (LogLevel)0);
						}
					}
					catch (Exception ex)
					{
						Monitor.Log("CloudSync: failed to read SaveGameInfo for '" + fileName + "': " + ex.Message, (LogLevel)0);
					}
				}
			}
			CloudFarmerIds = hashSet2;
			Loaded = true;
			Monitor.Log($"CloudSync: found {hashSet.Count} saves in cloud, matched {hashSet2.Count} to local farmers", (LogLevel)1);
		}
		catch (Exception ex2)
		{
			Monitor.Log("CloudSync: failed to check cloud saves: " + ex2.Message, (LogLevel)3);
		}
	}

	public void Clear()
	{
		CloudSaves.Clear();
		CloudFarmerIds.Clear();
		Loaded = false;
	}
}
