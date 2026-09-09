# CloudSync-Universal-WebDAV-Fix-zh-cn

CloudSync 通用 WebDAV 适配修复版本。这是一个独立版本，不可和原版 CloudSync WebDAV 共存。

# CloudSync 通用 WebDAV 适配补丁修复版

> 基于 `https://www.nexusmods.com/stardewvalley/mods/42850` 修改，**移除硬编码路径拼接，使模组能兼容更多 WebDAV 服务器**。

---

## 🔧 这个补丁做了什么

原版 CloudSync 代码里固定拼接了 `/remote.php/dav/files/` 路径，这是针对 Nextcloud 设计的。但很多 WebDAV 服务器（如坚果云、Koofr、自建 WebDAV 等）不使用这个路径结构，导致连接失败。顺便做了汉化。

**本补丁的修改：**
- 移除了原版代码中的固定路径拼接逻辑
- 让模组直接使用 `config.json` 里填写的完整 WebDAV 地址
- 完全汉化（GMCM 配置界面、HUD 提示、控制台命令、存档悬停文字）
- 不影响原版功能，其他服务器仍可正常使用

---

## 兼容性

理论上支持所有提供标准 WebDAV 服务的服务器，包括但不限于：
- 坚果云
- Koofr
- 123云盘
- Nextcloud（原版路径依然可用）
- ownCloud
- 自建 WebDAV 服务器

---

## 使用方法

1. 备份你的存档（`StardewValley/Saves`）
2. 将本版本的模组包放入模组文件夹，或者下载 `CloudSync.dll` 覆盖掉原版 CloudSync 模组文件夹下的 dll 文件
3. 在 `config.json` 的 `NextcloudUrl` 中填入你的完整 WebDAV 地址（如 `https://dav.jianguoyun.com/dav/`）
4. 启动游戏，读档，睡一觉触发同步
5. 注意事项：这是 CloudSync WebDAV 版的补丁，不是原版 CloudSync 的，请不要混淆，以免造成存档损坏（虽然大概率 SMAPI 不会加载错误的模组，但是还是请小心）

---

## 文件结构

发布包顶层目录为 `CloudSync-Universal-WebDAV-Fix/`，分为两部分：可直接使用的模组本体 + 源码工程。

```
CloudSync-Universal-WebDAV-Fix/
│
├── README.md                              本说明文件
├── LICENSE                                MIT 开源许可证
│
├── CloudSync/                             【模组本体】复制到游戏 Mods\ 文件夹即可使用
│   ├── CloudSync.dll                      编译好的模组 DLL
│   ├── manifest.json                      SMAPI 模组清单（SMAPI 加载模组用）
│   └── config.json                        配置文件（运行一次游戏后自动生成，也可手动创建）
│
└── CloudSync-Universal-WebDAV-Patch/      【源码工程】用于自行编译和修改
    ├── .gitignore                         Git 忽略规则
    ├── CloudSync.csproj                   项目工程文件（定义依赖和编译配置）
    │
    ├── CloudSync/                          模组主逻辑
    │   ├── ModEntry.cs                    模组入口：事件注册、GMCM 配置界面、HUD 提示、控制台命令
    │   └── ModConfig.cs                   配置类：定义所有可配置选项的字段结构
    │
    ├── CloudSync.Patches/                 Harmony 补丁
    │   └── SaveFileSlotPatch.cs           存档选择界面的补丁：在已同步的存档旁显示云图标
    │
    ├── CloudSync.Sync/                    同步核心逻辑
    │   ├── SyncManager.cs                 同步管理器：上传/下载/全量同步/冲突处理
    │   ├── CloudSaveChecker.cs            云端存档检查器：列出云端存档、匹配本地农夫 ID
    │   ├── ConflictResolver.cs            冲突解决器：比对时间戳决定上传/下载/冲突备份
    │   ├── SyncState.cs                   同步状态持久化：读写 sync-state.json
    │   ├── SyncStateData.cs               同步状态数据结构
    │   ├── SaveSyncInfo.cs                单个存档的同步信息
    │   ├── ModConfigSyncInfo.cs           单个模组配置的同步信息
    │   ├── SyncDirection.cs               同步方向枚举（None/Upload/Download/Conflict）
    │   └── SyncResult.cs                  同步操作结果
    │
    ├── CloudSync.WebDav/                  WebDAV 客户端
    │   ├── WebDavClient.cs                WebDAV 协议实现：上传/下载/列目录/建目录/删除/PROPFIND
    │   └── WebDavResource.cs             WebDAV 资源数据结构
    │
    ├── CloudSync.Utils/                   工具类
    │   └── PathHelper.cs                  路径辅助：获取存档路径、存档文件列表、模组配置文件
    │
    ├── CloudSync.Integration/             第三方集成接口
    │   └── IGenericModConfigMenuApi.cs    GMCM（Generic Mod Config Menu）API 接口定义
    │
    ├── Properties/
    │   └── AssemblyInfo.cs                程序集信息（版本号等）
    │
    ├── scripts/                           编译脚本
    │   ├── build.bat                      Windows 批处理编译脚本
    │   ├── build.ps1                      Windows PowerShell 编译脚本
    │   ├── build.sh                       Linux/macOS bash 编译脚本
    │   └── Makefile                       GNU Make 编译脚本
    │
    ├── bin/                               编译输出（自动生成，已 gitignore）
    ├── obj/                               编译中间文件（自动生成，已 gitignore）
    │
    └── （编译前需手动复制的依赖 DLL，见下方"依赖文件获取"）
        ├── StardewModdingAPI.dll
        ├── 0Harmony.dll
        ├── MonoGame.Framework.dll
        ├── Stardew Valley.dll
        └── SMAPI.Toolkit.CoreInterfaces.dll
```

