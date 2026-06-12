# SDD-04 技能系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §5 技能系统 |
| 架构层 | L1 Combat |
| 实现度 | ⚠️ 70% |

---

## 1. 系统概述

7 元素技能运行时：解锁、冷却、Buff 聚合、自动施法与效果实现。

---

## 2. 技能清单

| SkillType | configId | 效果类 | 配置资产 | 战斗验证 |
|-----------|----------|--------|----------|----------|
| Shoot | skill.shoot | ShootSkillEffect | SkillData_Shoot | ✅ |
| FireRain | skill.fire_rain | FireRainSkillEffect | SkillData_FireRain | ⚠️ |
| Ice | skill.ice | IceSkillEffect | SkillData_Ice | ⚠️ |
| Lightning | skill.lightning | LightningSkillEffect | SkillData_Lightning | ⚠️ |
| Thunder | skill.thunder | ThunderSkillEffect | SkillData_Thunder | ⚠️ |
| WaterWave | skill.water_wave | WaterWaveSkillEffect | SkillData_WaterWave | ⚠️ |
| Heal | skill.heal | HealSkillEffect | SkillData_Heal | ⚠️ |

---

## 3. 核心类

| 类 | 路径 |
|----|------|
| `SkillManager` | `SkillSystem/Core/SkillManager.cs` |
| `SkillUnlockService` | `SkillSystem/SkillUnlockService.cs` |
| `SkillEffectFactory` | `SkillSystem/Effects/SkillEffectImplementations.cs` |
| `BuffManager` | `SkillSystem/Buff/BuffManager.cs` |
| `SkillBuffCatalog` | `SkillSystem/Buff/SkillBuffCatalog.cs` |
| `SkillCastVfxPlayer` | `SkillSystem/Vfx/SkillCastVfxPlayer.cs` |

---

## 4. 解锁规则

| 技能 | 累计游玩秒数 |
|------|-------------|
| Shoot | 0 |
| Lightning | 600 |
| Heal | 900 |
| Thunder | 1800 |
| FireRain | 3600 |
| Ice | 5400 |
| WaterWave | 7200 |

判定：`SaveData.skillLevels > 0` 或 `statistics.totalPlayTimeSeconds` 达标。

**缺口**：`SkillUnlockTableSO` 未绑定 `ConfigDatabase`，走代码 Fallback。

---

## 5. SkillBuffKind（局内构筑）

按技能分：射击（弹道/双发/穿透/弹射/分裂）、闪电（多道/连锁/爆炸/麻痹）、落雷、火雨、水浪、冰霜、恢复；另含全局攻速/伤害/暴击/冷却。

---

## 6. 实现状态分析

| 项 | 状态 |
|----|------|
| 7 效果类代码 | ✅ |
| 自动施法 + 冷却 | ✅ |
| Buff 路由 | ✅ |
| 局外时长解锁 | ✅ |
| 技能平衡与 VFX | ⚠️ |
| 解锁表资产注册 | ⚠️ |
| 局外 skillLevels 消耗升级 | ❌ |

---

## 7. 参考

`docs/skill_effect_flow_lightning_fire_rain_water_wave.md`

---

## 8. 测试要点

- 未解锁技能不可施放  
- 累计时长达标后下局解锁  
- 各技能 `TryAutoCast` 有目标时触发  
