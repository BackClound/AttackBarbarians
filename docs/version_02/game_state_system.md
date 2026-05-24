# Game State Machine 模块说明

## 概述

游戏级状态机将主菜单、加载、战斗、暂停、波次过渡、升级三选一、失败等流程收敛到单一状态源，并通过 `GameEvents` 驱动 UI、音频、存档与后续 Wave/Upgrade 系统。

## 核心类型

| 类型 | 挂载 | 职责 |
|------|------|------|
| `GameState` | 否 | 流程状态枚举 |
| `GameStateMachine` | 否 | 合法跳转、`Time.timeScale`、进入/退出回调 |
| `GameStateChange` | 否 | `Game.StateChanged` 事件 Payload |
| `GameManager` | `GameSystems` | 对外 API、`ServiceLocator` 注册 |
| `GameFlowManager` | `GameSystems` | 波次→过渡→升级→战斗的事件编排 |

## 状态与 timeScale

| 状态 | timeScale | 说明 |
|------|-----------|------|
| Playing、Loading、WaveTransition 等 | 1 | 正常模拟 |
| Paused、UpgradeChoosing、GameOver | 0 | 暂停/选卡/结算 |

**归属：** 仅 `GameStateMachine.ApplyTimeScaleForState` 在成功切换后写入；`GameManager` 不再散落赋值。

## 合法转换（摘要）

```
Bootstrapping → MainMenu | Loading | Playing
MainMenu → Loading | Exiting
Loading → Playing | MainMenu
Playing → Paused | WaveTransition | GameOver | Exiting
Paused → Playing | GameOver
WaveTransition → UpgradeChoosing | Playing
UpgradeChoosing → Playing
GameOver → Restarting | MainMenu | Exiting
Restarting → Loading | Playing
```

主流程：`Playing → WaveTransition → UpgradeChoosing → Playing`  
暂停：`Playing ↔ Paused`

非法跳转会拒绝并（在开启日志时）输出 Warning。

## 事件

| Key | 发布方 | 用途 |
|-----|--------|------|
| `Game.StateChanged` | `GameManager` | 任意状态变化；Payload=`GameStateChange` |
| `Game.Started` / `Paused` / `Resumed` / `Over` | `GameManager` | 与旧 Core 框架兼容 |
| `Upgrade.SelectionOpened` / `Completed` | `GameFlowManager` | 升级 UI（待实现面板） |
| `UI.PanelOpened` / `Closed` | `GameFlowManager` | 面板 Id 见 `GameConstants.UiPanelIds` |
| `Wave.Completed` | 未来 `WaveManager` | 触发 `WaveTransition` |

订阅示例：

```csharp
GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
// 或 GameEvents.SubscribeOnGameStateChanged(...)

private void OnGameStateChanged(GameEventContext ctx)
{
    if (ctx.Payload is GameStateChange change)
    {
        bool inputOn = change.NewState == GameState.Playing;
    }
}
```

## GameManager API

| 方法 | 目标状态 |
|------|----------|
| `StartGame()` | Playing |
| `PauseGame()` / `ResumeGame()` | Paused / Playing |
| `BeginWaveTransition()` | WaveTransition |
| `BeginUpgradeChoosing()` | UpgradeChoosing |
| `CompleteUpgradeAndResume()` | Playing |
| `GameOver()` | GameOver |
| `OpenMainMenu()` / `BeginLoading()` | MainMenu / Loading |
| `TryChangeState` / `CanTransitionTo` | 通用 |

属性：`IsGameplayInputEnabled`、`IsCombatActive`（均为 `Playing` 时为 true）。

## GameFlowManager

- 订阅 `Wave.Completed`、`Player.Died`、`Game.StateChanged`。
- 波次结束后：`Playing → WaveTransition`（协程等待）→ `UpgradeChoosing` 或（配置跳过）→ `Playing`。
- `GameConfig.SkipUpgradeChoosingOnBootstrap`：跳过后续升级阶段，便于当前场景验证战斗循环。
- Inspector 右键：**Debug/Simulate Wave Completed**、**Debug/Confirm Upgrade Selection**。

## 配置（GameConfig）

| 字段 | 说明 |
|------|------|
| `StartGameOnBootstrap` | Bootstrap 后是否 `StartGame()` |
| `SkipUpgradeChoosingOnBootstrap` | 波次结算后不进升级 |
| `DefaultWaveTransitionSeconds` | 过渡 UI 停留秒数 |
| `EnableRuntimeLogs` | 状态机/流程 Debug 日志 |

## 场景挂载

在 `GameSystems` 上增加 `GameFlowManager`（可与 `GameManager` 同物体）。`GameBootstrapper` 留空时会自动 `GetComponentInChildren` 或 `AddComponent`。

启动顺序：`ConfigManager` → `EventBus` → `PoolManager` → `GameManager` → `GameFlowManager` →（可选）`StartGame`。

## 测试步骤

1. 运行 `BattleScene`，确认 `ServiceLocator.Get<GameManager>().CurrentState == Playing`。
2. 令 Player 死亡 → `GameOver`，`timeScale == 0`，Console 有 `Game.StateChanged`（若开日志）。
3. 选中 `GameFlowManager` → **Simulate Wave Completed** → 经 `WaveTransition` 进入 `UpgradeChoosing`（未勾选 Skip）。
4. **Confirm Upgrade Selection** → 回到 `Playing`，`timeScale == 1`。
5. 在 `GameConfig` 勾选 **Skip Upgrade Choosing On Bootstrap**，重复步骤 3 → 应直接回 `Playing`。
6. 确认 `EnemyGenerateManager`、射击、旧 Player 逻辑仍正常。

## 旧逻辑边界

- `Player_Health.Die()` 仍直接调用 `GameManager.GameOver()`；`GameFlowManager` 对 `Player.Died` 做兜底，不替换旧脚本。
- 实体级 `StateMachine`（Player/Enemy/Wall）与游戏级状态机独立，互不替代。
- UI 面板、Audio、Save 仅接收事件，本模块不包含具体面板实现。
- `WaveManager` 未实现前，用 `GameEvents.RaiseWaveCompleted` 或 FlowManager 的 ContextMenu 验证。

## 后续任务

- [ ] `WaveManager` 在波次清空时 `RaiseWaveCompleted`
- [ ] `UpgradePanelUI` 订阅 `Upgrade.SelectionOpened` 并调用 `ConfirmUpgradeSelection`
- [ ] `PausePanelUI` 调用 `PauseGame` / `ResumeGame`
- [ ] Input 模块读取 `GameManager.IsGameplayInputEnabled`
- [ ] 主菜单场景：`OpenMainMenu` → `BeginLoading` → `StartGame`
