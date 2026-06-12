# SDD-01 核心框架与启动

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §17 技术架构 |
| 架构层 | L0 Foundation |
| 实现度 | ✅ 90% |

---

## 1. 系统概述

负责游戏启动、Manager 生命周期、全局状态机、服务定位与事件总线，是所有子系统的运行底座。

**边界**：不包含具体玩法逻辑；不直接操作 UI 控件。

---

## 2. 需求追溯

| FR | 描述 | 状态 |
|----|------|------|
| — | 场景启动按序初始化 | ✅ |
| — | 跨场景保留 Bootstrap | ✅ |
| — | 游戏状态驱动 UI/音频 | ✅ |

---

## 3. 核心类

| 类 | 路径 | 职责 |
|----|------|------|
| `GameBootstrapper` | `Core/GameBootstrapper.cs` | 启动引导、Manager 注册、Tick |
| `ServiceLocator` | `Core/ServiceLocator.cs` | 服务定位 |
| `GameManager` | `Managers/GameManager.cs` | 状态机门面 |
| `GameStateMachine` | `Core/GameStateMachine.cs` | 状态转换、timeScale |
| `GameFlowManager` | `Managers/GameFlowManager.cs` | 波次→升级流程编排 |
| `EventBus` | `Core/EventBus.cs` | 事件总线 |
| `GameEvents` | `Events/GameEvents.cs` | 类型化事件 API |
| `MonoSingleton<T>` | `Core/Singleton/` | 单例基类 |

---

## 4. Bootstrap 顺序

```
ConfigManager → SaveManager → ResourceManager → ShopManager → AdRewardService
→ AchievementManager → DailyRewardManager → UpgradeCardManager → MetaRewardService
→ TalentManager → EquipmentManager → PerformanceManager → PoolManager
→ GameManager → GameFlowManager → RunSessionTracker → RunRewardSettlementService
→ PlayerExperienceService → UpgradeManager → RandomRewardManager
→ EnemySpawnerManager → WaveManager → BossRunStatsBridge
→ DamageSystem → CollisionManager → ProjectileManager
→ SkillUnlockService → ContentRegistry → MapManager → GameplayEventManager
→ AudioManager
```

**场景集**：`BootstrapManagerSet.MainScene` / `BattleScene` / `All`

---

## 5. 游戏状态机

| 状态 | timeScale | 说明 |
|------|-----------|------|
| Bootstrapping | 1 | 初始化 |
| Playing | 1 | 战斗 |
| WaveTransition | 1 | 波次过渡 |
| UpgradeChoosing | 0 | 三选一 |
| Paused | 0 | 暂停 |
| GameOver | — | 结算 |

---

## 6. 实现状态分析

| 项 | 状态 | 说明 |
|----|------|------|
| Bootstrap 顺序 | ✅ | 已实现 |
| ServiceLocator | ✅ | 替代散落 Find |
| 状态机 | ✅ | 含非法转换拒绝 |
| GameFlow 编排 | ✅ | 波次→升级闭环 |
| asmdef 模块化 | ⚠️ | 工具就绪，默认 Assembly-CSharp |
| 旧 MetaGameService | ❌ | 已废弃，勿引用 |

---

## 7. 已知问题

1. `docs/version_02/architecture_design.md` 中 Economy 标「待实现」已过时  
2. asmdef Phase 0 未启用，长期需迁移降低编译时间  

---

## 8. 测试要点

- 两场景 Bootstrap 无重复单例冲突  
- `UpgradeChoosing` 时 `timeScale=0`  
- `GameFlowManager.SimulateWaveCompletedForTest` 可验证流程  
