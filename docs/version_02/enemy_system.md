# Enemy 系统实现说明

## 概述

敌人系统已接入配置驱动、对象池、波次刷怪与事件总线。按产品需求调整：

- **击杀**：仅增加玩家**局内经验**（`EnemyDataSO.experienceReward`），**死亡不掉落**金币/钻石。
- **局末结算**：`GameOver` 时由 `RunRewardSettlementService` 按**单局游玩时长**（每 5 分钟升一档）与 **难度**（`SaveData.settings.gameDifficulty`）结算金币/钻石，含最低奖励与每档微弱随机浮动。

## 数据流

```
WaveDataSO → WaveManager → EnemySpawnerManager → PoolManager → EnemyController
    → Enemy_Health → GameEvents.EnemyKilled → PlayerExperienceService（经验）
GameOver → RunSessionTracker（时长）→ RunRewardSettlementSO → SaveData.gold/diamonds
```

## 新增脚本

| 脚本 | 挂载 |
| --- | --- |
| `EnemyController` | 敌人 Prefab 根节点 |
| `EnemySpawnerManager` | GameSystems（或场景管理物体） |
| `WaveManager` | GameSystems |
| `PlayerExperienceService` | GameSystems |
| `RunSessionTracker` | GameSystems |
| `RunRewardSettlementService` | GameSystems |
| `RunRewardSettlementSO` | `Resources/Config/RunReward/RunRewardSettlement_Default.asset` |

## 配置

- **敌人**：`Assets/Resources/Config/Enemy/EnemyData_*.asset`（`experienceReward`、`abilityTags`、`poolKey`）
- **波次**：`Assets/Resources/Config/Wave/WaveData_*.asset`（`enemyConfigIds`、`statScalePerWave`）
- **局末奖励**：`RunRewardSettlement_Default`（`minutesPerTier=5`、`minimumGold`、各档 `baseGold`/`variance`、难度倍率）

## 场景与 Prefab

1. **GameSystems**：Bootstrap 会自动 `AddComponent` 上述 Manager（也可手动拖入 Inspector）。
2. **敌人 Prefab**：添加 `EnemyController`；保留 `Enemy`、`Enemy_Health`、`Entity_Stats`、状态机与 `IPoolable`。
3. **对象池**：`PoolManager` 中注册 `Enemy` 等 `poolKey`。
4. **旧刷怪**：`EnemyGenerateManager` 在 `EnemySpawnerManager` 初始化时默认 `SetAutoSpawnEnabled(false)`，可并存。

## 测试步骤

1. 进入战斗场景，确认 Bootstrap 后 `WaveManager` 开始刷怪（Playing 状态）。
2. 击杀敌人：Console 可开 `GameConfig.EnableRuntimeLogs`，观察经验与 `Player.LevelUp`。
3. 故意结束游戏（玩家死亡）：检查 `Run.RewardSettled` 与存档 `gold`/`diamonds` 增加。
4. 修改 `settings.gameDifficulty` 或拉长对局超过 5 分钟，验证奖励档位与随机区间。

## 未迁移边界

- `EnemyGenerateManager` 仍保留，默认关闭自动刷怪。
- `DamageSystem.ApplyDamage(DamageInfo)` 未统一，仍为 `TakeDamage(float)`。
- 能力标签（冲锋/护盾等）仅有 `IEnemyAbility` 接口，无具体实现组件。
- Boss 波次、掉落表、伤害飘字订阅方可后续接入 `EnemyKilled` / `RunRewardSettled`。
