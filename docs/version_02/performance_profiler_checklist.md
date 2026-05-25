# Profiler 检查清单与性能预算

> 阶段 6 · 性能 / GC / 内存 / 移动端

## 性能预算（默认 `PerformanceBudget_Default`）

| 类别 | 默认上限 | 移动端缩放 | 说明 |
|------|----------|------------|------|
| 敌人存活 | 80 | ×0.75 | `EnemySpawnerManager` 生成前 `TryAcquire` |
| 投射物 | 120 | ×0.75 | `ProjectileManager.Spawn` |
| 伤害飘字 | 32 | ×0.75 | `DamageNumberController` |
| 战斗特效 | 48 | ×0.75 | `CombatEffectSpawner` |
| 同时 SFX | 12 | ×0.75 | `AudioManager` |

目标扫描：空闲 0.12s / 交战 0.25s（`AutoAttackController`，可覆盖 `AutoAttackDataSO`）。

敌人逻辑：距玩家 &gt; 18 单位时，非 Boss/精英/特殊怪每 3 帧执行一次 `EnemyController.Update`。

## Unity Profiler 检查项

### CPU

- `PlayerController` / `AutoAttackController` 目标扫描（应无每帧全量扫描）
- `EnemyController.Update` 总量随敌人数线性增长
- `ProjectileController.Update` + `ScanHitsNonAlloc`
- UI `Canvas.BuildBatch` / TMP 重建

### GC Alloc

- 战斗循环帧应接近 **0 B**（避免 `List.Sort` 委托、`LINQ`、`string` 拼接）
- 对象池 `Spawn`/`Despawn` 无临时集合
- 伤害飘字、子弹、敌人死亡无 `Instantiate`/`Destroy`（池配置完整时）

### Physics2D

- 使用 `OverlapCircleNonAlloc`（`CollisionQuery` / `PlayerTargetScanner`）
- LayerMask 仅包含 Enemy / Wall / Projectile 必要层

### Rendering

- 同屏 50+ 敌人时 Batches / SetPass 是否陡增
- 移动端 Quality 档位是否已降档（`PerformanceManager.ApplyGraphicsFromSave`）

### UI

- 面板切换优先 `CanvasGroup` 而非频繁 `SetActive` 整树
- 飘字数量受预算限制

### Memory

- Memory Profiler：池化对象是否稳定平台
- 无泄漏：反复进出战斗场景后 `PoolManager` 活跃数回落

## 对比测试步骤

1. 菜单 **Attack Barbarians → Performance → Create Default Performance Assets**。
2. 场景 `GameSystems` 挂载 `PerformanceManager`（或由 Bootstrap 自动 `AddComponent`）。
3. `PoolConfig` / `PoolManager` 配置 Enemy、Bullet、DamageNumber（可选 CombatVfx）。
4. `GameConfig.enableRuntimeLogs = false` 复测 GC。
5. Profiler **Deep Profile** 关闭，录制 30s 高压力波次，对比优化前后 **GC.Alloc** 与 **CPU ms**。

## 场景挂载

```
GameSystems
├── PerformanceManager  ← 新建或 Bootstrap 自动创建
├── PoolManager
└── ...
```

`GameConfig` → **Performance Budget** 引用 `PerformanceBudget_Default`。

## 未迁移 Legacy 边界

- `SkillAreaZone` 仍使用 `new GameObject` + `Destroy`（技能范围区，低频）
- 地图背景 `MapManager` 仍 `Instantiate` 背景 Prefab（切换地图时）
- 对象池未配置时敌人/子弹回退 `Instantiate`（会打 `GameDebug.LogWarning`）
