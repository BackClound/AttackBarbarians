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

## 验收标准补充
- 本模块完成后必须形成最小可运行闭环，可以在当前 Unity 场景中通过手动挂载或现有入口验证核心流程。
- 相关配置、运行时数据、事件发布和事件订阅必须有明确入口，异常或缺失配置时要给出可定位的问题表现。
- 高频逻辑不得引入明显 GC 分配；涉及生成、销毁、特效、投射物、敌人或 UI 飘字时必须优先接入对象池。
- 完成实现后必须说明受影响脚本、Prefab/Scene 挂载要求、测试步骤和未迁移的旧逻辑边界。

## 模块依赖边界
- 本模块只能依赖已完成或同阶段明确约定的公共接口、配置数据和事件 Key，不直接依赖后续阶段的具体实现类。
- 跨模块通信优先通过 `EventBus`、`IGameSystem`、`ServiceLocator`、ScriptableObject 配置或明确的 Controller API 完成。
- 禁止从低层模块反向引用高层模块；例如 Damage、Pool、Config 不应依赖 UI、Upgrade、Shop 等外围系统。
- 禁止在核心逻辑中散落 `FindObjectOfType`、硬编码场景路径或直接访问无关单例；必须通过启动流程、序列化引用或服务注册注入依赖。
- 数据结构与事件 Payload 必须保持小而稳定，避免为了单个功能暴露整个 Manager 或运行时对象内部状态。

## 旧逻辑兼容与迁移约束
- 保持当前可运行逻辑，不一次性删除或重写现有 `Player`、`Enemy`、`SkillShoot`、`EnemyGenerateManager` 等已在场景中使用的脚本。
- 新系统先以适配器、旁路入口或可切换开关接入；确认新流程可运行后，再逐步迁移旧职责。
- 每次迁移只替换一个清晰职责，例如目标选择、伤害结算、对象生成、UI 刷新或配置读取，避免跨系统大改。
- 迁移期间必须保持旧 Prefab、Animator、Collider、Layer、Tag 和 Inspector 序列化字段不失效。
- 删除旧代码前必须确认没有场景引用、Prefab 引用和运行时调用；无法确认时保留兼容层并标注后续清理任务。

