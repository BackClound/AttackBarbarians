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
