# Object Pool System

## 概述

统一对象池管理敌人、子弹、伤害飘字等高频对象，降低 `Instantiate` / `Destroy` 与 GC 压力。由 `PoolManager`（`IGameSystem`）在 Bootstrap 时初始化，通过 `ServiceLocator` 获取。

## 文件结构

```
Assets/Scripts/Pool/
├── IPoolable.cs
├── ObjectPool.cs          # 泛型单池
├── PoolEntry.cs
├── PoolConfigSO.cs
├── PoolOverflowPolicy.cs
└── PoolManager.cs
```

## API 速查

| API | 说明 |
|-----|------|
| `PoolManager.Spawn(key, pos, rot, parent?)` | 取出并激活，调用 `IPoolable.OnSpawn` |
| `PoolManager.Spawn<T>(...)` | 同上并返回组件 |
| `PoolManager.Allocate(...)` | 借出但不激活、不记活跃（Skill 子弹预创建） |
| `PoolManager.Despawn(go)` | 回收；非池实例则 `Destroy` |
| `PoolManager.ReturnAllocated(go)` | 归还仅 Allocate 的实例 |
| `PoolManager.HasPool(key)` | 是否已注册 |
| `PoolManager.IsManagedInstance(go)` | 是否由本管理器持有 |
| `PoolManager.ClearAll` / `ClearPool` | 清空 |

### IPoolable

```csharp
void OnSpawn();   // 从池激活后
void OnDespawn(); // 回池前
```

## 配置

1. 创建 **Attack Barbarians → Config → Pool Config**（`PoolConfigSO`），或使用 `PoolManager` Inspector 内 **Inspector Pools** 列表。
2. 每条 `PoolEntry`：`Key`（与 `GameConstants.PoolKeys` 一致）、`Prefab`、`Initial Count`、`Max Count`、`Can Expand`、`Overflow Policy`、可选 `Parent`。
3. 将 `PoolConfigSO` 拖到 `GameSystems/PoolRoot` 上 `PoolManager.Pool Config`。
4. 推荐 Hierarchy 父节点：`Pool_Bullet`、`Pool_Enemy`、`Pool_DamageNumber`（见 `docs/core_framework_scene_setup.md`）。

### 默认 Key

| Key | 常量 | 用途 |
|-----|------|------|
| `Bullet` | `GameConstants.PoolKeys.Bullet` | 子弹 Prefab |
| `Enemy` | `GameConstants.PoolKeys.Enemy` | 敌人 Prefab |
| `DamageNumber` | `GameConstants.PoolKeys.DamageNumber` | 伤害飘字 Prefab |

`GameConfig.AllowPoolGrowth` 为 false 时，全局禁止池扩容（覆盖 Entry 的 Can Expand）。

### 溢出策略（`PoolOverflowPolicy`）

- **ExpandOrReject**：允许扩容则新建，否则返回 null。
- **Reject**：始终拒绝。
- **RecycleOldest**：回收最早借出的活跃实例再借出。

## 已接入类型

| 类型 | 方式 | 说明 |
|------|------|------|
| `SKillObject_Bullet` | `IPoolable` + `Allocate` / `ReturnAllocated` | `SkillObject_BulletSpawn` 预创建列表；射击时 `OnSpawn` + `SetActive` |
| `DamageNumber` | `IPoolable` + `Spawn` / `Despawn` | `DamageNumberController` 优先走池，无池时 Instantiate 回退 |
| `Enemy` | `IPoolable` + `Spawn` / `Despawn` | `EnemyGenerateManager` 优先生成；`Die()` 回池 |

## 数据流

```
Bootstrap → PoolManager.Initialize()
         → 合并 PoolConfigSO + Inspector Pools
         → ObjectPool.Prewarm per entry

Spawn:  inactive → active → OnSpawn → 使用
Despawn: OnDespawn → inactive
```

## 手动验证

1. 场景 `GameSystems/PoolRoot` 挂 `PoolManager`，配置三条池（子弹/敌人/飘字 Prefab 与 Parent）。
2. 运行：敌人自动刷怪、射击、受击飘字均正常。
3. Profiler：战斗高峰无大量 `Instantiate`/`Destroy`（需已配置池；未配置时仍走旧 Instantiate 回退）。
4. 敌人死亡动画结束后消失并回收到 `Pool_Enemy` 下。

## 未迁移 / 边界

- `SkillShoot` 的 `bulletSpawnPrefab` 波次物体仍 `Instantiate`（低频，非本模块范围）。
- 未在 `PoolManager` 注册的 Key：`Spawn` 返回 null，各调用方保留 `Instantiate` 回退。
- `DamageNumberController` 在无池配置时仍使用本地队列 + `numberPrefab`。
- 特效、掉落物等后续模块再增加 `PoolKeys` 与 Entry。

## 后续任务

- 创建并版本管理 `Assets/Resources/Config/PoolConfig.asset`（可选）。
- Wave/Spawner 统一走配置表驱动池 Key。
- 为 `SkillShoot` 的 bullet spawn 波次评估是否池化。
