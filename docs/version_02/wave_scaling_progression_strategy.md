# 局内波次成长与经验策略设计

| 项 | 内容 |
|----|------|
| 版本 | v1.0 |
| 关联文档 | `Design_document.md` §5–6、`SDD-02`、`SDD-05`、`SDD-07` |
| 状态 | 策略定稿 + 实现方案（待开发） |

---

## 1. 设计目标

在竖屏无限防守 + Roguelike 框架下，建立**随波次同步推进**的三条成长曲线：

1. **玩家升级门槛**：等级越高，升级所需经验越高（**固定 per 等级，与波次无关**）。
2. **敌人战斗强度**：生命、伤害、暴击、移速、攻速按不同斜率增长，移速/攻速有硬上限。
3. **经验供给与刷怪压力**：击杀经验、刷怪间隔、单波数量随波次调整，使「难度上升」与「成长供给」保持可控张力。

### 1.1 主流肉鸽参考

| 游戏 | 玩家经验 | 敌人强度 | 刷怪压力 |
|------|----------|----------|----------|
| **Vampire Survivors** | 升级需求随等级幂次上升；后期单级需求陡增 | 随时间/阶段 HP、伤害、数量同步抬升 | 同屏上限 + 生成频率双轨 |
| **Brotato** | 波次结束选属性；经验与击杀数强相关 | 每波独立乘区，HP/ATK 斜率高于移速 | 波次间敌人数量阶梯式增长 |
| **20 Minutes Till Dawn** | 等级曲线前期快、中后期放缓 | 分钟轴分段倍率；精英/Boss 额外乘区 | 时间窗内密度递增 |
| **土豆兄弟类塔防** | 击杀 = 主要经验源；升级选 Buff | 波次系数表驱动，分属性独立配置 | `interval ↓` + `count ↑` 组合 |

**本项目取舍**：

- 以**波次序号 `W`**（`WaveManager.CurrentWaveIndex`）为主轴驱动敌人强度与**经验供给**；**局内等级 `L`** 独立控制升级门槛。
- 保留现有精英模式（`EliteModeConfigSO`）、地图修正（`MapWaveModifierConfig`）、条目倍率（`WaveEnemyEntry`）作为**叠加层**，不替换。
- 经验来源以**击杀**为主（已实现）；时间经验作为可选扩展（GDD 已有，代码待接）。

---

## 2. 符号与变量

| 符号 | 含义 | 代码来源 |
|------|------|----------|
| `W` | 当前波次序号，≥ 1 | `WaveManager.CurrentWaveIndex` |
| `L` | 玩家当前等级，≥ 1 | `PlayerRuntimeData.CurrentLevel` |
| `E_base` | 敌人配置基础经验 | `EnemyDataSO.ExperienceReward` |
| `HP₀, ATK₀, MS₀, AS₀, CC₀, CP₀` | 敌人基础属性 | `EnemyDataSO.BaseStats` |
| `D` | 难度模式系数（普通=1，精英≈1.05） | `RunDifficultyContext` / 设置 |
| `M_map` | 地图敌人属性倍率 | `MapRuntimeContext.EnemyStatMultiplier` |

---

## 3. 玩家升级策略

### 3.1 设计原则

1. **前期快、中后期稳**：前 3–5 级快速触发 Roguelike 三选一，建立构筑手感。
2. **门槛与波次解耦**：同等级在任何波次的升级所需经验相同；波次只提高击杀经验与每波刷怪量。
3. **升级后经验归零**：单次击杀最多升一级，确认三选一后经验条从 0 重新累计（`PlayerController.GrantExperience`）。
4. **与 Buff 节奏对齐**：GDD 目标「每 4 次升级触发基础属性池」；10 分钟休闲局目标等级 `L ∈ [6, 10]`。

### 3.2 升级所需经验公式

**等级曲线**（来自 `Design_document.md` §6.2，**不含波次项**）：

\[
NeedExp(L) = NeedExp_{base}(L) \cdot D_{exp}
\]

\[
NeedExp_{base}(L) = E_0 \cdot L^{g} \cdot \exp\bigl(\lambda \cdot \max(0,\, L - L_0)\bigr)
\]

