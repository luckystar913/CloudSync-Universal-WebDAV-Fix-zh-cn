# CloudSync-Universal-WebDAV-Fix-zh-cn
CloudSync 通用 WebDAV 适配修复版本 这是一个独立版本！不可和原版CloudSync WebDAV 共存。

# CloudSync 通用 WebDAV 适配补丁修复版

> 基于 [advx 的 CloudSync 模组](https://www.nexusmods.com/stardewvalley/mods/42850) 修改，**移除硬编码路径拼接，使模组能兼容更多 WebDAV 服务器**。

---

## 🔧 这个补丁做了什么

原版 CloudSync 代码里固定拼接了 `/remote.php/dav/files/` 路径，这是针对 Nextcloud 设计的。但很多 WebDAV 服务器（如坚果云、Koofr、自建 WebDAV 等）不使用这个路径结构，导致连接失败。顺便做了汉化。

**本补丁的修改：**
- 移除了原版代码中的固定路径拼接逻辑
- 让模组直接使用 `config.json` 里填写的完整 WebDAV 地址
- 完全汉化
- **不影响原版功能**，其他服务器仍可正常使用

---

## ✅ 兼容性

理论上支持所有提供标准 WebDAV 服务的服务器，包括但不限于：
- 坚果云
- Koofr
- 123云盘
- Nextcloud（原版路径依然可用）
- ownCloud
- 自建 WebDAV 服务器

---

## 📥 使用方法

1. 备份你的存档 (`StardewValley/Saves`)
2. 将本版本的模组包放入模组文件夹，或者下载`CloudSync.dll` 覆盖掉原版CloudSync模组文件夹下的dll文件。
3. 在 `config.json` 的 `NextcloudUrl` 中填入你的完整 WebDAV 地址（如 `https://dav.jianguoyun.com/dav/`）
4. 启动游戏，读档，睡一觉触发同步
5. 注意事项：这是 CloudSync WebDAV 版的补丁，不是原版CloudSync的，请不要混淆，以免造成存档损坏（虽然大概率smapi不会加载错误的模组，但是还是请小心。）

---

## 📂 文件结构

```

CloudSync-Universal-WebDAV-Fix/
├── CloudSync/
│   └── CloudSync.dll    # 修改后的核心文件
├── README.md
└── LICENSE

```

---


---

## 📜 原模组信息

| 项目 | 信息 |
| :--- | :--- |
| 模组名称 | Cloud Sync (WebDAV 版) |
| 原作者 | advx |
| Nexus Mods 链接 | https://www.nexusmods.com/stardewvalley/mods/42850 |

---

## 📄 许可证

本补丁基于 advx 的 CloudSync 模组修改，以 **MIT License** 发布。  
原模组版权归原作者 advx 所有。
