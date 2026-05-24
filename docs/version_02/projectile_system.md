# Projectile System

## 职责
- `ProjectileDataSO`：速度、碰撞层、穿透/弹射/命中次数、生命周期、池 Key。
- `ProjectileSpawnRequest`：来源、目标、方向、伤害上下文与弹道排布。
- `ProjectileController`：移动、命中、`DamagePipeline.Apply`、池回收。
- `ProjectileManager`：对象池生成、Fan/Ring 排布、释放实例。

## 数据流
`SkillShoot` → `SkillObject_BulletSpawn` → `ProjectileSpawnRequest` → `PoolManager` → `ProjectileController` → `DamageSystem` → `GameEvents.RaiseProjectileHit`

## 场景挂载
1. `GameSystems` 上添加 `ProjectileManager`（或由 `GameBootstrapper` 自动 AddComponent）。
2. `PoolManager` 中保留 Key `Bullet`，Prefab 为 `Assets/Prefabs/SkillObject/Bullet.prefab`（根节点挂 `ProjectileController`）。
3. 可选：`Assets/Resources/Config/Projectile/Projectile_Default.asset` 拖入 `ProjectileManager.defaultProjectileData`。

## 测试步骤
1. 进入 `BattleScene`，确认 `GameBootstrapper` 已引导。
2. 靠近敌人触发 Player 射击动画，观察子弹从池生成、命中敌人、伤害数字弹出。
3. 命中后子弹应回收到池（`Inactive`），再次射击不应保留旧方向外的异常状态。
4. Console 无 `[ProjectileManager] 未注册` / `缺少 ProjectileDataSO` 警告。

## 已迁移的旧逻辑
| 旧代码 | 新归属 |
|--------|--------|
| `SKillObject_Bullet` 移动/碰撞/伤害/回收 | `ProjectileController` |
| `SkillObject_BulletSpawn` 子弹列表与 `Allocate` | `ProjectileManager` + 对象池 `Spawn` |
| `SkillObject_Base.DoDamage`（子弹路径） | `DamagePipeline.Apply` via `ProjectileController` |

## 未迁移边界
- `SkillObject_Base` 仍保留，供未来非投射物技能载体使用。
- `SkillShoot` 的敌人扫描与冷却逻辑未改动，仅发射点改为调用 `ProjectileManager`。
- 链式闪电、落雷范围等技能需新增 `ProjectileDataSO` 与对应 Pattern 调用。

## 扩展点
- 新技能：创建 `ProjectileDataSO`，调用 `ProjectileManager.Spawn` / `SpawnPattern`。
- 订阅 `GameEvents.SubscribeProjectileHit` 播放命中特效。
- `ProjectileMotionType.Chain` 可在 `ProjectileController` 内扩展弹跳目标搜索。
