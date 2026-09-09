using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudSync.Integration;
using CloudSync.Patches;
using CloudSync.Sync;
using CloudSync.Utils;
using CloudSync.WebDav;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CloudSync;

internal class ModEntry : Mod
{
	private ModConfig Config;

	private WebDavClient? Client;

	private SyncManager? Sync;

	private CloudSaveChecker? CloudChecker;

	private bool Enabled;

	private string LastInitKey = "";

	private readonly ConcurrentQueue<string> HudQueue = new ConcurrentQueue<string>();

	public override void Entry(IModHelper helper)
	{
		Config = helper.ReadConfig<ModConfig>();
		helper.Events.GameLoop.GameLaunched += OnGameLaunched;
		helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
		helper.Events.GameLoop.Saved += OnSaved;
		helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
		helper.Events.Input.ButtonPressed += OnButtonPressed;
		helper.ConsoleCommands.Add("cloudsync", "云同步命令：status 状态 / sync 同步 / restore 恢复 / test 测试 / list 列表", (Action<string, string[]>)OnCommand);
		InitSyncClient();
	}

	private void InitSyncClient()
	{
		if (string.IsNullOrWhiteSpace(Config.NextcloudUrl) || string.IsNullOrWhiteSpace(Config.Username) || string.IsNullOrWhiteSpace(Config.Password))
		{
			if (Enabled || LastInitKey == "")
			{
				((Mod)this).Monitor.Log("CloudSync: configure NextcloudUrl, Username, and Password to enable sync. Use GMCM or edit config.json.", (LogLevel)3);
			}
			Enabled = false;
			Client = null;
			Sync = null;
			CloudChecker = null;
			LastInitKey = "";
			return;
		}
		string text = $"{Config.NextcloudUrl}|{Config.Username}|{Config.Password}|{Config.TimeoutSeconds}";
		if (!(text == LastInitKey) || !Enabled)
		{
			Client = new WebDavClient(Config.NextcloudUrl, Config.Username, Config.Password, Config.TimeoutSeconds, ((Mod)this).Monitor);
			SyncState state = new SyncState(((Mod)this).Helper.DirectoryPath, ((Mod)this).Monitor);
			ConflictResolver conflicts = new ConflictResolver(Config.MaxBackups, ((Mod)this).Monitor);
			PathHelper paths = new PathHelper(((Mod)this).Helper);
			Sync = new SyncManager(Client, state, conflicts, paths, Config, ((Mod)this).Monitor);
			CloudChecker = new CloudSaveChecker(Client, Config, ((Mod)this).Monitor);
			Enabled = true;
			LastInitKey = text;
			((Mod)this).Monitor.Log("CloudSync loaded. Sync enabled.", (LogLevel)2);
		}
	}

