# Gameplay Events System（局内随机事件）

## 概述

局内随机事件（第四阶段 **Events**）在地图与波次之上叠加短时修正，通过 `GameplayEventDataSO` 配置触发条件与效果，由 `GameplayEventManager` 调度，经 `MapRuntimeContext` 影响刷怪与局末奖励，并经 `GameEvents` 通知 UI / 调试模块。

与第一阶段 **Event System**（`EventBus` / `GameEvents`）不同：本模块是**玩法内容事件**，不是全局消息总线。

## 数据流

```
GameplayEventDataSO (ConfigDatabase)
  → GameplayEventManager（触发 / 计时 / 结束）
  → MapRuntimeContext（聚合修正）
  → WaveManager / RunRewardSettlementService
  → GameEvents.GameplayEventStarted / Ended
```

地图绑定事件（必定在进图时触发一次）：

```
MapDataSO.linkedGameplayEventIds
  → MapManager.RaiseMapLoaded
  → GameplayEventManager.TryTriggerLinkedMapEvents
```

## 核心类型

| 类型 | 职责 |
|------|------|
| `GameplayEventDataSO` | 事件 ID、触发、持续时间、效果列表 |
| `GameplayEventTriggerConfig` | 触发时机、波次范围、概率 |
| `GameplayEventEffectConfig` | 单条效果（倍率 / Buff / 暂停刷怪等） |
| `GameplayEventManager` | 订阅波次/地图事件，维护活动事件列表 |
| `MapRuntimeContext` | 静态聚合：刷怪间隔、敌人属性、暂停刷怪、奖励倍率 |
| `GameplayEventDebugBridge` | 可选，Console 输出开始/结束 |

## 触发类型

| `GameplayEventTriggerType` | 说明 |
|---------------------------|------|
| `OnMapLoad` | `Map.Loaded` 后按概率尝试（不含 linked 强制列表） |
| `OnWaveStarted` | 每波开始 |
| `OnWaveCompleted` | 每波结束；`endsOnWaveComplete` 的活动事件会一并结束 |
| `RandomDuringWave` | 波次进行中每 8 秒按概率尝试 |

## 效果类型

| `GameplayEventEffectType` | 作用 |
|--------------------------|------|
| `ModifySpawnInterval` | 刷怪间隔乘算（&lt;1 更快） |
| `ModifyEnemyStats` | 敌人属性乘算 |
| `PauseSpawns` | 暂停普通刷怪（Boss 逻辑不变） |
| `ApplyPlayerBuff` | 立即施加 Buff（`stringParam` = buff configId） |
| `ModifyRewardMultiplier` | 局末金币/钻石结算乘算 |

## 事件 Key

| Key | Payload |
|-----|---------|
| `GameplayEvent.Started` | `GameplayEventArgs`（EventConfigId、WaveIndex、DurationSeconds） |
| `GameplayEvent.Ended` | `GameplayEventArgs` |

## 配置资产

| 路径 | configId |
|------|----------|
| `Resources/Config/Map/MapData_Default.asset` | `map.default` |
| `Resources/Config/Map/Events/GameplayEvent_Swarm.asset` | `gameplay_event.swarm` |

默认地图 `linkedGameplayEventIds` 含 `gameplay_event.swarm`：进图即开启虫潮（刷怪间隔 ×0.7，持续 12s 或至波次结束）。

编辑器菜单：**Attack Barbarians → Config → Create Default Map Assets**（可重建/更新上述资产并写入 `ConfigDatabase`）。

## 场景挂载

| 组件 | 挂载位置 |
|------|----------|
| `GameplayEventManager` | `GameSystems`（`GameBootstrapper` 可自动 AddComponent） |
| `GameplayEventDebugBridge` | `GameSystems`（可选，Bootstrap 自动创建） |
| `MapManager` | `GameSystems`（须先于 `GameplayEventManager` 初始化） |

`EventBus` / `GameEvents` 不挂载。

## 测试步骤

1. 确认 `ConfigDatabase.asset` 已登记 `maps` / `gameplayEvents`（或使用编辑器菜单生成）。
2. `GameConfig.defaultMapConfigId` = `map.default`，战斗场景含 `GameBootstrapper`。
3. Play：Console 应出现 `[GameplayEvent] Started id=gameplay_event.swarm`（`GameplayEventDebugBridge`）。
4. 进图后刷怪应明显加快（间隔 ×0.7）；约 12s 或本波结束后出现 `Ended` 日志。
5. 第 2 波起另有 35% 概率再次触发虫潮（`OnWaveStarted`）。
6. 配置 `ModifyRewardMultiplier` 的事件生效时，GameOver 结算日志含 `eventRewardMult=`。

## 未迁移边界

- 无专用事件 UI 面板；UI 阶段订阅 `GameplayEventStarted` 展示横幅即可。
- `RandomDuringWave` 检测间隔固定 8s，未做到 SO 可配。
- 敌人掉落、局内经验不受 `RewardMultiplier` 影响（仅局末结算）。
- Addressables 热更新事件包：仅 `ContentRegistry` / `IContentAssetLoader` 预留，事件表仍在 `ConfigDatabase`。

## 扩展点

- 新增事件：Create → Attack Barbarians → Config → Gameplay Event Data，登记到 `ConfigDatabase.gameplayEvents`。
- 新效果类型：扩展 `GameplayEventEffectType` + `MapRuntimeContext.ApplyEffect` +（若为即时效果）`GameplayEventManager.ApplyInstantEffects`。
- UI：`GameEvents.SubscribeGameplayEventStarted` 显示事件名与剩余时间。
