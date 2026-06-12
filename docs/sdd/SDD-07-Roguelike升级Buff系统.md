# SDD-07 Roguelike 升级 Buff 系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §6 Buff 系统 |
| 架构层 | L3 Progression |
| 实现度 | ⚠️ 65% |

---

## 1. 系统概述

局内三选一：从奖励池抽取升级选项，应用属性 Buff 或技能 Buff，记录局内已选叠层。

---

## 2. 核心类

| 类 | 路径 |
|----|------|
| `UpgradeManager` | `Upgrade/UpgradeManager.cs` |
| `RandomRewardManager` | `Upgrade/RandomRewardManager.cs` |
| `UpgradeApplicator` | `Upgrade/Core/UpgradeApplicator.cs` |
| `RewardPoolSO` | `Upgrade/Data/RewardPoolSO.cs` |
| `UpgradeOptionSO` | `Upgrade/Data/UpgradeOptionSO.cs` |
| `BuffDataSO` | `Config/Data/BuffDataSO.cs` |
| `UpgradePanelUI` | `UI/Panels/UpgradePanelUI.cs` |

---

## 3. 效果类型（UpgradeEffectType）

`StatBuff` · `SkillBuff` · `SkillUnlock` · `SkillLevelUp` · `ResourceGold` · `ResourceDiamond` · `WeaponEnhance`

---

## 4. 触发流程

```
WaveCompleted → GameFlowManager → UpgradeChoosing
  → RandomRewardManager.OnUpgradeSelectionOpened
  → UpgradeManager.TryRollChoices
  → GameEvents.RaiseUpgradeChoicesReady
  → UpgradePanelUI 展示
  → TrySelectChoice → TryApplyChoice → ConfirmUpgradeSelection

PlayerLevelUp (Playing 中) → BeginUpgradeChoosing → 同上
```

---

## 5. 抽取规则

1. 按波次解析 `RewardPoolSO`  
2. 过滤：前置条件、互斥组、叠层上限  
3. 加权随机，不重复  
4. 取 `min(ChoiceCount, 候选数)` 个  

---

## 6. GDD 四循环（未实现）

| 设计 | 代码 |
|------|------|
| buffEventCounter 每次升级 +1 | ❌ 无字段 |
| =4 时强制基础属性池并 -4 | ❌ |
| 暂停不重置 | — |

**当前**：波次完成与升级均走同一默认池逻辑。

---

## 7. 配置状态

| 资产 | 状态 |
|------|------|
| ConfigDatabase.rewardPools | ⚠️ 可能为空 |
| ConfigDatabase.upgradeOptions | ⚠️ 可能为空 |
| BuffData_AttackUp | ✅ 1 条 |
| Editor Bootstrap | `UpgradeConfigBootstrapMenu.cs` |

---

## 8. 实现状态分析

| 项 | 状态 |
|----|------|
| 抽取/互斥/叠层代码 | ✅ |
| UI 三选一 | ✅ |
| 局内存档 selectedUpgrades | ✅ |
| 奖励池资产 | ⚠️ 需 Bootstrap |
| 四循环属性 Buff | ❌ |
| Build 流派配置内容 | ⚠️ 待策划填表 |

---

## 9. 测试要点

- 互斥组不同时出现  
- 达 maxStacks 后选项消失  
- 选择后 `PlayerRuntimeStats` 或 `SkillManager` 变化可观察  
