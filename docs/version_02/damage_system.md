# Damage System

统一伤害结算、事件驱动飘字与旧 `TakeDamage(float)` 兼容。

## 架构

| 类型 | 挂载 | 说明 |
|------|------|------|
| `DamageSystem` | `GameSystems` | `IGameSystem`，计算 + 写血 + 发布 `Damage.Applied` |
| `DamagePipeline` | 否 | 静态入口，Bootstrap 前回退旧逻辑 |
| `DamageInfo` / `DamageResult` | 否 | 请求与结算 Payload |
| `DamageCalculationSO` | Resources 资产 | 护甲/暴击/元素公式参数 |

## 计算顺序

基础伤害 → 技能倍率 → 攻击方元素附加 → 护甲减免 → 元素倍率（预留）→ 暴击 → 最终伤害。

`DamageTag.SkipCalculation`：旧 float 直伤，不掷暴击、不走护甲公式。

## 数据流

```
SkillObject / EnemyCombat
    → DamageInfo
    → DamagePipeline.Apply / DamageSystem.ApplyDamage
    → Entity_Health.ApplyResolvedDamage
    → GameEvents.RaiseDamageApplied（仅 Enemy Tag 目标）
    → DamageNumberController
```

玩家受伤走 `PlayerDamaged` / `PlayerHealthChanged`，不发布飘字事件。

## 配置

- 路径：`Assets/Resources/Config/Damage/DamageCalculation_Default.asset`
- Resources Key：`GameConstants.ResourcePaths.DamageCalculation`
- 可选：在 `DamageSystem` Inspector 拖入 SO，覆盖自动加载。

## 场景挂载

1. 在 `GameSystems` 上 Add Component `DamageSystem`（或由 `GameBootstrapper` 自动 AddComponent）。
2. 确认 `GameBootstrapper` 引导后 `ServiceLocator.TryGet<DamageSystem>()` 为 true。
3. 场景中保留 `DamageNumberController` 与 `PoolKeys.DamageNumber` 池（已有）。

## 测试步骤

1. 进入 `BattleScene`，运行游戏。
2. 玩家射击蝙蝠：敌人头顶飘字，Console 无 `[DamageSystem] 未找到配置`（或仅首次警告）。
3. 蝙蝠攻击城墙：玩家血条下降，墙体受击动画，**无**敌人飘字。
4. 击杀敌人：`Enemy.Killed` 事件仍触发，经验正常。
5. （可选）临时禁用 `DamageSystem` 组件：子弹伤害仍生效（`DamagePipeline` 回退），飘字仍通过回退路径发布。

## 已迁移 / 未迁移边界

| 已迁移 | 说明 |
|--------|------|
| `SkillObject_Base.DoDamage` | 使用 `DamageInfo` + `DamageSystem` 暴击 |
| `SkillObject_BulletSpawn` | 传 `GetBaseAttackDamage()` |
| `EnemyCombatManager` | `WallControlManager.TakeDamageFromEnemy` |
| `Enemy_Health` | 不再直接 `RaiseDamageApplied` |
| `BatEnemy.GetDamageValue` | 基础攻击力 |

| 保留旧逻辑 | 说明 |
|------------|------|
| `TakeDamage(float)` | 转 `DamageInfo.FromFloat` → `DamagePipeline.Apply` |
| `Entity_Stats.GetTotalDamage()` | 标记旧 API，内部仍可用 |
| `AttackInfo`（StatType/value） | 未接入元素映射，后续 Projectile/Skill 模块对接 |
| Buff 减伤 / 元素抗性 | 公式预留，未读 Buff 目标侧修正 |
| DOT / 击退 / 穿透 | 类型与 Tag 已定义，无运行时驱动 |

## 扩展点

- `DamageCalculationSO` 增加元素抗性曲线。
- `DamageSystem.Calculate` 读取目标 `StatRuntimeSnapshot` 减伤。
- `DamageTag` 驱动击退、连锁在 Projectile 模块实现。
- `AttackInfo` → `ElementType` 映射表放在 Skill 配置。
