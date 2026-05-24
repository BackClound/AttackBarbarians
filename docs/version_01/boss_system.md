# Boss & Elite System

## 概述

Boss 与 Elite 模块在现有 `Enemy` / `WaveManager` / `EnemySpawnerManager` 之上旁路接入，不破坏旧 Prefab 与 `EnemyGenerateManager` 兼容层。

## 数据流

```
WaveDataSO → WaveManager.TrySpawnBoss
  → EnemySpawnerManager.SpawnBossInternal
  → EnemyController.InitializeForSpawn
  → BossController.Initialize
  → BossPhaseController / BossSkillRunner
  → GameEvents (BossSpawned / PhaseChanged / Defeated)
```

精英模式：

```
GameConfig.EliteModeConfig → RunDifficultyBootstrap → RunDifficultyContext
  → EnemyController.ApplyEliteScaling（全局或个体 Elite 标记）
```

## 核心类型

| 类型 | 职责 |
|------|------|
| `BossDataSO` | Boss ID、基础敌人、阶段阈值、技能列表、奖励 |
| `BossSkillDataSO` | 冲锋/召唤/范围/护盾/弹幕技能参数 |
| `BossController` | 出场、数值覆盖、阶段与技能驱动、事件发布 |
| `BossPhaseController` | 血量/时间阶段切换与属性修正 |
| `BossSkillRunner` | 技能冷却与释放 |
| `EliteModeConfigSO` | 精英局四维倍率 + 精英个体额外倍率 |
| `RunDifficultyContext` | 单局精英模式开关 |
| `EliteController` | 精英个体标记与 `EliteSpawned` 事件 |

## 事件 Key

| Key | Payload |
|-----|---------|
| `Boss.Spawned` | `BossSpawnedEventArgs` |
| `Boss.PhaseChanged` | `BossPhaseChangedEventArgs` |
| `Boss.Defeated` | `BossDefeatedEventArgs` |
| `Elite.Spawned` | `EliteSpawnedEventArgs` |

订阅示例：`BossHudPresenter`、`BossRunStatsBridge`；UI/Audio/Save 系统仅订阅事件，不反向引用 Boss 内部状态。

## 配置资产

| 路径 | configId |
|------|----------|
| `Resources/Config/Boss/BossData_BatKing.asset` | `boss.bat_king` |
| `Resources/Config/Boss/Skill/BossSkillData_AreaSlam.asset` | `boss_skill.area_slam` |
| `Resources/Config/Boss/Skill/BossSkillData_SummonBats.asset` | `boss_skill.summon_bats` |
| `Resources/Config/Elite/EliteModeConfig_Default.asset` | （非 ConfigDataBase，Resources 路径加载） |
| `Resources/Config/Wave/WaveData_01.asset` | 已启用 Boss @ 20s、精英概率 8% |

须在 `ConfigDatabase.asset` 的 `bosses` / `bossSkills` 列表中登记上述资产。

## 场景 / Prefab 挂载

| 组件 | 挂载位置 |
|------|----------|
| `BossRunStatsBridge` | `GameSystems`（`GameBootstrapper` 可自动 AddComponent） |
| `BossHudPresenter` | 战斗 UI Canvas（可选，未绑 UI 时打日志） |
| `BossController` | Boss Prefab 根节点（**可选**；缺失时生成 Boss 自动 `AddComponent`） |
| `EliteController` | 敌人 Prefab（**可选**；精英生成时自动添加） |

`GameConfig.asset`：

- 拖入 `Elite Mode Config` → `EliteModeConfig_Default`
- 调试精英全局倍率：勾选 `Start With Elite Mode`

## 测试步骤

1. 打开含 `GameBootstrapper` + `WaveManager` 的战斗场景，Play。
2. 波次进行约 **20 秒** 应生成 Bat King，Console 出现 `[BossHud] Boss 出现` 或 `BossSpawned`（开启 `GameEvents.EnableDebugLogging`）。
3. 击打 Boss 至 66%/33% 血量应触发阶段事件与 SFX Key（`audio.sfx.boss_phase`）。
4. 击杀 Boss 后：波次在 `RequireBossDefeatToComplete` 下完成；经验含 `bonusExperience`；`BossRunStatsBridge` 计数 +1。
5. 精英：将 `GameConfig.startWithEliteMode` 设为 true，普通敌人 HP/攻击应高于普通局；`WaveData` 的 `eliteSpawnChance` 会额外刷精英个体。

## 未迁移边界

- `IMPLEMENTATION.md` 中的 `BattleRunController` / `BossEnemyMarker` **未实现**；本模块用 `BossController` + 配置表替代时间点刷 Boss。
- 里程碑 5/10/15/20 分钟 Boss 调度仍可由后续 `TimeDirector` 订阅 `WaveManager` 或直调 `EnemySpawnerManager.TrySpawnBoss`。
- 正式 Boss 血条 UI 美术、Boss BGM/音效资源需 Audio 系统接 `GameEvents.RaiseAudioPlayMusic/Sfx`。
- Save 持久化 `BossKillCount`：`BossRunStatsBridge` 仅内存计数，存档写入待 Save 模块订阅 `BossDefeated`。

## 扩展点

- 新 Boss：复制 `BossData` + 技能 SO，登记 Database，`WaveDataSO.bossConfigId` 引用。
- 新技能：扩展 `BossSkillType` 与 `BossSkillRunner.TryCast` 分支。
- 阶段机制：在 `BossDataSO` 使用 `ElapsedTime` 模式或增加 `BossPhaseTransitionMode` 枚举值。
