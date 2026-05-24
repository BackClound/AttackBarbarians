# More Skills System

## 概述

在基础射击（`SkillType.Shoot`）之外，实现 **闪电、落雷、火雨、水浪、冰霜、恢复** 六类自动战斗技能。数据由 `SkillDataSO` 驱动，释放逻辑由 `ISkillEffect` 实现，局外按累计游玩时长解锁，局内通过升级三选一获得 Buff 或当场解锁。

## 架构

| 组件 | 挂载 | 职责 |
|------|------|------|
| `SkillManager` | Player | 冷却、自动释放、Buff 聚合 |
| `SkillEffectFactory` + `*SkillEffect` | 无 | 各技能伤害/控制逻辑 |
| `SkillUnlockService` | GameSystems | 时长解锁、存档、开局同步 |
| `SkillUnlockTableSO` | Resources | 解锁阈值配置 |
| `SkillCastVfxPlayer` | 无 | 闪电链 LineRenderer 特效 |
| `EnemyStatusController` | Enemy Prefab | 麻痹、冰冻、减速 |

## 数据流

```
SkillUnlockTableSO + SaveData.statistics.totalPlayTimeSeconds
  → SkillUnlockService.RefreshMetaUnlocks
  → SaveData.skillLevels
  → SkillUnlockService.ApplyUnlocksToPlayerSkillManager
  → SkillManager.UnlockSkill

UpgradeOptionSO (SkillUnlock / SkillBuff)
  → UpgradeApplicator
  → SkillManager.ApplySkillBuff / UnlockSkill

SkillManager.Update (AutoCast)
  → ISkillEffect.TryAutoCast
  → DamagePipeline + EnemyStatusController
  → GameEvents.SkillUsed
```

## 元进度解锁阈值（默认）

| configId | 累计秒数 |
|----------|----------|
| `skill.shoot` | 0（默认已解锁） |
| `skill.lightning` | 600 |
| `skill.heal` | 900 |
| `skill.thunder` | 1800 |
| `skill.fire_rain` | 3600 |
| `skill.ice` | 5400 |
| `skill.water_wave` | 7200 |

配置资产：`Resources/Config/Skill/SkillUnlockTable_Default.asset`（菜单 **Attack Barbarians → Config → Create More Skills Assets** 生成）。

## 事件 Key

| Key | 说明 |
|-----|------|
| `Skill.Used` | 技能释放（已有） |
| `Skill.LevelUp` | 技能等级变化 |
| `Skill.Unlocked` | 元进度或局内首次解锁 |

## 场景挂载

1. **GameSystems**：`GameBootstrapper` 会自动解析/创建 `SkillUnlockService`。
2. **Player**：`PlayerSkillManager`、`SkillManager`（已有）。
3. **Enemy Prefab**：`EnemyStatusController`。

## 测试步骤

1. 菜单 **Create More Skills Assets**，确认 `ConfigDatabase` 含解锁表与升级项。
2. Play 战斗场景，确认射击正常。
3. Player → `SkillManager` → **Debug/Unlock All Skills**，观察闪电/落雷/火雨等自动释放。
4. 修改存档 `statistics.totalPlayTimeSeconds >= 600`，重开一局，应自动解锁闪电（`Skill.Unlocked` 事件）。
5. 局内升级：仅当元进度已解锁的技能会出现在 `SkillUnlock` 选项；`SkillBuff` 需对应技能已在 `SkillManager` 解锁。

## API 摘要

```csharp
ServiceLocator.Get<SkillUnlockService>().IsMetaUnlocked(GameConstants.ConfigIds.SkillThunder);
skillManager.ApplySkillBuff(SkillBuffKind.LightningChainTargets, 3);
skillManager.UnlockSkill(GameConstants.ConfigIds.SkillFireRain, 1);
```

## 未迁移 / 后续

| 项 | 说明 |
|----|------|
| 落雷/火雨 Prefab VFX | 仍为范围查询 + 伤害，无落点预警动画 |
| `SkillAreaZone` | 落雷持续圈未进对象池 |
| 技能栏 UI | 需订阅 `Skill.Unlocked` 刷新锁定态 |
| 详细 Buff 池 | 需扩充 `BuffDataSO` 与 `RewardPoolSO` 条目 |

## 相关文档

- [skill_buff_system.md](skill_buff_system.md) — 全技能 Buff 表
- [prompt_skill_system.md](prompt_skill_system.md) — 技能系统总需求
