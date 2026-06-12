# SDD-06 Boss 系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §8 Boss 系统 |
| 架构层 | L2 Gameplay |
| 实现度 | ⚠️ 55% |

---

## 1. 系统概述

Boss 出场、阶段切换、技能释放与 HUD；由 `WaveManager` 定时触发 `EnemySpawnerManager.TrySpawnBoss`。

---

## 2. 核心类

| 类 | 路径 | 职责 |
|----|------|------|
| `BossDataSO` | `Config/Data/BossDataSO.cs` | Boss 配置 |
| `BossSkillDataSO` | `Config/Data/BossSkillDataSO.cs` | 技能参数 |
| `BossController` | `Boss/BossController.cs` | 初始化、数值 |
| `BossPhaseController` | `Boss/BossPhaseController.cs` | 阶段 |
| `BossSkillRunner` | `Boss/BossSkillRunner.cs` | 技能 CD |
| `BossHudPresenter` | `UI/BossHudPresenter.cs` | HUD |
| `BossRunStatsBridge` | `Boss/BossRunStatsBridge.cs` | 局内统计 |

---

## 3. 出场链路

```
WaveManager.TrySpawnBoss (elapsed >= bossSpawnAtElapsed)
  → EnemySpawnerManager.TrySpawnBoss
  → BossController.Initialize
  → GameEvents.RaiseBossSpawned
```

---

## 4. 阶段系统

| 模式 | 触发 |
|------|------|
| HealthRatio | 当前 HP/MaxHP ≤ 阈值 |
| ElapsedTime | 存活秒数 ≥ 阈值 |

阶段效果：`BossPhaseModifierEntry` 叠加属性修正。

---

## 5. Boss 技能

| 类型 | 实现 | 状态 |
|------|------|------|
| Charge | 向下冲刺 | ✅ |
| Summon | 召唤小怪 | ✅ |
| AreaAttack | 范围打墙 | ✅ |
| Barrage | 多段打墙 | ✅ |
| Shield | 调用 ApplySlow | ⚠️ 语义错误，非护盾 |

---

## 6. 当前配置

| 资产 | 状态 |
|------|------|
| BossData_Bat (bat_king) | ⚠️ skillConfigIds 为空 |
| BossSkillData_AreaSlam / Summon | ⚠️ 存在但未挂接 |
| WaveData_01 Boss @20s | ✅ |

---

## 7. 实现状态分析

| 项 | 状态 |
|----|------|
| 框架与事件 | ✅ |
| 阶段切换 | ✅ |
| 技能内容串联 | ❌ |
| Shield 技能语义 | ⚠️ 需修复 |
| Boss 击杀存档 | ❌ |
| 5/10/15/20 分钟里程碑 | ❌ |
| castDelay/range 字段 | ⚠️ Runner 未消费 |

---

## 8. 测试要点

- Boss 出场暂停普怪（若配置）  
- 阶段切换发布 `Boss.PhaseChanged`  
- 击杀后波次可完成（若 requireBossDefeat）  