| 参数 | 建议默认 | 说明 |
|------|----------|------|
| `E₀` | 80 | 1 级升 2 级基础经验 |
| `g` | 1.35 | 等级幂次，控制中后期斜率 |
| `λ` | 0.02 | 超过 `L₀` 后的指数放缓项 |
| `L₀` | 8 | 指数项起始等级 |
| `D_exp` | 1.0（普通）/ 1.05（精英） | 开局难度档位对门槛的微调 |

> **设计意图**：升级门槛由等级曲线主导；后期升级节奏通过**随波次增加的经验供给**（击杀奖励 × 刷怪数量）调节，而非抬高同级门槛。

### 3.3 参考数值表（D_exp = 1，任意波次相同）

| 等级 L | NeedExp |
|--------|---------|
| 1→2 | 80 |
| 3→4 | 353 |
| 5→6 | 703 |
| 8→9 | 1,325 |
| 10→11 | 1,864 |
| 15→16 | 3,580 |

### 3.4 经验来源策略

| 来源 | 公式 | 默认 | 状态 |
|------|------|------|------|
| 击杀 | 见 §5.1 | 主来源 | ✅ `PlayerExperienceService` |
| 时间 | `dExp/dt = B_time · D` | `B_time = 6` | ⚠️ GDD 有，代码未接 |
| 玩家加成 | `实际获得 = 基础 × (1 + ExperienceGain)` | StatType | ✅ `PlayerController.GrantExperience` |

**时间经验接入建议**：仅在 `GameState.Playing` 且未暂停时累加；数值取击杀经验的 15%–25% 等效每分钟，避免喧宾夺主。

### 3.5 升级事件与 Roguelike 选单

| 触发 | 行为 | 状态 |
|------|------|------|
| 击杀经验满 `NeedExp(L)` | `GameEvents.RaisePlayerLevelUp` → 局内三选一 | ✅ |
| 每累计 4 次升级 | 强制基础属性 Buff 池，`counter -= 4` | ❌ GDD 有，待实现 |
| 波次完成 | `WaveCompleted` → 升级选单 | ✅ |

---

## 4. 敌人属性随波次增幅策略

### 4.1 设计原则

1. **分属性独立斜率**：HP、伤害增长最快；移速、攻速最慢且带上限（防止不可交互）。
2. **暴击克制增长**：暴击率/暴击伤害为**加法或低倍率乘法**，设绝对上限，避免后期被秒。
3. **与现有管线兼容**：在 `EnemyStatScaling` 层拆分，最终仍写入 `StatRuntimeSnapshot` → `Entity_Stats`。
4. **叠加顺序不变**：

```
最终属性 = BaseStats
         × M_wave(W)           // 分属性波次倍率
         × M_entry             // WaveEnemyEntry 条目倍率
         × M_elite             // EliteMode / 精英个体
         × M_map               // 地图修正
```

### 4.2 波次属性倍率公式

以波次序号 `W` 替代 GDD 中时间 `t`（分钟）的等价物：

\[
M_{attr}(W) = \mathrm{clamp}\bigl(1 + a_{attr} \cdot (W - 1)^{p_{attr}},\ 1,\ M^{max}_{attr}\bigr)
\]

对无上限属性（HP、伤害、暴击伤害倍率）：

\[
M_{attr}(W) = 1 + a_{attr} \cdot (W - 1)^{p_{attr}}
\]

### 4.3 建议默认系数

| 属性 | `a` | `p` | `M_max` | W=5 | W=10 | W=20 |
|------|-----|-----|---------|-----|------|------|
| **MaxHp** | 0.10 | 1.30 | — | ×1.35 | ×1.85 | ×2.95 |
| **Damage**（含 contactDamage） | 0.08 | 1.25 | — | ×1.28 | ×1.68 | ×2.55 |
| **MoveSpeed** | 0.025 | 1.15 | **1.45** | ×1.10 | ×1.22 | ×1.38 |
| **AttackSpeed** | 0.03 | 1.20 | **1.35** | ×1.12 | ×1.25 | ×1.35（封顶） |
| **CritChance** | 0.004 | 1.10 | **+0.15**（绝对加值上限） | +1.6% | +3.6% | +7.6%→封顶 |
| **CritPower** | 0.02 | 1.10 | **+0.40**（绝对加值上限） | +0.08 | +0.18 | +0.38 |

