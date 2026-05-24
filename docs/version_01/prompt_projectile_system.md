# Projectile System 模块需求提示词

## 目标
建立投射物系统，支持子弹、闪电、落雷、火球、冰锥、水波等技能载体，并统一对象池、命中检测和伤害提交。

## 当前基础
`SkillObject_BulletSpawn` 与 `SKillObject_Bullet` 已实现基础子弹发射和碰撞伤害，但命名不统一，生命周期和池化接口未标准化，穿透/弹射/范围等能力未抽象。

## 输出要求
- 生成 `ProjectileController`：移动、命中、生命周期、回收。
- 生成 `ProjectileDataSO`：速度、半径、命中次数、穿透、弹射、持续时间、命中特效。
- 生成 `ProjectileSpawnRequest`：来源、目标、方向、伤害上下文、技能配置。
- 支持直线、追踪、范围落点、链式、环形、多弹道发射。
- 命中时调用 `DamageSystem.ApplyDamage`。

## 实现流程
1. 先将现有 Bullet 行为包装成通用 Projectile。
2. 将发射逻辑从具体 Skill 中抽成 `ProjectileFactory` 或 `ProjectileManager`。
3. 接入对象池，OnSpawn 初始化，OnDespawn 清理目标和状态。
4. 使用 LayerMask 和 NonAlloc 碰撞检测降低开销。

## 数据流
`SkillSystem -> ProjectileSpawnRequest -> PoolManager -> ProjectileController -> DamageSystem -> OnProjectileHit`

## 验收标准
- 不同技能可以复用同一投射物基础逻辑。
- 投射物支持穿透、弹射、命中次数和生命周期限制。
- 投射物回收后不会保留旧目标或旧伤害数据。

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

