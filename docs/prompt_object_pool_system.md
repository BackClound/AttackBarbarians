# Object Pool System 模块需求提示词

## 目标
实现统一对象池系统，管理敌人、子弹、技能特效、伤害数字、掉落物等高频对象，降低 GC 和实例化开销。

## 当前基础
`SkillObject_BulletSpawn` 已有子弹预创建思路，`DamageNumberController` 使用队列复用伤害数字，但敌人仍通过 `EnemyGenerateManager` 直接 `Instantiate`，回收链路不统一。

## 输出要求
- 生成 `IPoolable`：包含 `OnSpawn()`、`OnDespawn()`。
- 生成泛型 `ObjectPool<T>`。
- 生成 `PoolManager`：支持按 Key 注册、预热、获取、回收、清空。
- 生成 `PoolConfigSO` 和 `PoolEntry`，配置 prefab、初始数量、最大数量、是否可扩容。
- 改造 Bullet、Enemy、DamageNumber 的池化接入方案。

## 实现流程
1. 先实现独立对象池，不立即替换所有旧对象。
2. 优先接入子弹和伤害数字。
3. 再将 EnemySpawner 从 `Instantiate/Destroy` 迁移到 Pool。
4. 回收时统一重置 Transform、Collider、Rigidbody、状态机、Buff、血量和可见状态。

## 性能要求
- 高频对象禁止每次生成时 `new List`、`GetComponent` 或加载资源。
- 池对象预热在战斗开始前完成。
- 超出最大容量时要有明确策略：扩容、拒绝生成或回收最旧对象。

## 验收标准
- 子弹、敌人、伤害数字均从池中获取并回收。
- Profiler 中战斗高峰不出现大量 Instantiate/Destroy。
- `OnSpawn` 和 `OnDespawn` 生命周期可被各对象稳定调用。

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