> 计算示例（W=10，Bat HP₀=30）：`HP = 30 × 1.85 ≈ 56`；伤害 `10 × 1.68 ≈ 17`。

### 4.4 与当前实现的差异

| 项 | 当前 | 目标 |
|----|------|------|
| 倍率形状 | 线性 `1 + (W-1)×0.08` | 幂函数 + 分属性系数 |
| 缩放属性 | HP/MS/AS/Damage/Armor 统一 | 增加 CritChance/CritPower；contactDamage 同步 |
| 攻速上限 | 无 | `M_max = 1.35` |
| 移速上限 | 无 | `M_max = 1.45` |

### 4.5 精英与 Boss 叠加（保持现有）

| 层 | 倍率来源 | 说明 |
|----|----------|------|
| 精英模式全局 | `EliteModeConfigSO` | HP×1.5, ATK×1.3, MS×1.1, AS×1.15 |
| 精英个体 | `eliteEnemyBonusMultiplier` | 在波次倍率之上再 ×1.25 |
| Boss | `BossDataSO` 基础 + 波次倍率 | 不改公式，仅换用分属性 `M_wave` |

---

## 5. 敌人经验、刷怪速度与数量策略

### 5.1 击杀经验随波次

**原则**：经验增速**慢于** HP 增速，使「杀一只怪」相对变难，但「一波总经验」仍随密度上升而增加，形成紧张但不崩坏的成长曲线。

\[
Exp_{kill}(W) = \mathrm{round}\Bigl(E_{base} \cdot M_{exp}(W) \cdot w_{type} \cdot D_{exp\_gain}\Bigr)
\]

\[
M_{exp}(W) = 1 + a_{exp} \cdot (W - 1)^{p_{exp}}
\]

| 参数 | 建议默认 | 说明 |
|------|----------|------|
| `a_exp` | 0.06 | 约为 HP 系数 `a_hp` 的 60% |
| `p_exp` | 1.20 | 与伤害接近 |
| `w_type` | 敌种权重 | 普通=1，特殊=1.5，精英个体=2.0，Boss 额外加 `BonusExperience` |
| `D_exp_gain` | 与 §3 一致 | 精英模式可 ×1.05 |

**敌种基础经验建议（E_base）**：

| 类型 | E_base | w_type | 备注 |
|------|--------|--------|------|
| 普通蝙蝠 | 5 | 1.0 | 当前默认 |
| 特殊能力怪 | 8 | 1.5 | 含 `specialBonusExperience` |
| 精英个体 | 5 | 2.0 | 基础按普通，权重补偿难度 |
| Boss | 20 + Bonus 50 | — | `BossDataSO` |

**参考表（普通怪 E_base=5）**：

| W | M_exp | 单怪经验 | 若本波击杀 18 只 |
|---|-------|----------|------------------|
| 1 | ×1.00 | 5 | 90 |
| 5 | ×1.22 | 6 | 110 |
| 10 | ×1.52 | 8 | 144 |
| 20 | ×2.08 | 10 | 180 |

### 5.2 刷怪间隔随波次

\[
Interval(W) = \max\Bigl(I_{min},\ I_0 \cdot \rho^{\,W-1} \cdot M^{map}_{interval}\Bigr)
\]

| 参数 | 建议默认 | 说明 |
|------|----------|------|
| `I₀` | 1.50 s | 首波间隔（当前 `WaveData_01`） |
| `ρ` | 0.965 | 每波间隔 ×0.965（约 -3.5%） |
| `I_min` | 0.35 s | 硬下限，防止帧级刷怪 |

| W | Interval | 对比 W=1 |
|---|----------|----------|
| 1 | 1.50 s | — |
| 5 | 1.28 s | -15% |
| 10 | 1.07 s | -29% |
| 15 | 0.89 s | -41% |
| 20 | 0.74 s | -51% |
| 30+ | **0.35 s** | 触底 |

