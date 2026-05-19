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
