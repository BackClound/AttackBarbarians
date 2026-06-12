# SDD-05 敌人与波次系统

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联 PRD | §7 敌人系统 |
| 架构层 | L2 Gameplay |
| 实现度 | ⚠️ 75% |

---

## 1. 系统概述

敌人生成、AI、属性缩放、波次推进与刷怪规则。

---

## 2. 核心类

| 类 | 路径 |
|----|------|
| `WaveManager` | `Managers/WaveManager.cs` |
| `EnemySpawnerManager` | `Enemy/EnemySpawnerManager.cs` |
| `WaveDataSO` | `Config/Data/WaveDataSO.cs` |
| `WaveSpawnSelector` | `Wave/WaveSpawnSelector.cs` |
| `Enemy` / `EnemyController` | `Enemy/` |
| `EnemyDataSO` | `Config/Data/EnemyDataSO.cs` |
| `SpecialEnemyController` | `SpecialEnemy/` |
| `EliteController` | `Elite/` |

---

## 3. 敌人 AI

```
Idle → (墙在范围) Attack / Move
Move → (近墙) Idle
Attack → 动画帧 → ExecuteWallAttack
任意 → Dead
```

---

## 4. 敌人类型（当前内容）

| 类型 | configId 示例 | 状态 |
|------|---------------|------|
| 普通 Bat ×5 | enemy.bat 等级档 | ✅ |
| 冲锋 | enemy.bat_charge | ✅ |
| 护盾 | enemy.bat_shield | ✅ |
| 分裂 | enemy.bat_split | ✅ |
| 召唤 | enemy.bat_summoner | ✅ |
| 远程 | enemy.bat_ranged | ✅ |
| 精英个体 | eliteSpawnChance | ✅ |
| 精英模式全局 | EliteModeConfigSO | ✅ |

---

## 5. 波次规则（WaveDataSO）

| 字段 | 含义 |
|------|------|
| waveDuration | 波次最长秒数 |
| spawnInterval | 刷怪间隔 |
| maxSpawnCount | 本波上限 |
| statScalePerWave | 每波属性成长 |
| hasBoss / bossSpawnAtElapsed | Boss 定时 |
| requireBossDefeatToComplete | 必须击杀 Boss |
| eliteSpawnChance | 精英概率 |
| specialSpawnChance | 特殊怪概率 |

### 完成条件（任一）

1. Boss 模式：Boss 已击败且场上无 Boss  
2. 超时：`waveElapsed >= waveDuration`  
3. 清场：刷满且 `AliveCount == 0`

---

## 6. 属性缩放

```
最终 = BaseStats × (1 + (wave-1) × statScalePerWave) × 精英倍率 × 地图倍率 × entry倍率
```

---

## 7. 实现状态分析

| 项 | 状态 |
|----|------|
| 波次推进逻辑 | ✅ |
| 权重选怪 | ✅ |
| 5 种特殊能力 | ✅ |
| 仅 1 份 Wave 资产 | ⚠️ 内容少 |
| WaveEnemyEntry.maxSpawnCount | ❌ 未 enforced |
| GDD 时间轴里程碑 | ❌ |
| 非 Bat 敌种 | ❌ 内容扩展 |

---

## 8. 测试要点

- 波次超时/清场均能完成  
- 精英模式全局倍率生效  
- 特殊怪能力按 tag 挂载  
