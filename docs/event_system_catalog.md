# Event System 事件清单

本文档描述 `GameEvents` + `EventBus` 的事件分区、发布者、订阅者与触发时机。事件 Key 定义于 `GameConstants.EventKeys`。

## 架构

```
发布方 → GameEvents.RaiseXxx → EventBus.Publish → 订阅方 Handler
```

- **门面**：`Assets/Scripts/Events/GameEvents.cs`（唯一推荐发布入口）
- **总线**：`Assets/Scripts/Core/EventBus.cs`（Bootstrap 注册，不挂载）
- **负载**：`Assets/Scripts/Events/GameEventArgs.cs`、`GameStateChange`（GameManager）
- **订阅模板**：`Assets/Scripts/Events/GameEventSubscriberBase.cs`

调试：运行时设置 `GameEvents.EnableDebugLogging = true`。

---

## Game

| Key | Raise 方法 | 发布者 | 典型订阅者 | 触发时机 |
|-----|------------|--------|------------|----------|
| `Game.StateChanged` | `RaiseGameStateChanged` | `GameManager.ChangeState` | UI、输入、各 Manager | 任意状态切换 |
| `Game.Started` | `RaiseGameStarted` | `GameManager.StartGame` | UI、Audio | 进入 Playing |
| `Game.Paused` | `RaiseGamePaused` | `GameManager.PauseGame` | UI、Audio | 暂停 |
| `Game.Resumed` | `RaiseGameResumed` | `GameManager.ResumeGame` | UI、Audio | 恢复 |
| `Game.Over` | `RaiseGameOver` | `GameManager.GameOver` | UI、Save、Audio | 玩家死亡后 |

**Payload**：`GameStateChange`（OldState / NewState）

---

## Wave

| Key | Raise 方法 | 发布者 | 典型订阅者 | 触发时机 |
|-----|------------|--------|------------|----------|
| `Wave.Started` | `RaiseWaveStarted` | *待 WaveManager* | EnemySpawner、UI | 波次开始 |
| `Wave.Completed` | `RaiseWaveCompleted` | *待 WaveManager* | Upgrade、Save、UI | 波次清场 |

**Payload**：`WaveEventArgs`（WaveIndex、DurationSeconds、EnemyCount）

---

## Player

| Key | Raise 方法 | 发布者 | 典型订阅者 | 触发时机 |
|-----|------------|--------|------------|----------|
| `Player.Damaged` | `RaisePlayerDamaged` | `Player_Health.ReduceHp` | UI、Audio、Buff | 墙体/敌人伤害转发后 |
| `Player.Died` | `RaisePlayerDied` | `Player_Health.Die` | UI、Save | HP 归零 |
| `Player.HealthChanged` | `RaisePlayerHealthChanged` | `Player_Health` | UI 血条 | 受伤或治疗 |
| `Player.LevelUp` | `RaisePlayerLevelUp` | *待 UpgradeManager* | Skill、UI | 升级 |

**Payload**：`PlayerHealthEventArgs` 或 `int`（等级）

---

## Enemy

| Key | Raise 方法 | 发布者 | 典型订阅者 | 触发时机 |
|-----|------------|--------|------------|----------|
| `Enemy.Spawned` | `RaiseEnemySpawned` | *待 Spawner/Pool* | Wave 统计 | 从池取出并激活 |
| `Enemy.Killed` | `RaiseEnemyKilled` | `Enemy_Health.Die` | Wave、经验、Audio | HP 归零进入死亡状态前 |

**Payload**：`EnemyEventArgs`（EnemyObject、Position、Killer、EnemyTypeId）

---

## Damage

| Key | Raise 方法 | 发布者 | 典型订阅者 | 触发时机 |
|-----|------------|--------|------------|----------|
| `Damage.Applied` | `RaiseDamageApplied` | `Enemy_Health.ReduceHp` | `DamageNumberController`、DOT | 敌人扣血时 |

**Payload**：`DamageEventArgs`（Amount、WorldPosition、Source、Target、IsCritical）

---

## Skill / Buff / UI / Audio / Save

已定义 Key 与 `RaiseXxx`，待对应模块实现时接入。参见 `GameEvents.cs` 各分区。

---

## 已迁移 vs 未迁移

### 已迁移（本模块）

- `GameManager` → `GameEvents`（状态/暂停/结束/开始）
- `Player_Health` → `Player.Damaged` / `Player.Died` / `Player.HealthChanged`
- `Enemy_Health` → `Damage.Applied` / `Enemy.Killed`
- `Enemy.TakeDamage` 不再直接调用 `DamageNumberController`
- `DamageNumberController` 订阅 `Damage.Applied`

### 未迁移（后续模块）

- `WallControlManager` → 玩家伤害仍走 `Player_Health.TakeDamage`（已间接发事件）
- `EnemyGenerateManager` 生成敌人 → `Enemy.Spawned`
- 波次 `Wave.Started` / `Wave.Completed`
- 技能/Buff/UI/Audio/Save 全部分区
- 移除 `DamageNumberController.numberControllerInstance` 静态入口（确认无引用后）

---

## 订阅模板示例

```csharp
public class MyUiPanel : GameEventSubscriberBase
{
    protected override void RegisterHandlers(EventBus bus)
    {
        GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
    }

    protected override void UnregisterHandlers(EventBus bus)
    {
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
    }

    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is GameStateChange change)
        {
            // 切换面板
        }
    }
}
```

非 MonoBehaviour：在 `Initialize` 中 `GameEvents.SubscribeXxx`，在 `Shutdown` 中 `UnsubscribeXxx`。
