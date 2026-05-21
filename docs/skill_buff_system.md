# Skill System & Buff System

数据驱动技能 + 可叠加 Buff 管线，兼容既有 `SkillShoot` 动画射击流程。

## 架构

| 组件 | 挂载 | 职责 |
|------|------|------|
| `SkillManager` | Player | 解锁技能、冷却、自动释放、Buff Profile 聚合 |
| `BuffManager` | Player | Buff SO / `SkillBuffKind` 统一入口 |
| `PlayerSkillManager` | Player | 暴露 `sKillShoot`、`SkillManager`、`BuffManager` |
| `ISkillEffect` + `SkillEffectFactory` | 无 | 各技能释放逻辑 |
| `SkillBuffProfile` / `SkillBuffCatalog` | 无 | 技能 Buff 数值表 |
| `EnemyStatusController` | Enemy Prefab | 麻痹、冰冻、减速 |
| `ProjectileRuntimeOverrides` | 无 | 穿透/弹射/分裂/冰爆 |

## 技能列表

### 1. 射击（Shoot）`SkillType.Shoot`
- **释放**：`SkillManager` 冷却自动释放 → `ShootSkillEffect.TryAutoCast` → `ShootProjectileCaster`（与闪电/冰霜同链路；不再依赖射击动画攻击帧）
- **基础**：单发、单弹道，直线投射物 + `DamagePipeline`
- **Buff**
  | Buff | Kind | Tier 效果 |
  |------|------|-----------|
  | 双/三/四弹道 | `ShootTrajectoryLines` | 2 / 3 / 4 条扇形弹道 |
  | 每次双/三发 | `ShootVolleyCount` | 2 / 3 发/次 |
  | 穿透增益 | `ShootPierce` | +1 / +2 / +3 穿透 |
  | 弹射 | `ShootBounce` | 1 / 2 次弹射 |
  | 击中分裂 | `ShootSplitOnHit` | 1 / 2 次分裂子弹 |

### 2. 闪电（Lightning）`SkillType.Lightning`
- **释放**：自动冷却，从玩家射向最近敌人，链式命中
- **元素**：`ElementType.Lightning`
- **Buff**
  | Buff | Kind | Tier |
  |------|------|------|
  | 2/3/4 条闪电 | `LightningBoltCount` | 并行多道 |
  | 连接 2~5 目标 | `LightningChainTargets` | 链式伤害 |
  | 末端爆炸 | `LightningEndExplosion` | 范围额外伤害 |
  | 麻痹 | `LightningStun` | 短暂 `EnemyStatusController.ApplyStun` |
- **备注**：特效颜色随 Buff 加深变紫（待 VFX 接入）

### 3. 落雷（Thunder）`SkillType.Thunder`
- **释放**：随机落点 AoE（以场内敌人为锚点）
- **Buff**
  | Buff | Kind |
  |------|------|
  | 落雷数量 2~5 | `ThunderStrikeCount` |
  | 范围 +30%~+130% | `ThunderRadius` |
  | 范围内全体麻痹 | `ThunderStunAll` |
  | 麻痹时长 +20%/+50% | `ThunderStunDuration` |
  | 持续爆炸圈 | `ThunderPersistentZone` + 时长 tier |

### 4. 火雨（FireRain）`SkillType.FireRain`
- **释放**：随机区域多段 Tick 伤害
- **Buff**：范围/持续时间缩放；击杀连锁附近 2/3 敌 (`FireRainChainOnKill`)

### 5. 水浪（WaterWave）`SkillType.WaterWave`
- **释放**：朝向主目标的方向波，2~3 道
- **Buff**：波次数量、减速强度与时长、体积

### 6. 冰霜（Ice）`SkillType.Ice`
- **释放**：扇形冰霜投射物，路径穿透 + 命中冰冻
- **Buff**：弹道/齐射、冰冻时长、体积、半程爆炸 + 爆炸范围 tier

### 7. 恢复（Heal）`SkillType.Heal`
- **被动 Tick**：每秒 1% 最大生命（`HealRegenPerSecond`）
- **Buff**：最大生命%、每分钟 10%、每 3 分钟峰值 +10%、一次性满血
- **说明**：当前墙体伤害仍汇总到 `Player_Health`，治疗同时惠及玩家（墙代理）

