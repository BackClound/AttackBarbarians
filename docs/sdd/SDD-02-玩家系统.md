# SDD-02 玩家系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §3 玩家系统 |
| 架构层 | L1 Combat |
| 实现度 | ✅ 85% |

---

## 1. 系统概述

玩家实体、属性聚合、目标扫描、经验升级与表现层状态机。战斗逻辑主路径在 `SkillManager` / `AutoAttackController`，状态机偏动画。

---

## 2. 需求追溯

| FR | 状态 |
|----|------|
| FR-COMBAT-01 竖屏防守 | ✅ |
| FR-COMBAT-08 击杀经验升级 | ✅ |
| FR-META-01 技能解锁接入 | ✅ |

---

## 3. 核心类

| 类 | 路径 | 职责 |
|----|------|------|
| `Player` | `Player/Player.cs` | 实体单例、状态机构建 |
| `PlayerController` | `Player/PlayerController.cs` | 配置、扫描、经验、属性刷新 |
| `PlayerRuntimeStats` | `Player/PlayerRuntimeStats.cs` | 多源修正合并 |
| `PlayerTargetScanner` | `Player/PlayerTargetScanner.cs` | 射程内索敌 |
| `PlayerTargetPolicy` | `Player/PlayerTargetPolicy.cs` | 目标策略枚举 |
| `PlayerExperienceService` | `Player/PlayerExperienceService.cs` | 击杀经验 |
| `PlayerDataSO` | `Config/Data/PlayerDataSO.cs` | 基础配置 |

---

## 4. 属性模型

### StatType（`Stats/StatType.cs`）

MaxHp · MoveSpeed · AttackSpeed · Damage · CritChance · CritPower · Armor · ArmorReduce · 元素伤害 · AttackRadius（独立字段）

### 修正源合并顺序

```
PlayerDataSO.baseStats
+ permanentUpgrades (SaveData)
+ extraModifiers (局内 Upgrade/Buff)
+ talentModifiers (TalentManager)
+ equipmentModifiers (EquipmentManager)
+ activeBuffs (Tick 过期)
→ StatRuntimeSnapshot → Entity_Stats
```

---

## 5. 状态机

| 状态 | 文件 | 行为 |
|------|------|------|
| Idle | `PlayerIdleState.cs` | 待机 |
| Shoot | `PlayerShootState.cs` | 射击动画倍率 |
| Dead | `PlayerDeadState.cs` | 死亡锁定 |

**注意**：实际射击由 `SkillShoot` 驱动，不依赖 `OnAnimAttackTrigger`。

### 目标策略

| 策略 | 行为 |
|------|------|
| NearestToWall | 距城墙最近（默认） |
| BossFirst | 优先 Boss |
| Nearest / LowestHealth | 备选 |

---

## 6. 配置

| 资产 | 路径 |
|------|------|
| 默认玩家 | `Resources/Config/Player/PlayerData_Default.asset` |

---

## 7. 实现状态分析

| 项 | 状态 |
|----|------|
| 属性合并管线 | ✅ |
| 目标扫描 | ✅ |
| 经验升级事件 | ✅ |
| 天赋/装备修正接入 | ✅ |
| attributeBaseLevels 接入 | ❌ |
| 时间经验（GDD 双来源） | ⚠️ 待核对 |
| Shoot 状态机双轨 | ⚠️ 文档需标明主路径 |

---

## 8. 测试要点

- 切换 `targetPolicy` 锁定目标正确  
- 天赋升级后 `PlayerStatsChanged` 触发、局内数值变化  
- 死亡发布 `Player.Died`，进入 GameOver  
