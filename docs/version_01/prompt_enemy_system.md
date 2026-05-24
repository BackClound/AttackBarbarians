# Enemy System 模块需求提示词

## 目标
完善敌人系统，支持普通怪、精英怪、特殊怪、Boss 前置能力、属性成长、受击反馈、死亡奖励和对象池回收。

## 当前基础
已有 `Enemy`、`BatEnemy`、敌人状态机、`Enemy_Health`、`EnemyCombatManager` 和 `EnemyGenerateManager`。当前敌人直接实例化，波次和数据配置尚不完整，受击上下文只传 float damage。

## 输出要求
- 生成 `EnemyDataSO`：ID、名称、Prefab、生命、攻击、移速、护甲、奖励、能力标签。
- 生成 `EnemyController` 或改造现有 `Enemy`：接收 RuntimeData，管理状态机。
- 生成 `EnemySpawnerManager`：由 WaveManager 驱动，从对象池获取敌人。
- 支持普通、冲锋、护盾、分裂、召唤、远程等能力接口。
- 死亡时发布 `OnEnemyDied`，携带奖励和来源。

## 实现流程
1. 将敌人生成从 `EnemyGenerateManager` 迁移到 Wave/Pool 驱动。
2. 敌人 OnSpawn 重置血量、Buff、状态、Collider、位置。
3. 受击统一走 `DamageSystem.ApplyDamage(DamageInfo)`。
4. 死亡不直接 Destroy，等待死亡动画后回收到池。
5. 敌人奖励通过事件交给 Upgrade/Resource 系统处理。

## 数据流
`WaveDataSO -> EnemySpawnerManager -> PoolManager -> EnemyController -> DamageSystem -> GameEvents.OnEnemyDied`

## 验收标准
- 不同波次可配置不同敌人池。
- 敌人随波次获得属性缩放。
- 敌人死亡、奖励、回收、UI 飘字均通过统一事件流触发。

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