---

## 编译环境

### 本机环境（已验证通过）

| 项目 | 版本 |
|------|------|
| 操作系统 | Windows 10/11 |
| .NET SDK | 6.0.x（必须 6.0，不能用 7/8/9） |
| 星露谷物语 | 1.6.x |
| SMAPI | 4.0.0+ |

### 推荐环境

- **.NET SDK 6.0**：模组目标框架是 `net6.0`，SMAPI 4.x 也基于 .NET 6。装 7/8/9 可以编译但运行时不保证兼容。
- **Visual Studio 2022** 或 **VS Code** + C# 扩展：可选，命令行 `dotnet build` 足够。
- 下载地址：https://dotnet.microsoft.com/download/dotnet/6.0

---

## 依赖文件获取

编译需要 5 个 DLL，它们来自你已安装的星露谷物语 + SMAPI 游戏目录。

### 第 1 步：找到你的游戏目录

Steam 默认路径（根据你实际安装位置）：

```
C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley
```

如果你把游戏装在别的盘，到那个目录找。

### 第 2 步：复制 5 个 DLL 到源码工程根目录

从游戏目录复制以下文件到 `CloudSync-Universal-WebDAV-Patch/` 目录下（和 `CloudSync.csproj` 放在一起）：

| DLL 文件 | 游戏目录中的位置 |
|---------|----------------|
| `StardewModdingAPI.dll` | 游戏根目录 |
| `Stardew Valley.dll` | 游戏根目录 |
| `MonoGame.Framework.dll` | 游戏根目录 |
| `0Harmony.dll` | 游戏根目录下的 `smapi-internal\` 文件夹 |
| `SMAPI.Toolkit.CoreInterfaces.dll` | 游戏根目录下的 `smapi-internal\` 文件夹 |

### Windows 复制命令示例

如果你的游戏在 `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley`，在源码工程根目录下打开 PowerShell：

```powershell
$game = "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley"
Copy-Item "$game\StardewModdingAPI.dll" . -Force
Copy-Item "$game\Stardew Valley.dll" . -Force
Copy-Item "$game\MonoGame.Framework.dll" . -Force
Copy-Item "$game\smapi-internal\0Harmony.dll" . -Force
Copy-Item "$game\smapi-internal\SMAPI.Toolkit.CoreInterfaces.dll" . -Force
```

复制后工程根目录应该有这 5 个 DLL 文件。

> **注意**：这些 DLL 仅用于编译时引用，不会被打包进模组（csproj 里设了 `Private=False`）。运行时由 SMAPI 从游戏目录加载。

---

## 编译方法

### 方法一：使用编译脚本（推荐）

**Windows（批处理）：**
```cmd
scripts\build.bat
```

**Windows（PowerShell）：**
```powershell
.\scripts\build.ps1
```

**Linux / macOS：**
```bash
chmod +x scripts/build.sh
./scripts/build.sh
```

**GNU Make：**
```bash
make -f scripts/Makefile release
```

脚本会自动检查 5 个依赖 DLL 是否存在，缺失会报错提示。

### 方法二：直接用 dotnet 命令

```bash
dotnet build CloudSync.csproj -c Release
```

### 编译输出

编译成功后，DLL 在：
```
bin\Release\net6.0\CloudSync.dll
```

---

## 部署到游戏

编译完成后，把以下文件复制到游戏的 `Mods\CloudSync\` 文件夹：

```
Mods\CloudSync\
├── CloudSync.dll          ← 从 bin\Release\net6.0\ 复制
├── manifest.json          ← 从工程根目录复制
└── config.json            ← 运行一次游戏后自动生成，或手动创建（见下方）
```

### config.json 示例

> JSON 格式不支持注释，下面 `#` 后的内容仅为说明，实际使用时请删掉 `#` 及后面的文字。