### 5.3 单波最大刷怪数

\[
Count(W) = \min\Bigl(C_{max},\ \mathrm{round}\bigl(C_0 + \Delta_c \cdot (W - 1)\bigr) \cdot M^{map}_{count}\Bigr)
\]

| 参数 | 建议默认 | 说明 |
|------|----------|------|
| `C₀` | 20 | 首波上限（当前配置） |
| `Δ_c` | 3 | 每波 +3 只 |
| `C_max` | 80 | 性能与可读性上限 |

| W | Count |
|---|-------|
| 1 | 20 |
| 5 | 32 |
| 10 | 47 |
| 20 | 77 |
| 22+ | **80**（封顶） |

### 5.4 波次时长

\[
Duration(W) = \mathrm{clamp}\bigl(D_0 + \lfloor (W-1)/5 \rfloor \cdot \Delta_d,\ D_0,\ D_{max}\bigr)
\]

| 参数 | 默认 |
|------|------|
| `D₀` | 30 s |
| `Δ_d` | +5 s / 每 5 波 |
| `D_max` | 45 s |

使后期单波可容纳更多刷怪 tick，同时避免单波过长拖节奏。

### 5.5 精英 / 特殊怪概率（可选）

\[
P_{elite}(W) = \mathrm{clamp}\bigl(P_0 + 0.004 \cdot (W-1),\ 0,\ 0.25\bigr)
\]

\[
P_{special}(W) = \mathrm{clamp}\bigl(P_0 + 0.005 \cdot (W-1),\ 0,\ 0.30\bigr)
\]

当前 `WaveData_01`：`eliteSpawnChance=0.08`，`specialSpawnChance=0.12`，可作为 `P₀`。

### 5.6 经验—难度平衡校验（设计约束）

目标：**W=10 时， diligent 玩家（击杀率 85%）应处于 L≈7–9**。

估算（每波净经验 ≈ `Count(W) × Exp_kill(W) × 0.85`）：

| W | 预估单波经验 | 累计（约） | 对应等级区间 |
|---|-------------|-----------|-------------|
| 1–3 | 90–130/波 | ~330 | L 1→4 |
| 4–6 | 130–160/波 | ~780 | L 4→6 |
| 7–10 | 160–200/波 | ~1,500 | L 6→9 |

若实测偏离 ±2 级，优先调 `a_exp`（供给）或 `E₀`/`g`（需求），**不要**先动 HP 系数。

---

## 6. 难度模式与地图修正

在以上公式最终结果上叠加（已实现，保持不变）：

| 修正 | 作用域 | 字段 |
|------|--------|------|
| 精英模式 | 敌人属性 | `EliteModeConfigSO` |
| 地图 | 属性 / 间隔 / 数量 | `MapWaveModifierConfig` |
| 游戏难度设置 | 局末金币；可扩展至 `D`、`D_exp` | `SaveData.settings.gameDifficulty` |

建议扩展 `RunDifficultyContext`：

| difficulty | `D`（敌人） | `D_exp`（门槛） | `D_exp_gain`（获得） |
|------------|-------------|-----------------|----------------------|
| 0 简单 | 0.90 | 0.95 | 1.10 |
| 1 普通 | 1.00 | 1.00 | 1.00 |
| 2 困难 | 1.12 | 1.05 | 0.95 |

---

## 7. 实现方案

### 7.1 新增配置资产

#### `WaveProgressionConfigSO`

路径：`Assets/Resources/Config/Wave/WaveProgression_Default.asset`

集中存放 §3–§5 全部曲线参数，供 `ConfigDatabaseSO` 引用。

```csharp
// 建议字段分组
[Header("Player Level")]
float expBase; float expGrowthPower; float expLambda; int expLambdaStartLevel;

[Header("Enemy Stats Per Wave")]
WaveStatCurve hpCurve, damageCurve, moveSpeedCurve, attackSpeedCurve;
WaveStatCurve critChanceCurve, critPowerCurve; // additive mode flag

[Header("Enemy Experience")]
float expWaveCoeff; float expWavePower;

[Header("Spawn")]
float spawnIntervalBase; float spawnIntervalDecay; float spawnIntervalMin;
int spawnCountBase; int spawnCountPerWave; int spawnCountMax;
float waveDurationBase; int waveDurationStep; float waveDurationMax;

[Header("Optional")]
float eliteChanceBase; float eliteChancePerWave; float eliteChanceMax;
```

