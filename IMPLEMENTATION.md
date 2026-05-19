# AttackBarbarians — 功能实现说明（对接 GDD/PRD v0.3）

本仓库已落地 **可运行的核心骨架**，与现有 `Player` / `Enemy` / `SkillShoot` / `EnemyGenerateManager` 对接。以下为引擎内需要完成的 **场景与 Prefab 挂载**（代码无法自动改 `.unity` 二进制）。

## 1. 自动启动的局外服务

`MetaGameService` 通过 `RuntimeInitializeOnLoadMethod` 在进场景前创建 **DontDestroyOnLoad** 对象；存档路径：`Application.persistentDataPath/attack_barbarians_save.json`。

- **编辑器**：默认 `bypassStaminaInEditor = true`，体力不足仍可开战（可在 Inspector 关掉以验收体力逻辑）。

## 2. 战斗场景必挂组件

在 **BattleScene**（或你的主战斗场景）创建空物体 **`BattleBootstrap`**，挂载：

| 组件 | 作用 |
| --- | --- |
| `BattleRunController` | 局内时间、精英开关、经验曲线、敌人成长公式、Boss 时间点、结算 |
|（可选）`BattleRunHUD` | 绑定 Legacy `Text` 显示经验 / 时间 / 击杀 |

### Boss（可选）

- `BattleRunController`：`bossPrefab`、`bossSpawnPoint`。  
- Boss Prefab 根节点加 **`BossEnemyMarker`**（用于 `BossKillCount`/钻石奖励）。  
- Boss 需带 **`Enemy_Health`** + **`Entity_Stats`** + **`EnemyRuntimeScaling`**（与普通怪一致）。

## 3. Player Prefab

在 **Player** 根节点增加：

- **`PlayerRunBuffState`** — 局内四维 Buff 叠层与递减。  
- **`MetaDevPanel`**（可选）— 运行时 **F1** 打开：免费钻、签到、离线、抽奖测试。

`MetaCombatStatsLoader` 会在开局把 **局外四维等级 + 基础射击技能等级** 乘到 `Entity_Stats` 的 base 值上。

## 4. Enemy Prefab

每个可生成敌人 Prefab 上增加 **`EnemyRuntimeScaling`**（与 `Entity_Stats`、`Enemy_Health` 同物体即可）。  
生成时会按当前 `BattleRunController` 局内时间应用 **HP/伤害/移速/攻速** 倍率（精英模式再乘 `elite*`）。

`BatEnemy.GetDamageValue()` 已改为读取 **`Entity_Stats.GetTotalDamage()`**，与成长表一致。

## 5. 已实现与未实现边界

| 已实现 | 未实现 / 需后续 |
| --- | --- |
| 存档、体力、结算、累计时长、技能解锁判定、免费钻 12h、签到/离线/抽奖 API + Dev 面板 | 正式 UI、向僵尸开炮式选 Buff 弹窗（当前为 **随机三选一之一** 自动应用） |
| 经验时间+击杀、每 4 级基础 Buff（计数可跨局.persist） | 里程碑 **技能效果 Buff**（5/10/15/20）仅留了 Boss 生成钩子 |
| 敌人四维成长、精英倍率、刷怪加压 | **闪电/火雨等技能**仍为设计占位，局内只强化基础射击相关数值链路 |
| 通用技能卡完全替代专卡、升级消耗公式 | 装备系统（二期） |

## 6. 关键脚本路径

- `Assets/Scripts/Meta/` — `MetaGameService`, `GameSaveData`, `GameIds`, `MetaDevPanel`  
- `Assets/Scripts/BattleRun/` — `BattleRunController`, `PlayerRunBuffState`, `EnemyRuntimeScaling`, `MetaCombatStatsLoader`, `BattleRunHUD`  

更完整机制与公式见 **[设计文档.md](设计文档.md)**；需求与开发说明见 **[需求文档.md](需求文档.md)**。数值表可逐步从代码中的 `[SerializeField]` 迁到 `ScriptableObject`。
