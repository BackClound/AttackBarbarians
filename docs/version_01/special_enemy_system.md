# Special Enemy System

## 概述

特殊敌人在现有 `Enemy` / `IEnemyAbility` / `WaveManager` 之上旁路接入，与 Boss、Elite 并列：Boss 负责阶段与波次 Boss 战，Elite 负责数值倍率，**Special Enemy 负责机制型能力**（冲锋、护盾、分裂、召唤、远程）。

## 数据流

```
WaveDataSO.specialSpawnChance + specialEnemyConfigIds
  → WaveManager.TrySpawnBonusSpecial
  → EnemySpawnerManager.TrySpawnSpecialEnemy
  → EnemyController.SetupSpecialEnemyMechanics
  → SpecialEnemyAbilityFactory.EnsureAbilities
  → Enemy*Ability (IEnemyAbility)
  → GameEvents (SpecialEnemy.Spawned / SpecialEnemy.AbilityUsed)
```

也可在 `WaveDataSO.enemyEntries` 中直接配置带 `abilityTags` 的敌人 configId，生成时同样挂载能力。

## 核心类型

| 类型 | 职责 |
|------|------|
| `SpecialEnemyAbilityDataSO` | 能力冷却、倍率、护盾 HP、召唤/分裂目标 |
| `EnemyAbilityBase` | 能力基类：冷却、配置解析、事件发布 |
| `EnemyChargeAbility` 等 | 五种机制实现 |
| `SpecialEnemyAbilityFactory` | 按 `EnemyDataSO.abilityTags` 自动 `AddComponent` |
| `SpecialEnemyController` | 特殊怪标记、额外经验、`SpecialEnemy.Spawned` |
| `EnemyDamageShield` | 护盾吸收伤害（`Enemy_Health` 结算前） |
| `WaveSpecialEnemySelector` | 从波次特殊怪池随机 configId |

## 事件 Key

| Key | Payload |
|-----|---------|
| `SpecialEnemy.Spawned` | `SpecialEnemySpawnedEventArgs` |
| `SpecialEnemy.AbilityUsed` | `SpecialEnemyAbilityUsedEventArgs` |

订阅示例：`SpecialEnemyHudPresenter`；Audio 使用 `audio.sfx.special_enemy_spawn` / `audio.sfx.special_enemy_ability`。

## 配置资产

| 路径 | configId |
|------|----------|
| `Resources/Config/SpecialEnemy/Ability/SpecialEnemyAbility_Charge.asset` | `special_ability.charge` |
| `Resources/Config/SpecialEnemy/Ability/SpecialEnemyAbility_Shield.asset` | `special_ability.shield` |
| `Resources/Config/SpecialEnemy/Ability/SpecialEnemyAbility_Split.asset` | `special_ability.split` |
| `Resources/Config/SpecialEnemy/Ability/SpecialEnemyAbility_Summon.asset` | `special_ability.summon` |
| `Resources/Config/SpecialEnemy/Ability/SpecialEnemyAbility_Ranged.asset` | `special_ability.ranged` |
| `Resources/Config/Enemy/Special/EnemyData_Bat_*.asset` | `enemy.bat_charge` 等 |

须在 `ConfigDatabase.asset` 的 `enemies` 与 `specialEnemyAbilities` 列表中登记。`WaveData_01` 已设置 `specialSpawnChance: 0.12` 与五种特殊怪池。

## 场景 / Prefab 挂载

| 组件 | 挂载位置 |
|------|----------|
| `SpecialEnemyHudPresenter` | 战斗 UI Canvas（可选） |
| `Enemy*Ability` | **无需**手动挂载；`SpecialEnemyAbilityFactory` 生成时添加 |
| `SpecialEnemyController` | 可选；缺失时生成特殊怪自动添加 |

## 测试步骤

1. 打开含 `GameBootstrapper` + `WaveManager` 的战斗场景，Play。
2. 波次进行中约每 8~10 次普通刷怪可能额外出现特殊怪（12% 概率）；Console 出现 `[SpecialEnemyHud] 特殊敌人: enemy.bat_*`。
3. **冲锋怪**：贴墙后周期性向下冲刺。
4. **护盾怪**：周期获得可吸收伤害的护盾（观察血量扣减变慢）。
5. **召唤怪**：周期召唤额外 `enemy.bat`。
6. **分裂怪**：击杀后额外生成 2 只弱化蝙蝠。
7. **远程怪**：未贴墙时对墙体多段远程伤害。
8. 击杀特殊怪经验 = 基础 `experienceReward` + `specialBonusExperience`。

## 未迁移边界

- 敌人专用投射物 Prefab / 碰撞层未实现；远程能力当前为对墙体的逻辑伤害 + `ExecuteWallAttack`。
- 特殊怪独立血条/头像 UI 待 UI 系统接事件扩展。
- `abilityBindings` 可覆盖默认 `special_ability.*` 映射，但 Prefab 上预挂能力组件仍支持（与工厂去重：同类型仅一个组件）。

## 扩展点

- 新能力：扩展 `EnemyAbilityTag`、新增 `EnemyXxxAbility` + `SpecialEnemyAbilityFactory` 分支 + SO 资产。
- 新特殊怪：复制 `EnemyData_Bat_*`，设置 `abilityTags` 与数值，登记 Database 与 `WaveData.specialEnemyConfigIds`。
- 波次条目：在 `enemyEntries` 中加入特殊怪 configId 与时间窗，无需依赖 `specialSpawnChance`。