`WaveStatCurve` 结构：`coefficient`, `power`, `maxMultiplier`（0 表示无上限）, `additiveCap`（暴击类）。

#### `PlayerLevelCurveSO`（可选拆分）

若希望与 `PlayerDataSO` 解耦，可将 §3.2 参数迁入此 SO；`PlayerDataSO` 仅保留 `startLevel`。

### 7.2 新增运行时服务

#### `WaveProgressionCalculator`（静态纯函数）

```csharp
public static class WaveProgressionCalculator
{
    public static float GetNeedExperience(int level, WaveProgressionConfigSO cfg, float difficultyExpMult);
    public static float GetEnemyStatMultiplier(StatType stat, int wave, WaveProgressionConfigSO cfg);
    public static int GetKillExperience(int baseExp, int wave, float typeWeight, WaveProgressionConfigSO cfg, float difficultyGainMult);
    public static float GetSpawnInterval(int wave, WaveProgressionConfigSO cfg, float mapMult);
    public static int GetMaxSpawnCount(int wave, WaveProgressionConfigSO cfg, float mapMult);
    public static float GetWaveDuration(int wave, WaveProgressionConfigSO cfg);
}
```

**依赖边界**：仅依赖 `Config` 层与 `StatType`，不引用 UI / Upgrade。

#### `RunProgressionContext`（局内单例上下文）

```csharp
public static class RunProgressionContext
{
    public static int CurrentWave { get; set; }  // WaveManager 在 StartWave 时写入
    public static WaveProgressionConfigSO Config { get; set; }  // Bootstrap 注入
}
```

### 7.3 改动点清单

| 优先级 | 文件 | 改动 |
|--------|------|------|
| P0 | `EnemyStatScaling.cs` | `ApplyMultiplier` 改为 `ApplyWaveScaling(snapshot, wave, cfg)`，分属性调用 |
| P0 | `PlayerController.cs` | `GetNeedExperienceForCurrentLevel` 使用 `GetNeedExperience(L, …)`（与波次无关） |
| P0 | `EnemyController.cs` | `GetExperienceReward` 乘以 `GetKillExperience` 波次倍率 |
| P0 | `WaveManager.cs` | `GetEffectiveSpawnInterval/MaxSpawnCount` 改用计算器；`StartWave` 更新 `RunProgressionContext` |
| P1 | `GameplayHudTopPanel.cs` | 经验条分母改为动态 `NeedExp(L)` |
| P1 | `WaveDataSO.cs` | 保留 `statScalePerWave` 作**兼容覆写**；新逻辑优先读 `WaveProgressionConfigSO` |
| P1 | `ConfigDatabaseSO` | 注册 `waveProgression` 条目 |
| P2 | `PlayerExperienceService` 或新 `PlayerPassiveExpTicker` | 接入 `B_time` 时间经验 |
| P2 | `UpgradeManager` | 实现 `buffEventCounter` 四循环 |
| P2 | Editor 工具 | `Tools/generate_wave_progression_preview.py` 输出 CSV 曲线表 |

### 7.4 数据流（目标态）

```
WaveProgressionConfigSO
    ↓ Bootstrap → RunProgressionContext
WaveManager.StartWave(W)
    → RunProgressionContext.CurrentWave = W
    → Interval/Count/Duration 取自 Calculator
    → EnemySpawnerManager.TrySpawnEnemy(multiplier)
         → EnemyStatScaling.ApplyWaveScaling(wave)
EnemyKilled
    → GetKillExperience(wave) → PlayerExperienceService
    → GrantExperience → NeedExp(L) 判定升级
```

### 7.5 迁移与兼容

