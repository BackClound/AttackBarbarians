# SDD-08 局外成长与升级卡

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §10 局外成长 |
| 架构层 | L3 / L5 |
| 实现度 | ⚠️ 60% |

---

## 1. 系统概述

局外持久化成长：升级卡库存、多来源发卡、Meta 奖励（在线/离线/抽奖/通关）、局末结算。

---

## 2. 核心类

| 类 | 路径 |
|----|------|
| `UpgradeCardManager` | `UpgradeCard/UpgradeCardManager.cs` |
| `MetaRewardService` | `UpgradeCard/MetaRewardService.cs` |
| `RunRewardSettlementService` | `Managers/RunRewardSettlementService.cs` |
| `DailyRewardManager` | `DailyReward/DailyRewardManager.cs` |
| `AchievementManager` | `Achievement/AchievementManager.cs` |
| `SkillUnlockService` | `SkillSystem/SkillUnlockService.cs` |

---

## 3. 升级卡（13 张）

**技能 ×8**：shoot, fire_rain, ice, lightning, thunder, water_wave, heal, generic  
**属性 ×5**：max_hp, damage, move_speed, attack_speed, generic

**奖池 ×8**：shop_crate_common/premium, run_settlement, online/offline_reward, lottery, daily_reward, stage_reward

资产：`Resources/Config/UpgradeCard/` — ✅ 已生成并注册 ConfigDatabase

---

## 4. 发卡来源

| 来源 | 服务 | UI 入口 | 状态 |
|------|------|---------|------|
| 商城宝箱 | ShopManager | ShopCrateRewardPopup | ✅ |
| 局末结算 | RunRewardSettlementService | GameOverPanel | ⚠️ 卡列表 UI 无 |
| 在线/离线 | MetaRewardService | MainSceneRewardPanel | ⚠️ |
| 抽奖 | MetaRewardService | MetaRewardPagePanel | ⚠️ 无保底 |
| 签到 | DailyRewardManager | DailyRewardPanelUI | ⚠️ |
| 通关 | MetaRewardService | MainSceneRewardPanel | ✅ 服务 |

---

## 5. Meta 奖励计时（默认）

| 类型 | 间隔 |
|------|------|
| 在线 | 900s (15min) |
| 离线 | 28800s (8h) 上限 |
| 抽奖 | 3600s 冷却 |
| 通关 | 86400s (24h) |

---

## 6. 消耗逻辑（未实现）

PRD 要求：消耗 N 张卡 → `skillLevels` 或 `attributeBaseLevels` +1。

| 项 | 状态 |
|----|------|
| TryUseCard / TryConsume | ❌ |
| attributeBaseLevels → 战斗 | ❌ |
| 背包 UI | ❌ |
| 通用卡消耗替代 | ❌ |

---

## 7. 实现状态分析

| 项 | 状态 |
|----|------|
| 库存读写存档 | ✅ |
| 权重抽取 + 通用卡解析 | ✅ |
| 奖池预览 | ✅ |
| 多服务集成 | ✅ |
| 卡片消耗养成 | ❌ P0 |
| gachaPityCounter | ❌ |
| 图标资源 | ⚠️ 占位 |

---

## 8. 待办

见 `docs/TODO.md`

---

## 9. 测试要点

- 各来源发卡后 `upgradeCardInventory` 增加  
- 重启存档保持  
- 通用技能卡解析为 7 技能之一  