```json
{
  "NextcloudUrl": "https://dav.jianguoyun.com/dav/",  # WebDAV 服务器地址，需以 / 结尾。坚果云填这个；其他网盘请到对应网盘的 WebDAV 说明里查
  "Username": "your-email@example.com",               # 网盘登录账号，一般为注册邮箱，视网盘而定
  "Password": "your-app-password",                    # 应用专用密码，不是登录密码。坚果云在 网页端 > 安全选项 > 第三方应用管理 里生成
  "RemotePath": "/StardewSync",                       # 网盘上存放同步数据的目录名，可自定义，首次使用会自动创建
  "SyncSaves": true,                                  # 是否同步存档文件（主存档 + SaveGameInfo），核心功能，建议 true
  "SyncModConfigs": false,                            # 是否同步其他模组的 config.json，装了大量模组时建议 false 以节省流量
  "SyncModData": false,                               # 是否同步模组存档数据（进度、解锁等），同上，按需开启
  "AutoSyncOnSave": true,                             # 游戏保存后（睡觉过夜）是否自动上传到网盘
  "AutoSyncOnLoad": true,                             # 加载存档时是否自动检查并下载云端较新版本
  "AlwaysDownloadFromCloud": false,                   # 加载时是否强制用云端版本覆盖本地（云端为权威），true 时忽略本地修改
  "ManualSyncKey": "None",                            # 手动触发完整双向同步的按键，填 None 表示禁用。可用值如 H、F5 等
  "MaxBackups": 3,                                    # 冲突时每个存档保留的本地备份数量，0~10，备份存在存档文件夹的 _cloudsync_backup_ 子目录
  "TimeoutSeconds": 60,                               # WebDAV 请求超时时间（秒），网络慢可调大，范围 5~120
  "ShowHudNotifications": true                        # 是否在游戏内显示同步状态提示（如 已上传、已是最新 等）
}
```

> 如果安装了 GMCM（Generic Mod Config Menu），也可以在游戏内打开配置界面，所有选项都有中文说明。

---

## 配置说明

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| NextcloudUrl | string | "" | 支持 WebDAV 的网盘基础地址，需以 `/` 结尾 |
| Username | string | "" | 所用网盘的登录账号 |
| Password | string | "" | 登录密码或应用专用密码 |
| RemotePath | string | "/StardewSync" | 网盘上存放同步数据的目录 |
| SyncSaves | bool | true | 同步存档文件 |
| SyncModConfigs | bool | true | 同步其他模组的 config.json |
| SyncModData | bool | true | 同步模组存档数据 |
| AutoSyncOnSave | bool | true | 保存后自动上传 |
| AutoSyncOnLoad | bool | true | 加载时自动下载较新版本 |
| AlwaysDownloadFromCloud | bool | false | 强制用云端覆盖本地 |
| ManualSyncKey | SButton | None | 手动触发同步的按键 |
| MaxBackups | int | 3 | 每个存档保留的本地备份数 |
| TimeoutSeconds | int | 30 | WebDAV 请求超时（秒） |
| ShowHudNotifications | bool | true | 显示游戏内 HUD 提示 |

---

## 控制台命令

在 SMAPI 控制台输入（按 `~` 键打开）：

| 命令 | 说明 |
|------|------|
| `cloudsync status` | 查看当前同步状态 |
| `cloudsync sync` | 执行完整双向同步 |
| `cloudsync restore` | 从云端强制恢复存档 |
| `cloudsync test` | 测试 WebDAV 连接 |
| `cloudsync list` | 列出云端存档 |

---

## 常见问题

### Q: 编译报错 "找不到 StardewModdingAPI.dll"
A: 你没有把 5 个依赖 DLL 复制到工程根目录。参见上方"依赖文件获取"。

### Q: 运行时报 PROPFIND 方法不可用
A: 这是运行环境问题（如 Android Cinderbox 兼容层拦截了 WebDAV 方法）。在正常 PC 环境 + SMAPI 下不会出现。

### Q: 坚果云连接失败
A: 检查是否使用了"应用密码"而非登录密码。坚果云需在网页端"安全选项"中生成应用专用密码。

### Q: 编译有 CS8632 警告
A: 这是可空注解警告，不影响编译和运行。如需消除，可在 csproj 的 PropertyGroup 中加 `<Nullable>enable</Nullable>`。

### Q: 可以和原版 CloudSync 共存吗
A: 不可以。这是一个独立版本，UniqueID 和原版相同，SMAPI 会冲突。请删除原版模组文件夹后再使用本版本。

---

## 技术说明

- 日志（SMAPI 控制台的技术性日志）保留英文，方便对照源码排错
- 玩家可见的界面（GMCM 配置、HUD 提示、命令输出、存档悬停文字）已汉化
- WebDAV 客户端为通用实现，不绑定特定网盘服务商，兼容坚果云、InfiniCLOUD、Nextcloud 等

---

## 📜 原模组信息

| 项目 | 信息 |
| :--- | :--- |
| 模组名称 | Cloud Sync (WebDAV 版) |
| 原作者 | advx |
| Nexus Mods 链接 | `https://www.nexusmods.com/stardewvalley/mods/42850` |

---

## 许可证

本补丁基于 advx 的 CloudSync 模组修改，以 **MIT License** 发布。  
原模组版权归原作者 advx 所有。见 [LICENSE](LICENSE)。
