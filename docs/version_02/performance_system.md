# Performance System（阶段 6）

## 脚本

| 文件 | 职责 |
|------|------|
| `PerformanceManager` | 预算计数、移动端帧率/画质、敌人 Update 降频 |
| `PerformanceBudgetSO` | 上限与移动端参数 |
| `GameDebug` | 运行时日志门控 |
| `CombatEffectSpawner` | 命中特效 + 预算 |
| `PooledTimedVfx` | 特效生命周期与回池 |

## 依赖

- L0：`ConfigManager`、`SaveManager`、`PoolManager`
- 不依赖 UI / Shop / Upgrade

## 接入点

- `EnemySpawnerManager`：生成前 `TryAcquire(Enemy)`
- `ProjectileManager`：Spawn/Release 投射物预算
- `DamageNumber` / `DamageNumberController`：飘字预算
- `ProjectileController`：命中特效走 `CombatEffectSpawner`
- `AudioManager`：`maxConcurrentSfx` 读预算
- `AutoAttackController`：扫描间隔读预算

## 事件

- 订阅 `Enemy.Killed` 释放敌人预算（`PerformanceManager`）

## 测试

1. 运行 Bootstrap 场景，Console 无 `[PerformanceManager] 未找到` 警告。
2. 故意将 `maxEnemiesAlive` 设为 5，确认第 6 只不再生成。
3. Profiler 战斗帧 GC ≈ 0（池与日志关闭前提下）。
