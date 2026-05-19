# Damage System 模块需求提示词

## 目标
建立统一伤害系统，支持普通伤害、暴击、护甲、元素伤害、DOT、范围伤害、穿透、弹射、减益和伤害飘字。

## 当前基础
当前 `IDamagable.TakeDamage(float damage)` 只传递数值，`AttackInfo` 尚未接入，暴击、元素、护甲、减益和攻击来源无法表达。

## 输出要求
- 生成 `DamageInfo`：攻击来源、目标、基础伤害、倍率、技能 ID、元素类型、是否暴击、DOT、击退、标签。
- 生成 `DamageResult`：最终伤害、是否击杀、是否暴击、被抵抗数值、触发效果。
- 生成 `DamageSystem`：统一计算伤害并发布事件。
- 扩展 `IDamageable`：接收 `DamageInfo`，返回 `DamageResult`。
- 生成 `ElementType`、`DamageType`、`DamageTag` 枚举。

## 实现流程
1. 保留旧 `TakeDamage(float)` 兼容入口，但内部转换成 `DamageInfo`。
2. 将 Player、Enemy、SkillObject 的伤害调用逐步迁移。
3. 在 DamageSystem 中集中处理暴击、护甲、元素、Buff Modifier。
4. 通过 `OnDamageApplied` 触发飘字、音效、受击反馈。

## 计算顺序
基础伤害 -> 技能倍率 -> 攻击方增益 -> 防御方减免 -> 元素修正 -> 暴击 -> 最终伤害 -> DOT/附加效果。

## 验收标准
- 所有伤害来源都能追踪到来源对象和技能 ID。
- 暴击、元素、护甲和 Buff 对最终伤害有统一影响。
- UI 飘字不再由 Enemy 直接调用。
