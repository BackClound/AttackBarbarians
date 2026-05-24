# Config System

## 概述

ScriptableObject 驱动的配置系统：`ConfigManager` 在 Bootstrap 时加载 `ConfigDatabaseSO`，按 `configId` 建索引并校验。运行时通过 `*RuntimeData` 复制状态，**不修改 SO**，存档只保存 `configId` 与等级。

## 文件结构

```
Assets/Scripts/Config/
├── ConfigDataBase.cs
├── ConfigDatabaseSO.cs
├── ConfigModifierType.cs
├── StatModifierConfig.cs
├── StatBlockConfig.cs
├── ConfigStatBridge.cs          # 写入 Entity_Stats 的迁移适配器
├── Data/
│   ├── PlayerDataSO.cs
│   ├── EnemyDataSO.cs
│   ├── SkillDataSO.cs
│   ├── BuffDataSO.cs
│   ├── WaveDataSO.cs
│   ├── BossDataSO.cs
│   └── DropTableSO.cs
├── Runtime/
│   ├── StatRuntimeSnapshot.cs
│   ├── PlayerRuntimeData.cs
│   ├── EnemyRuntimeData.cs
│   ├── SkillRuntimeData.cs
│   ├── BuffRuntimeData.cs
│   └── ConfigRuntimeFactory.cs
└── Validation/
    ├── ConfigValidationResult.cs
    └── ConfigValidator.cs

Assets/Editor/Config/
└── ConfigDatabaseSOEditor.cs    # Inspector「Validate」按钮

Assets/Scripts/Managers/ConfigManager.cs
```

## 资产路径与命名规范

| 类型 | 菜单路径 | 推荐路径 | 命名示例 |
|------|----------|----------|----------|
| Game Config | Attack Barbarians → Config → Game Config | `Assets/Resources/Config/GameConfig.asset` | `GameConfig` |
| Config Database | … → Config Database | `Assets/Resources/Config/ConfigDatabase.asset` | `ConfigDatabase` |
| Player | … → Player Data | `Assets/Resources/Config/Player/` | `PlayerData_Default.asset` |
| Enemy | … → Enemy Data | `Assets/Resources/Config/Enemy/` | `EnemyData_Bat.asset` |
| Skill | … → Skill Data | `Assets/Resources/Config/Skill/` | `SkillData_Shoot.asset` |
| Buff | … → Buff Data | `Assets/Resources/Config/Buff/` | `BuffData_AttackUp.asset` |
| Wave | … → Wave Data | `Assets/Resources/Config/Wave/` | `WaveData_01.asset` |
| Boss | … → Boss Data | `Assets/Resources/Config/Boss/` | `BossData_xxx.asset` |
| Drop Table | … → Drop Table | `Assets/Resources/Config/Drop/` | `DropTable_Common.asset` |

**configId 约定**：小写 + 点分，如 `enemy.bat`、`skill.shoot`。常量见 `GameConstants.ConfigIds`。

## API 速查

| API | 说明 |
|-----|------|
| `ConfigManager.TryGetEnemy(id, out data)` | 按 ID 取 SO |
| `ConfigManager.CreateEnemyRuntime(id)` | 创建运行时副本 |
| `ConfigRuntimeFactory.CreateSkill(so, level)` | 从 SO 创建 SkillRuntimeData |
| `ConfigValidator.ValidateDatabase(db)` | 校验（启动时自动调用） |
| `ConfigStatBridge.ApplyToEntityStats(snapshot, stats)` | 将快照写入现有 `Entity_Stats` |
| `StatModifierConfig` + `ConfigModifierType` | Flat / PercentAdd / PercentMultiply / FinalMultiplier |
| `ConfigValidator.ApplyModifiers(base, mods, stat)` | 按类型合并修正 |

## 数据流

```
ConfigDatabaseSO (资产)
    → ConfigManager.Initialize：索引 + Validate
    → TryGet* / Create*Runtime
    → *RuntimeData（可变）
    → ConfigStatBridge（可选）→ Entity_Stats
    → Event / UI 读 RuntimeData
```

## 场景挂载

1. `GameSystems` 上已有 `ConfigManager`（与 `GameBootstrapper` 同物体或子物体）。
2. `GameConfig.asset` 中将 **Config Database** 指向 `ConfigDatabase.asset`（或留空由 Resources 加载 `Config/ConfigDatabase`）。
3. 运行后 Console 应出现 `[ConfigManager] 已加载配置：...` 与「配置校验通过」。

## 测试步骤

1. 打开含 `GameBootstrapper` 的可玩场景，确认 `ConfigManager` 已挂载。
2. 确认 `Assets/Resources/Config/ConfigDatabase.asset` 及示例子配置存在。
3. 运行 Play：无配置校验 Error；`Enable Runtime Logs` 开启时可见加载计数。
4. 在 Project 选中 `ConfigDatabase` → Inspector **Validate Config Database**，应通过。
5. （可选）临时脚本验证：
   ```csharp
   var cm = ServiceLocator.Get<ConfigManager>();
   var enemy = cm.CreateEnemyRuntime(GameConstants.ConfigIds.EnemyBat);
   Debug.Log(enemy.Stats.Get(StatType.MaxHp));
   ```

## 校验规则

- 空 `configId`、重复 ID、列表内空引用
- 负血量、非法等级、`MinCount > MaxCount`、掉落总权重 ≤ 0
- Wave/Boss 引用的 `enemyConfigId` 必须存在于 Database
- Buff 缺图标为 **Warning**

## 与旧逻辑边界

| 仍由旧逻辑负责 | 本模块已提供、待迁移 |
|----------------|----------------------|
| `Entity_Stats` Inspector 默认值 | `EnemyDataSO` / `PlayerDataSO` + `ConfigStatBridge` |
| `SkillShoot` 序列化字段 | `SkillDataSO` + `SkillRuntimeData` |
| `EnemyGenerateManager` 波次与 Prefab 数组 | `WaveDataSO` + `EnemyDataSO.Prefab/PoolKey` |
| Buff 管线未接 Modifier | `BuffDataSO` + `BuffRuntimeData` |

**未改动**：`Player`、`Enemy`、`SkillShoot`、`EnemyGenerateManager` 行为与 Prefab 引用保持不变；新配置为旁路，按需逐步接入。

## 后续任务

- `EnemyGenerateManager` 按 `WaveDataSO` 与 `enemyConfigId` 加权刷怪
- Buff System 消费 `StatModifierConfig` 与 `StatRuntimeSnapshot.ApplyModifiers`
- Save System 仅序列化 `configId` + level/stacks
- Boss 阶段切换读取 `BossDataSO.PhaseHpThresholds`