	private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		if (!e.IsMultipleOf(30u))
		{
			return;
		}
		string result;
		while (HudQueue.TryDequeue(out result))
		{
			try
			{
				Game1.addHUDMessage(new HUDMessage(result, 2));
			}
			catch
			{
			}
		}
	}

	private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		RegisterGmcm();
		if (Enabled && CloudChecker != null)
		{
			Harmony harmony = new Harmony(((Mod)this).ModManifest.UniqueID);
			SaveFileSlotPatch.Init(CloudChecker, ((Mod)this).Monitor);
			SaveFileSlotPatch.Apply(harmony);
			SaveFileSlotPatch.LoadSprite();
			Task.Run(async delegate
			{
				try
				{
					await CloudChecker.Refresh();
				}
				catch (Exception ex)
				{
					((Mod)this).Monitor.Log("CloudSync: cloud check failed: " + ex.Message, (LogLevel)3);
				}
			});
		}
		if (!Enabled)
		{
			return;
		}
		Task.Run(async delegate
		{
			try
			{
				if (await Client.TestConnection())
					{
						((Mod)this).Monitor.Log("云同步：已连接到云端", (LogLevel)2);
						ShowHud("云同步：已连接到云端");
					}
					else
					{
						((Mod)this).Monitor.Log("云同步：连接失败", (LogLevel)4);
						ShowHud("云同步：连接失败");
					}
			}
			catch (Exception value)
			{
				((Mod)this).Monitor.Log($"CloudSync error: {value}", (LogLevel)4);
			}
		});
	}

	private void RegisterGmcm()
	{
		IGenericModConfigMenuApi api = ((Mod)this).Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
		if (api == null)
		{
			((Mod)this).Monitor.Log("Generic Mod Config Menu not found, skipping in-game config.", (LogLevel)1);
			return;
		}
		api.RegisterModConfig(((Mod)this).ModManifest, delegate
		{
			Config = new ModConfig();
		}, delegate
		{
			((Mod)this).Helper.WriteConfig<ModConfig>(Config);
			InitSyncClient();
		});
		api.RegisterLabel(((Mod)this).ModManifest, "连接设置", "");
		api.RegisterSimpleOption(((Mod)this).ModManifest, "WebDAV 地址", "支持 WebDAV 的网盘基础地址，需以 / 结尾。各家不同，请到所用网盘的 WebDAV 说明里查", () => Config.NextcloudUrl, delegate(string val)
		{
			Config.NextcloudUrl = val;
		});
		api.RegisterSimpleOption(((Mod)this).ModManifest, "用户名", "所用网盘的登录账号（邮箱或用户名，视网盘而定）", () => Config.Username, delegate(string val)
		{
			Config.Username = val;
		});
		api.RegisterSimpleOption(((Mod)this).ModManifest, "密码 / 应用密码", "网盘的登录密码或应用专用密码。部分网盘要求用应用专用密码而非登录密码，请到所用网盘的安全设置里查", () => Config.Password, delegate(string val)
		{
			Config.Password = val;
		});
		api.RegisterSimpleOption(((Mod)this).ModManifest, "远程路径", "网盘上存放同步数据的目录（如 /StardewSync）", () => Config.RemotePath, delegate(string val)
		{
			Config.RemotePath = val;
		});
		api.RegisterClampedOption(((Mod)this).ModManifest, "超时（秒）", "WebDAV 请求的超时时间（秒）", () => Config.TimeoutSeconds, delegate(int val)
		{
			Config.TimeoutSeconds = val;
		}, 5, 120);
		api.RegisterLabel(((Mod)this).ModManifest, "同步范围", "");
		api.RegisterSimpleOption(((Mod)this).ModManifest, "同步存档", "上传/下载存档文件到网盘", () => Config.SyncSaves, delegate(bool val)
		{
			Config.SyncSaves = val;
		});
		api.RegisterSimpleOption(((Mod)this).ModManifest, "同步 Mod 配置", "同步其他已安装模组的 config.json 文件", () => Config.SyncModConfigs, delegate(bool val)
		{
			Config.SyncModConfigs = val;
		});
		api.RegisterSimpleOption(((Mod)this).ModManifest, "同步 Mod 数据", "同步模组存档数据（进度、解锁内容等）", () => Config.SyncModData, delegate(bool val)
		{
			Config.SyncModData = val;
		});
		api.RegisterLabel(((Mod)this).ModManifest, "行为", "");
		api.RegisterSimpleOption(((Mod)this).ModManifest, "保存时自动上传", "游戏保存后自动上传到网盘", () => Config.AutoSyncOnSave, delegate(bool val)
		{
			Config.AutoSyncOnSave = val;
		});
		api.RegisterSimpleOption(((Mod)this).ModManifest, "加载时自动下载", "加载存档时自动下载云端较新的版本", () => Config.AutoSyncOnLoad, delegate(bool val)
		{
			Config.AutoSyncOnLoad = val;
		});
		api.RegisterSimpleOption(((Mod)this).ModManifest, "始终以云端为准", "加载时强制用云端版本覆盖本地（云端为权威）", () => Config.AlwaysDownloadFromCloud, delegate(bool val)
		{
			Config.AlwaysDownloadFromCloud = val;
		});
		api.RegisterSimpleOption(((Mod)this).ModManifest, "手动同步按键", "按下此键触发完整双向同步", () => Config.ManualSyncKey, delegate(SButton val)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			Config.ManualSyncKey = val;
		});
		api.RegisterClampedOption(((Mod)this).ModManifest, "最大备份数", "每个存档保留的本地备份份数（冲突解决用）", () => Config.MaxBackups, delegate(int val)
		{
			Config.MaxBackups = val;
		}, 0, 10);
		api.RegisterSimpleOption(((Mod)this).ModManifest, "显示HUD提示", "在游戏中显示同步状态消息", () => Config.ShowHudNotifications, delegate(bool val)
		{
			Config.ShowHudNotifications = val;
		});
	}

	private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
	{
		if (!Enabled || !Config.AutoSyncOnLoad)
		{
			return;
		}
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				if (Config.AlwaysDownloadFromCloud)
					{
						SyncResult syncResult = await Sync.ForceDownloadSave();
						if (syncResult.Failed)
						{
							ShowHud("云同步：同步失败（见 SMAPI 日志）");
						}
						else if (syncResult.FilesDownloaded > 0)
						{
							ShowHud($"云同步：从云端恢复 {syncResult.FilesDownloaded} 个文件");
						}
						else
						{
							ShowHud("云同步：未找到云端存档");
						}
					}
					else
					{
						SyncResult syncResult = await Sync.DownloadSaveIfNewer();
						if (syncResult.Failed)
						{
							ShowHud("云同步：同步失败（见 SMAPI 日志）");
						}
						else if (syncResult.HadConflict)
						{
							ShowHud("云同步：冲突已解决，已创建本地备份");
						}
						else if (syncResult.FilesDownloaded > 0)
						{
							ShowHud("云同步：已下载存档（云端较新）");
						}
						else
						{
							ShowHud("云同步：已是最新");
						}
					}
			}
			catch (Exception value)
			{
				((Mod)this).Monitor.Log($"CloudSync error: {value}", (LogLevel)4);
			}
		});
	}

	private void OnSaved(object? sender, SavedEventArgs e)
	{
		if (!Enabled || !Config.AutoSyncOnSave)
		{
			return;
		}
		Task.Run(async delegate
		{
			try
			{
				if ((await Sync.UploadSave()).Failed)
					{
						ShowHud("云同步：同步失败（见 SMAPI 日志）");
					}
					else
					{
						ShowHud("云同步：存档已上传");
					}
			}
			catch (Exception value)
			{
				((Mod)this).Monitor.Log($"CloudSync error: {value}", (LogLevel)4);
			}
		});
	}

	private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		if (!Enabled || !Context.IsWorldReady || e.Button != Config.ManualSyncKey)
		{
			return;
		}
		Task.Run(async delegate
		{
			try
				{
					ShowHud("云同步：正在同步...");
					SyncResult syncResult = await Sync.FullSync();
					if (syncResult.Failed)
					{
						ShowHud("云同步：同步失败（见 SMAPI 日志）");
					}
					else if (syncResult.HadConflict)
					{
						ShowHud("云同步：冲突已解决，已创建本地备份");
					}
					else if (syncResult.FilesDownloaded > 0)
					{
						ShowHud($"云同步：已下载 {syncResult.FilesDownloaded} 个文件");
					}
					else if (syncResult.FilesUploaded > 0)
					{
						ShowHud($"云同步：已上传 {syncResult.FilesUploaded} 个文件");
					}
					else
					{
						ShowHud("云同步：已是最新");
					}
				}
			catch (Exception value)
			{
				((Mod)this).Monitor.Log($"CloudSync error: {value}", (LogLevel)4);
			}
		});
	}

	private void OnCommand(string command, string[] args)
	{
		if (!Enabled)
		{
			((Mod)this).Monitor.Log("云同步：尚未配置，请在 GMCM 或 config.json 中填写 WebDAV 地址、用户名、密码。", (LogLevel)3);
			return;
		}
		switch (args.FirstOrDefault()?.ToLower() ?? "status")
		{
		case "status":
		{
			string text = Constants.SaveFolderName ?? "（未加载存档）";
			((Mod)this).Monitor.Log("当前存档：" + text, (LogLevel)2);
			((Mod)this).Monitor.Log("WebDAV 地址：" + Config.NextcloudUrl, (LogLevel)2);
			((Mod)this).Monitor.Log($"保存时自动上传：{Config.AutoSyncOnSave}", (LogLevel)2);
			((Mod)this).Monitor.Log($"加载时自动下载：{Config.AutoSyncOnLoad}", (LogLevel)2);
			break;
		}
		case "sync":
			Task.Run(async delegate
			{
				try
				{
					SyncResult syncResult = await Sync.FullSync();
					((Mod)this).Monitor.Log("同步结果：" + syncResult.Message, (LogLevel)2);
				}
				catch (Exception value)
				{
					((Mod)this).Monitor.Log($"CloudSync error: {value}", (LogLevel)4);
				}
			});
			break;
		case "restore":
			Task.Run(async delegate
			{
				try
				{
					((Mod)this).Monitor.Log("云同步：正从云端强制恢复...", (LogLevel)2);
					SyncResult syncResult = await Sync.ForceDownloadSave();
					((Mod)this).Monitor.Log("恢复结果：" + syncResult.Message, (LogLevel)2);
				}
				catch (Exception value)
				{
					((Mod)this).Monitor.Log($"CloudSync error: {value}", (LogLevel)4);
				}
			});
			break;
		case "test":
			Task.Run(async delegate
			{
				try
				{
					bool flag = await Client.TestConnection();
					((Mod)this).Monitor.Log(flag ? "连接成功" : "连接失败", (LogLevel)(flag ? 2 : 4));
				}
				catch (Exception value)
				{
					((Mod)this).Monitor.Log($"CloudSync error: {value}", (LogLevel)4);
				}
			});
			break;
		case "list":
			Task.Run(async delegate
			{
				try
				{
					string remotePath = Config.RemotePath + "/saves";
					List<WebDavResource> list = await Client.PropFind(remotePath);
					if (list == null)
					{
						((Mod)this).Monitor.Log("云端无存档", (LogLevel)2);
						return;
					}
					foreach (WebDavResource item in list)
					{
						if (item.IsCollection && !item.Href.TrimEnd('/').EndsWith("saves"))
						{
							string value = Uri.UnescapeDataString(item.Href.TrimEnd('/').Split('/').Last());
							((Mod)this).Monitor.Log($"  {value}（修改时间：{item.LastModified:u}）", (LogLevel)2);
						}
					}
				}
				catch (Exception value2)
				{
					((Mod)this).Monitor.Log($"CloudSync error: {value2}", (LogLevel)4);
				}
			});
			break;
		default:
			((Mod)this).Monitor.Log("用法：cloudsync [status|sync|restore|test|list]", (LogLevel)2);
			break;
		}
	}

	private void ShowHud(string message)
	{
		if (Config.ShowHudNotifications)
		{
			HudQueue.Enqueue(message);
		}
	}
}
