# Achievement 与 Daily Reward System 模块需求提示词

## 目标
实现成就与每日奖励系统，增强长期留存，并与资源、存档、UI 和事件系统解耦。

## 当前基础
`work_flow.md` 第五阶段包含 Achievement 和 Daily Reward，当前代码尚未实现。

## 输出要求
- 生成 `AchievementDataSO`：ID、名称、描述、目标类型、目标值、奖励。
- 生成 `AchievementManager`：监听事件、累计进度、领取奖励。
- 生成 `DailyRewardDataSO`：天数、奖励类型、数量、补签规则。
- 生成 `DailyRewardManager`：日期校验、签到、领取、保存。
- UI 通过事件展示红点、进度和可领取状态。

## 实现流程
1. 成就进度来自事件，如击杀数、波次数、Boss 击杀、累计金币。
2. 每日奖励使用本地日期，预留服务器时间接口。
3. 奖励发放统一走 RandomReward/ResourceManager。
4. 所有领取状态写入 SaveData。

## 数据流
`GameEvents -> AchievementManager/DailyRewardManager -> RewardManager -> ResourceManager -> SaveManager/UI`

## 验收标准
- 成就进度能随战斗事件增长。
- 每日奖励同一天不可重复领取。
- 奖励领取后资源和存档同步更新。

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