### 8. 通用增益（可无限叠加）
| Kind | 作用 |
|------|------|
| `GlobalAttackSpeed` | `StatType.AttackSpeedMulti` +10%/tier |
| `GlobalBaseDamage` | `StatType.Damage` +10%/tier |
| `GlobalCritChance` | `StatType.CritChance` +10%/tier |
| `GlobalCritDamage` | `StatType.CritPower` +10%/tier |
| `GlobalCooldownReduction` | 技能冷却 ×(1-10%/tier)，下限 `SkillBuffProfile.MinCooldownSeconds` |

## 数据流

```
Upgrade / BuffManager.ApplyBuff(BuffDataSO)
    → SkillManager.ApplySkillBuff / PlayerController.ApplyBuff
    → SkillBuffProfile + PlayerRuntimeStats
    → ISkillEffect.TryAutoCast / SkillShoot 动画
    → DamageInfo → DamagePipeline
    → EnemyStatusController（控制类）
    → GameEvents.SkillUsed / BuffChanged
```

## 配置资产

路径：`Assets/Resources/Config/Skill/`

| configId | 资产 |
|----------|------|
| `skill.shoot` | SkillData_Shoot.asset |
| `skill.lightning` | SkillData_Lightning.asset |
| `skill.thunder` | SkillData_Thunder.asset |
| `skill.fire_rain` | SkillData_FireRain.asset |
| `skill.water_wave` | SkillData_WaterWave.asset |
| `skill.ice` | SkillData_Ice.asset |
| `skill.heal` | SkillData_Heal.asset |

将上述资产加入 `ConfigDatabase.asset` 的 `skills` 列表后，`ConfigManager.TryGetSkill` 方可加载。

`BuffDataSO` 可勾选 **Has Skill Buff**，指定 `SkillBuffKind` + `SkillBuffTier`。

## 场景挂载

1. **Player** 上确保有：`Player`、`PlayerController`、`PlayerSkillManager`（自动 Add `SkillManager`、`BuffManager`）
2. **Enemy Prefab** 添加 `EnemyStatusController`（推荐）
3. **GameSystems** 保持 `DamageSystem`、`ProjectileManager`、`ConfigManager`

## 测试步骤

1. 运行战斗场景，确认射击仍正常。
2. 选中 Player → `SkillManager` 上下文菜单 **Debug/Unlock All Skills**。
3. Inspector 或运行时调用：
   ```csharp
   player.skillManager.ApplySkillBuff(SkillBuffKind.ShootTrajectoryLines, 2);
   player.skillManager.ApplySkillBuff(SkillBuffKind.LightningChainTargets, 3);
   ```
4. 观察自动闪电/落雷/火雨等与敌人麻痹、冰冻、减速。
5. 应用 `GlobalCooldownReduction` 多档，确认冷却不低于 0.15s。

## 代码 API 摘要

```csharp
// 解锁
skillManager.UnlockSkill(GameConstants.ConfigIds.SkillLightning, 1);

// Buff（代码）
skillManager.ApplySkillBuff(SkillBuffKind.FireRainRadius, 2);

// Buff（配置）
buffManager.ApplyBuff(buffDataSO, stacks);
```

## 未迁移 / 后续

| 项 | 说明 |
|----|------|
| 闪电/落雷 VFX | 当前仅伤害 + 状态，无链状 LineRenderer |
| `SkillAreaZone` | 落雷持续圈为轻量 MonoBehaviour，未进对象池 |
| UpgradeManager 三选一 | 需将 Buff 池条目指向 `BuffDataSO` 或调用 `ApplySkillBuff` |
| 墙体独立血量 | 仍经 `Player_Health` 代理 |
| 投射物弹射物理 | `BounceCount` 已写入覆盖，反弹方向待完善 |

## 扩展新技能

1. `SkillType` 增加枚举值（注意序列化顺序）。
2. 新建 `SkillData_*.asset`。
3. 实现 `ISkillEffect`，在 `SkillEffectFactory` 注册。
4. 在 `SkillBuffKind` / `SkillBuffCatalog` 增加 Buff 映射。
