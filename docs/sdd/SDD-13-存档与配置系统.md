# SDD-13 存档与配置系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §16 存档 · §17 配置 |
| 架构层 | L0 |
| 实现度 | ✅ 90% |

---

## 1. 系统概述

JSON 本地存档、版本迁移、自动保存；ScriptableObject 配置库与 configId 索引。

---

## 2. 存档（SaveManager）

| 项 | 内容 |
|----|------|
| 路径 | `{persistentDataPath}/save.json` |
| 备份 | backup 文件 |
| 版本 | `SaveConstants.CurrentVersion = 3` |
| 防抖 | MarkDirty + 2s |
| 强制保存 | Pause / Quit |

### SaveData 字段组

- 资源：gold, diamonds, energy, adTickets, techPoints  
- 成长：talentLevels, equipmentLevels, skillLevels, upgradeCardInventory, attributeBaseLevels  
- 商店/签到/Meta 时间戳  
- settings, statistics, runProgress  

### RunProgressData

currentWave, currentLevel, currentHp, selectedUpgrades[], activeBuffs[]

---

## 3. 版本迁移

`SaveVersionMigrator`：v0→v1→v2→v3（含 upgradeCardInventory）

**待验证**：旧档升级后新字段默认值。

---

## 4. 配置（ConfigManager）

| 项 | 内容 |
|----|------|
| 聚合 | ConfigDatabaseSO |
| 索引 | configId → *DataSO |
| 根目录 | Resources/Config/{Module}/ |
| 校验 | ConfigValidator（升级卡交叉校验待补） |

### Editor Bootstrap 菜单

Shop · Audio · Ad · Achievement&Daily · UpgradeCard · Upgrade · Skill · Map · Performance

---

## 5. 配置原则

1. 平衡参数零硬编码  
2. 存档只用 configId 字符串  
3. 废弃 ID 不回收  

---

## 6. 实现状态分析

| 项 | 状态 |
|----|------|
| 读写/备份/导入导出 | ✅ |
| 自动存档 | ✅ |
| v3 升级卡字段 | ✅ |
| gachaPityCounter | ❌ PRD 待扩展 |
| buffEventCounter 持久化 | ❌ |
| ConfigDatabase 部分表空 | ⚠️ rewardPools 等 |
| Cloud Save | 🔜 |

---

## 7. 测试要点

- 杀进程重启数据保持  
- 主档损坏走 backup  
- Bootstrap 后 ConfigManager 索引非空  
