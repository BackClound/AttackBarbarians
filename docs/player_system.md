# Player System

## 概述

玩家防守核心：`PlayerController` 协调配置、运行时属性与目标扫描；保留现有 `Player` 状态机（Idle / Shoot / Dead）与 `SkillShoot` 攻击链路。

## 数据流

```
PlayerDataSO + SaveData.permanentUpgrades + BuffRuntimeData
    → PlayerRuntimeStats
    → ConfigStatBridge → Entity_Stats
    → PlayerController / PlayerCombat / SkillShoot
    → GameEvents（血量、攻击、技能、属性）
```

## 新增脚本

| 脚本 | 挂载 |
|------|------|
| `PlayerController` | Player 根物体（与 `Player` 同物体） |
| `PlayerHealthBarView` | 可选：血条 Slider 物体 |
| `PlayerRuntimeStats` | 否（由 Controller 持有） |
| `PlayerTargetScanner` | 否（由 Controller 持有） |

## 配置

- `Assets/Resources/Config/Player/PlayerData_Default.asset`：`attackRadius`、`targetPolicy`（0 最近 / 1 最低血 / 2 距墙最近 / 3 Boss 优先）。
- `PlayerController` Inspector：`enemyLayer`、`scanOrigin`（默认同物体）、`playerConfigId`（默认 `player.default`）。

## 事件 Key

- `Player.HealthChanged` / `Player.Damaged` / `Player.Died`
- `Player.StatsChanged`（`PlayerStatsChangedEventArgs`）
- `Player.AttackStarted`（`PlayerAttackEventArgs`）
- `Player.SkillCast`（skillId 字符串）

## API 入口

- `PlayerController.ApplyModifier` / `ApplyBuff`：Buff / Upgrade 接入。
- `PlayerController.ScanCombatTargets` / `CopyCombatTargetsTo`：Combat 与 Skill 共用目标列表。
- `PlayerRuntimeStats.ApplyModifier`：底层属性修正。

## 测试步骤

1. 场景 Player 上添加 `PlayerController`，配置 Enemy Layer 与扫描原点。
2. Play：确认 `ConfigManager` 已 Bootstrap，Console 无 PlayerData 缺失错误。
3. 生成敌人进入射程：`SkillShoot` 应进入 Shoot 并发射；血条可通过事件刷新（`PlayerHealthBarView` 或 `Player_Health` 内 Slider）。
4. 修改 `PlayerData_Default` 的 `attackRadius` / `targetPolicy`，运行观察选敌优先级变化。
5. 敌人击杀玩家或墙受伤：应触发 `Player.Died` 与 GameOver。

## 未迁移边界

- `SkillShoot` 仍使用自有 `bulletWaveList` 与 `Instantiate` 弹道（对象池迁移属 Projectile 阶段）。
- 多技能（闪电、落雷、火雨等）由 `SkillManager` + `ISkillEffect` 自动释放；元进度解锁见 `more_skills_system.md`。
- `PlayerCombat.FixedUpdate` 中旧扫描逻辑仍注释；实战由 `SkillShoot.Update` 驱动。
- Boss 通过 `Enemy.isBoss` 标记，无独立 Boss 子类。
