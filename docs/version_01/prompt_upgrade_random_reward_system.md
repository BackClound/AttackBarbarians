# Upgrade 与 Random Reward System 模块需求提示词

## 目标
实现局内升级和随机奖励系统，每波结束或经验升级时提供三选一 Buff/技能/属性强化，并支持权重、稀有度、前置条件和堆叠。

## 当前基础
`SkillBuff核心机制.md` 和 `level_upgrade_system.md` 已描述三选一升级，但代码层尚未有完整 `UpgradeManager`、随机池、稀有度、过滤条件和 UI 数据流。

## 输出要求
- 生成 `UpgradeOptionSO`：ID、名称、描述、图标、稀有度、权重、类型、前置条件。
- 生成 `RewardPoolSO`：奖励池、权重、波次限制、互斥规则。
- 生成 `UpgradeManager`：生成候选、选择升级、应用效果、记录已选。
- 生成 `RandomRewardManager`：负责三选一、掉落、结算奖励和稀有度规则。
- 支持基础属性 Buff、技能升级、新技能解锁、武器强化、金币/钻石奖励。

## 实现流程
1. Wave/Exp 事件触发 `ShowUpgradeChoices`。
2. RandomReward 根据当前状态过滤不可用选项。
3. 按权重和稀有度抽取 3 个不重复选项。
4. 玩家选择后应用到 Buff/Skill/Weapon/Resource 系统。
5. 保存局内已选升级，避免异常退出丢失进度。

## 数据流
`WaveCompleted/LevelUp -> RandomRewardManager -> UpgradeManager -> UI -> Buff/Skill/Weapon/Save`

## 验收标准
- 三选一不会出现重复或不可用选项。
- 已选 Buff 可堆叠并立即影响 Player。
- 奖励池可通过配置调整，不需要修改代码。

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

