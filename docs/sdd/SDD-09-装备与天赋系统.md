# SDD-09 装备与天赋系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §9 装备 · §10 天赋 |
| 架构层 | L3 Progression |
| 实现度 | ⚠️ 50% |
| 分期 | 装备 UI 🔜 二期 |

---

## 1. 系统概述

局外永久属性修正：天赋树金币升级、装备穿戴强化与套装加成，合并至 `PlayerRuntimeStats`。

---

## 2. 天赋（TalentManager）

| 项 | 内容 |
|----|------|
| 路径 | `Talent/TalentManager.cs` |
| 存档 | `SaveData.talentLevels` |
| 配置 | `talent.max_hp`, `talent.attack_damage` |
| 接入 | `PlayerRuntimeStats.SetTalentModifiers()` |

**状态**：✅ 后端 · ❌ 专用 UI（仅 ContextMenu）

---

## 3. 装备（EquipmentManager）

### 部位（EquipmentSlot）

Weapon · Helmet · Chest · Boots · Ring

### 当前配置

| 装备 | configId |
|------|----------|
| Battle Axe | equipment.battle_axe |
| Leather Cap | equipment.leather_cap |
| Warrior Chest | equipment.warrior_chest |
| Warrior Boots | equipment.warrior_boots |

**套装** `set.warrior`：4 件套触发 SetBonus

| 项 | 内容 |
|----|------|
| 存档 | `equipmentLevels`, `equippedItems` |
| 接入 | `PlayerRuntimeStats.SetEquipmentModifiers()` |

**状态**：✅ 后端 · ❌ UI · 🔜 PRD 二期主交付

---

## 4. 强化规则

- 天赋：金币扣费，前置等级校验，合并 StatModifier  
- 装备：金币强化 `equipmentLevels`，穿戴槽位 `equippedItems`

---

## 5. 实现状态分析

| 项 | 状态 |
|----|------|
| Manager 逻辑 | ✅ |
| 局内属性生效 | ✅ |
| 配置规模 | ⚠️ 2 天赋 + 4 装备 |
| 成长页 Tab | ❌ |
| 商城装备入口 | 🔜 二期不做 |

---

## 6. 测试要点

- 天赋升级后局内 MaxHp/Damage 上升  
- 穿戴套装后 SetBonus 生效  
- 存档重启保持穿戴状态  
