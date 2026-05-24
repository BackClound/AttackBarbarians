# Wave System 模块需求提示词

## 目标
实现波次系统，控制敌人生成、波次计时、波次完成、难度成长、升级选择触发和自动进入下一波。

## 当前基础
`EnemyGenerateManager` 已实现按间隔随机生成敌人，但没有 WaveManager、WaveDataSO、敌人池、30 秒推进、击杀统计和波次事件闭环。

## 输出要求
- 生成 `WaveDataSO`：波次 ID、持续时间、敌人组合、生成间隔、最大数量、Boss 标记、奖励表。
- 生成 `WaveEnemyEntry`：敌人配置、数量、权重、生成时间段、属性倍率。
- 生成 `WaveManager`：StartWave、StopWave、SpawnNext、OnEnemyDied、CompleteWave。
- 生成 `SpawnAreaController`：根据相机和安全区计算生成范围。
- 波次完成后发布事件并触发随机升级选单。

## 实现流程
1. 保留当前生成位置算法，抽到 SpawnAreaController。
2. 用 WaveManager 替代 EnemyGenerateManager 的自动生成职责。
3. 使用对象池生成敌人。
4. 满足任一条件完成波次：敌人清空、达到 30 秒、Boss 被击败。
5. 完成时暂停生成，结算奖励，打开 Upgrade/Reward UI。

## 数据流
`WaveDataSO -> WaveManager -> EnemySpawnerManager -> PoolManager -> GameEvents.OnWaveCompleted`

## 验收标准
- 每波敌人数量、类型和间隔可配置。
- 达到 30 秒或清空敌人后能稳定进入下一波。
- 波次完成能触发升级、保存和 UI 刷新。

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

