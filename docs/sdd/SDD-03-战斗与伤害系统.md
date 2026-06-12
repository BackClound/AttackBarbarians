# SDD-03 战斗与伤害系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §4 战斗系统 |
| 架构层 | L1 Combat |
| 实现度 | ✅ 85% |

---

## 1. 系统概述

统一伤害结算、投射物池化、碰撞查询、自动攻击节拍与城墙伤害代理。

---

## 2. 需求追溯

| FR | 状态 |
|----|------|
| FR-COMBAT-04 自动攻击 | ✅ |
| FR-COMBAT-05 统一伤害 | ✅ |
| FR-COMBAT-06 投射物池化 | ✅ |
| FR-COMBAT-14 伤害飘字 | ✅ |

---

## 3. 核心类

| 类 | 路径 |
|----|------|
| `DamageSystem` | `Damage/DamageSystem.cs` |
| `DamagePipeline` | `Damage/DamagePipeline.cs` |
| `DamageCalculationSO` | `Damage/DamageCalculationSO.cs` |
| `AutoAttackController` | `AutoAttack/AutoAttackController.cs` |
| `ProjectileManager` | `Projectile/ProjectileManager.cs` |
| `CollisionManager` | `Collision/CollisionManager.cs` |
| `WallControlManager` | `Wall/WallControlManager.cs` |
| `DamageNumberController` | `UI/DamageNumberController.cs` |

---

## 4. 伤害公式

```
1. SkipCalculation → Base × SkillMultiplier
2. damage = Base × SkillMultiplier
3. + 元素附加 (攻击方元素 Stat × elementStatScale)
4. × 护甲: armorDenominator / (armorDenominator + max(0, 目标Armor - 攻击方ArmorReduce))
5. × 元素倍率 (当前恒 1)
6. 暴击: 1 + CritPower (默认 0.5)
```

**类型**：`Normal` · `Critical` · `Dot` · `True`  
**标签**：`Pierce` · `Chain` · `Area` · `Knockback` · `TrueDamage` · `SkipCalculation`

---

## 5. 战斗数据流

```
AutoAttack/Skill → ProjectileManager
  → Collision/Trigger
  → DamagePipeline.DealDamage
  → DamageSystem.ApplyDamage
  → Entity_Health / 飘字
```

敌人攻击城墙：`EnemyAttackState` → `ExecuteWallAttack` → `WallControlManager` → 玩家生命

---

## 6. 实现状态分析

| 项 | 状态 |
|----|------|
| 护甲/暴击公式 | ✅ |
| 对象池子弹 | ✅ |
| NonAlloc 碰撞 | ✅ |
| 元素抗性 | ❌ GetElementMultiplier 恒 1 |
| 击退标签位移 | ⚠️ 仅打标签 |
| 穿透/弹射/分裂 | ⚠️ 依赖射击 Buff 配置 |

---

## 7. 测试要点

- 高护甲目标减伤符合公式  
- `TrueDamage` 跳过护甲  
- DOT 在 `allowDotCritical=false` 时不暴击  
