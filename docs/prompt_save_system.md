# Save System 模块需求提示词

## 目标
实现数据持久化系统，保存局外成长、货币、技能等级、设置、签到、统计，以及必要的局内临时进度。

## 当前基础
`数据持久化和优化.md` 已描述 JSON + PlayerPrefs/本地文件方案，但代码层尚未有完整 SaveManager 和 SaveData。

## 输出要求
- 生成 `SaveData`：版本号、玩家资源、永久成长、技能等级、设置、统计、局内进度。
- 生成 `SaveManager`：Load、Save、AutoSave、Delete、Export、Import。
- 生成 `SaveVersionMigrator`：处理版本升级。
- 生成 `SettingsData`：音量、画质、语言、输入偏好。
- 支持 Application.persistentDataPath JSON 文件存储。

## 实现流程
1. 定义最小默认存档。
2. 游戏启动时加载，没有则创建默认数据。
3. 波次完成、升级选择、购买、退出时自动保存。
4. 使用配置 ID 保存引用，不保存 Unity 对象。
5. 读写失败时保留备份并给出错误日志。

## 数据流
`GameBootstrapper -> SaveManager.Load -> RuntimeData -> GameEvents -> SaveManager.Save`

## 验收标准
- 删除存档后能重新创建默认数据。
- 已选永久升级和设置重启后仍保留。
- 存档结构支持版本迁移。
