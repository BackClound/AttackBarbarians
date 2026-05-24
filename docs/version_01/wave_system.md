# Wave Spawn System

## 职责
- `WaveDataSO`：波次时长、生成间隔、上限、`WaveEnemyEntry` 组合、Boss 与奖励表 ID。
- `WaveEnemyEntry`：敌人 configId、权重、时间窗、条目属性倍率。
- `WaveManager`：`StartWave` / `StopWave` / `SpawnNext` / `OnEnemyDied` / `CompleteWave`（经 `RaiseWaveCompleted`）。
- `SpawnAreaController`：相机视口矩形生成区（与旧 `EnemyGenerateManager` 一致）。
- `EnemySpawnerManager`：对象池生成、`WaveSpawnSelector` 加权选取。
- `GameFlowManager`：订阅 `WaveCompleted` → `WaveTransition` → `UpgradeChoosing`。

## 数据流
```
WaveDataSO → WaveManager.Tick
    → EnemySpawnerManager.TrySpawnEnemy / TrySpawnBoss
    → PoolManager.Spawn
    → EnemyController.InitializeForSpawn
    → GameEvents.EnemyKilled → WaveManager.OnEnemyDied
    → GameEvents.WaveCompleted → GameFlowManager
```

## 波次完成条件（满足任一）
1. 波次时长达到 `WaveDuration`（默认 30s）。
2. 已生成数量达 `MaxSpawnCount` 且场上无存活敌人（含 Boss 清空条件）。
3. 配置了 Boss 且 `RequireBossDefeatToComplete`：Boss 已生成并被击杀。

## 场景挂载
1. **GameSystems** 上挂 `GameBootstrapper`、`WaveManager`、`EnemySpawnerManager`。
2. `EnemySpawnerManager` 可引用同物体上的 `SpawnAreaController`（未指定时自动 AddComponent）。
3. `WaveManager.spawner` 指向 `EnemySpawnerManager`；`waveConfigIds` 含 `wave.01`。
4. 配置：`Assets/Resources/Config/Wave/WaveData_01.asset` 写入 `ConfigDatabaseSO.Waves`。
5. **EnemyGenerateManager**：`EnemySpawnerManager` 初始化时默认 `SetAutoSpawnEnabled(false)`，保留场景引用。

## 状态机重构（Player / Enemy）
| 职责 | 实现 |
|------|------|
| 状态机 Tick | `PlayerController` / `EnemyController`（`IEntityStateMachineHost`） |
| 玩家战斗准入 | `PlayerState.TryEnterShootState` → `ScanCombatTargets` + `CanEnterCombatState` |
| 敌人墙体探测 | `EnemyState.IsWallInAttackRange` → `EnemyController` |
| 敌人移动 | `EnemyState.ApplyMoveVelocity` / `StopMovement` |
| 攻击帧 | Animator → `Enemy.OnAnimatorAttackTrigger` → `EnemyAttackState` → `ExecuteWallAttack` |

## 测试步骤
1. 进入 `BattleScene`，Play 后 Console 无 WaveManager 每帧 Warning。
2. 约每 `spawnInterval` 秒在屏幕上方生成蝙蝠，30s 或清场后进入 WaveTransition。
3. 升级选单确认后进入第 2 波（`currentWaveIndex + 1`）。
4. 敌人贴墙攻击：城墙掉血；玩家射程内自动 Shoot。
5. Inspector：`WaveManager` 右键可配合 `GameFlowManager.SimulateWaveCompletedForTest` 验证流程。

## 未迁移边界
- `EnemyGenerateManager` 仍保留，仅关闭自动刷怪。
- `WaveEnemyEntry.maxSpawnCount` 条目限额尚未在 `WaveManager` 逐条扣减（仅用权重与时间窗）。
- `rewardTableId` 仅配置字段，局内掉落结算待 Upgrade/Drop 模块接入。