1. **Phase 1**：新增 `WaveProgressionConfigSO` + Calculator，Feature Flag `useWaveProgressionV2` 默认 `false`。
2. **Phase 2**：Flag 打开后走新曲线；对比遥测/手动测试后调参。
3. **Phase 3**：废弃 `WaveDataSO.statScalePerWave` 线性逻辑，字段标记 `[Obsolete]`。
4. 多 `WaveData` 资产方案：可为每 5 波配置不同敌人组合，**数值曲线仍走全局 `WaveProgressionConfigSO`**，避免逐波手工填表。

### 7.6 测试用例

| # | 场景 | 期望 |
|---|------|------|
| T1 | W=1 击杀 16 只普通蝙蝠 | L 升至 2–3，触发至少 1 次升级选单 |
| T2 | W=10 模拟全清每波 | L ∈ [7, 10] |
| T3 | W=20 敌人移速 | ≤ 基础 ×1.45 × 地图 × 精英 |
| T4 | W=20 敌人攻速 | ≤ 基础 ×1.35 × 地图 × 精英 |
| T5 | W=30 刷怪间隔 | = `I_min`（0.35s） |
| T6 | 精英模式 W=10 | 同屏 TTK 提升约 40%–60%，但等级仅低 0–1 级 |
| T7 | 经验溢出 | 一次击杀升 2 级时，事件触发 2 次 `PlayerLevelUp` |
| T8 | 暂停 / 切后台 | 时间经验不计（若已接入） |

---

## 8. 联调参数速查（首版推荐）

```yaml
# Player
E0: 80
g: 1.35
lambda: 0.02
L0: 8
a_need: 0.015
p_need: 1.2

# Enemy Stats
hp:      { a: 0.10, p: 1.30, max: 0 }
damage:  { a: 0.08, p: 1.25, max: 0 }
move:    { a: 0.025, p: 1.15, max: 1.45 }
attack:  { a: 0.03, p: 1.20, max: 1.35 }
critChance: { a: 0.004, p: 1.10, additiveCap: 0.15 }
critPower:  { a: 0.02, p: 1.10, additiveCap: 0.40 }

# Experience
a_exp: 0.06
p_exp: 1.20
B_time: 6  # 可选

# Spawn
I0: 1.5
rho: 0.965
I_min: 0.35
C0: 20
delta_c: 3
C_max: 80
D0: 30
delta_d: 5
D_max: 45
```

---

## 9. 当前实现差距摘要

| 能力 | 文档策略 | 代码现状 |
|------|----------|----------|
| 升级需求随等级曲线 | `NeedExp(L)` | ✅ `WaveProgressionCalculator.GetNeedExperience` |
| 升级需求与波次解耦 | 门槛不含 `W` | ✅ 已实现 |
| 敌人分属性波次缩放 | 幂函数 + 上限 | ✅ V2 `WaveProgressionConfigSO` |
| 敌人暴击波次成长 | 加法 + 上限 | ✅ V2 曲线 |
| 击杀经验随波次 | `M_exp(W)` | ✅ `GetKillExperience` |
| 刷怪间隔/数量随波次 | 指数 / 线性 | ✅ V2 Calculator + WaveManager |
| 时间经验 | `B_time` | ⚠️ 配置存在，未接入 |
| 四循环基础 Buff | counter=4 | ⚠️ GDD 有，待实现 |

---

## 10. 相关文件索引

| 类型 | 路径 |
|------|------|
| 玩家经验 | `Assets/Scripts/Player/PlayerController.cs` |
| 经验服务 | `Assets/Scripts/Player/PlayerExperienceService.cs` |
| 敌人缩放 | `Assets/Scripts/Enemy/EnemyController.cs`（`EnemyStatScaling`） |
| 波次驱动 | `Assets/Scripts/Managers/WaveManager.cs` |
| 波次配置 | `Assets/Scripts/Config/Data/WaveDataSO.cs` |
| 敌人配置 | `Assets/Scripts/Config/Data/EnemyDataSO.cs` |
| GDD 原文 | `Design_document.md` §5–6 |
| SDD | `docs/sdd/SDD-02-玩家系统.md`、`docs/sdd/SDD-05-敌人与波次系统.md` |

---

*文档结束 — 实现时请以此为准调参，并在 `Tools/` 下补充曲线预览脚本辅助平衡。*
