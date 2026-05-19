# Attack Barbarians — 需求文档（PRD）& 功能开发说明

> 文档版本：v0.3  
> 文档类型：产品需求文档（PRD）+ 功能开发说明  
> 关联文档：[设计文档.md](设计文档.md)（GDD 与数值机制）、[IMPLEMENTATION.md](IMPLEMENTATION.md)（Unity 场景挂载与代码对照）

---

# 第一部分：需求文档（PRD）

## 1. 文档目的与范围

### 1.1 目的

定义 Attack Barbarians **一期** 可交付的功能范围、验收标准与非功能要求，供产品、程序、策划、QA 对齐开发与测试。

### 1.2 范围

| 包含 | 不包含 |
| --- | --- |
| 无限战斗、敌人/Boss、经验与 Buff、技能与基础属性养成 | **装备系统**（二期） |
| 体力、商城、钻石、抽奖、签到、离线、广告礼包 | 多人在线、服务器权威战斗 |
| 普通/精英模式 | 完整商业化 SDK 对接细节（另附接入文档） |

### 1.3 术语

| 术语 | 含义 |
| --- | --- |
| 升级事件 | 局内经验条满一次，等级 +1 |
| 整卡 | 结算或玩法直接获得的完整升级卡（非碎片） |
| 四维 | HP、ATK、移速、攻速（或项目等价字段） |

---

## 2. 用户故事

| ID | 角色 | 诉求 | 目的 | 优先级 |
| --- | --- | --- | --- | --- |
| US-01 | 玩家 | 经验同时来自时间和击杀 | 少打与多打都能成长 | P0 |
| US-02 | 玩家 | 每升 4 级能稳定选一次基础 Buff，且计数不因暂停清零 | 节奏可预期 | P0 |
| US-03 | 玩家 | 累计游玩足够时长后解锁新技能 | 长期目标清晰 | P0 |
| US-04 | 玩家 | 体力限制开局、可随时间恢复 | 控制节奏 | P0 |
| US-05 | 玩家 | 商店定时领取少量钻石 | 非付费也有钻石获取感 | P0 |
| US-06 | 玩家 | 抽奖界面能看到概率与保底说明 | 信任与合规 | P0 |
| US-07 | 玩家 | 打不过时多局能获得整卡提升战力 | 局外兜底 | P0 |
| US-08 | 玩家 | 离线最多 8 小时有收益 | 不登录也不完全落后 | P1 |
| US-09 | 玩家 | 每日签到获得升级卡 | 留存与习惯 | P1 |
| US-10 | 玩家 | 看广告获得随机礼包奖励 | 商业化与加速 | P1 |

---

## 3. 功能需求清单（一期）

| 编号 | 模块 | 需求描述 | 优先级 |
| --- | --- | --- | --- |
| FR-01 | 战斗核心 | 无限模式、刷怪、时间轴、暂停/恢复 | P0 |
| FR-02 | 敌人 | 四维随局内时间成长；敌种权重随时间解锁 | P0 |
| FR-03 | Boss | 5/10/15/20 分钟精英 Boss 生成；击杀统计与奖励 | P0 |
| FR-04 | 经验升级 | 时间 + 击杀经验；升级事件；`NeedExp(L)` 可配置 | P0 |
| FR-05 | 基础 Buff | 每 4 次升级事件触发三选一；暂停/切后台不重置计数 | P0 |
| FR-06 | 里程碑 | 技能向 Buff 与时间点/Boss 绑定（一期可先占位） | P0 |
| FR-07 | 技能养成 | 技能列表、局外等级、专卡/通用卡完全替代消耗 | P0 |
| FR-08 | 技能解锁 | 主路径：`total_play_seconds` 达阈值解锁 | P0 |
| FR-09 | 基础属性 | 局外四维等级；升级卡消耗随等级递增 | P0 |
| FR-10 | 结算 | 按时长与模式发整卡与货币；档位与上限可配 | P0 |
| FR-11 | 体力 | 开局扣体力；为 0 禁止开局；时间恢复 | P0 |
| FR-12 | 商城 | 金币/钻石商品；限购；12h 免费领取 5 钻 | P0 |
| FR-13 | 钻石 | 广告、玩法产出、商店免费领取三路可追踪 | P0 |
| FR-14 | 抽奖 | 分奖品概率/权重；公示；钻石/券消耗；保底建议 | P0 |
| FR-15 | 签到 | 1～6 日属性卡向；第 7 日技能卡向 | P1 |
| FR-16 | 离线巡逻 | 分档奖励；累计上限 8 小时 | P1 |
| FR-17 | 广告礼包 | 随机奖励；每日频控 | P1 |
| FR-18 | 模式 | 普通/精英；精英四维更高、结算更高 | P0 |

---

## 4. 验收标准（可测）

### 4.1 战斗与成长

| # | 验收项 | 通过条件 |
| --- | --- | --- |
| AC-01 | 经验双来源 | 同一经验条同时计入时间经验与击杀经验；升级产生一次升级事件 |
| AC-02 | Buff 计数 | `buffEventCounter` 每次升级 +1；=4 时必出基础属性三选一，触发后 -4 |
| AC-03 | 中断不重置 | 暂停、切后台不重置 `buffEventCounter` |
| AC-04 | 跨局计数 | 由 `buff_event_counter_persist` 写死；QA 按发布配置验收 |
| AC-05 | 击杀经验 | 不同敌人若配置不同 \(K_i\)，实际获得与表一致 |
| AC-06 | 精英四维 | 精英局敌人 HP/ATK/MS/AS = 普通同时间点 × `EliteModeMultipliers` |

### 4.2 养成与经济

| # | 验收项 | 通过条件 |
| --- | --- | --- |
| AC-07 | 技能解锁 | `total_play_seconds` 未达阈值不解锁；达到则解锁 |
| AC-08 | 通用技能卡 | 升级需 `N` 张时，可用 `N` 张通用或 `N` 张专属或任意组合凑满 `N` |
| AC-09 | 升级消耗递增 | 技能/属性等级升高时，下一级消耗卡数 ≥ 当前级（按公式表） |
| AC-10 | 结算整卡 | 结算主奖励为整卡；碎片若存在不得替代整卡主路径 |
| AC-11 | 免费钻石 | `now - last_claim < 12h` 不可领；满足后一次 +5（或配置值） |
| AC-12 | 钻石玩法产出 | 存在至少一种可追踪的非广告、非免费领取的钻石来源 |
| AC-13 | 抽奖公示 | UI 展示概率/权重与逻辑一致；钻石抽奖先扣费后发奖，失败回滚 |
| AC-14 | 体力 | 体力不足无法开局（编辑器绕过需文档标注）；恢复速率符合配置 |
| AC-15 | 离线 8h | 离线累计超过 8 小时仅按 8 小时计奖 |
| AC-16 | 签到 | 同日不可重复领；第 7 日奖励类型与配置一致 |

---

## 5. 边界、异常与非功能需求

### 5.1 异常处理

| 场景 | 预期行为 |
| --- | --- |
| 广告失败 | 不发奖；次数按 SDK 约定是否消耗 |
| 抽奖扣费后失败 | 回滚钻石/券 |
| 存档读写失败 | 使用默认档或提示；不静默丢档 |
| 本地时间回拨 | 离线/免费钻/体力：单调时钟或钳制（单机风险提示） |

### 5.2 非功能需求（NFR）

| 类别 | 要求 |
| --- | --- |
| 性能 | 同屏敌人上限；对象池；后期特效可分级 |
| 数据 | 钻石领取、抽奖、结算写档原子性 |
| 合规 | 概率公示；未成年人/隐私按发行地区 |
| 可维护 | 核心数值 **表驱动**（ScriptableObject / CSV） |

---

# 第二部分：功能开发说明

## 6. 系统架构与职责

### 6.1 逻辑模块（目标架构）

| 组件 / 系统 | 职责 |
| --- | --- |
| `RunSession` / `BattleRunController` | 单局生命周期、时间、模式、结束结算 |
| `TimeDirector` | 局内时间、里程碑、Boss 调度 |
| `DifficultyDirector` | 读取 `EnemyScaling4D`，输出全局倍率 |
| `SpawnDirector` / `EnemyGenerateManager` | 刷怪权重、间隔、同屏上限 |
| `ExperienceSystem` | 时间/击杀经验、`NeedExp`、升级事件 |
| `LevelUpEventBus` | `OnLevelUp` → `buffEventCounter` |
| `BuffOfferSystem` / `PlayerRunBuffState` | 满 4 次 Buff 选择与递减叠层 |
| `MilestoneSkillBuffSystem` | 时间点技能向强化（待完整 UI/效果） |
| `EnemyStatApplier` / `EnemyRuntimeScaling` | 基础 × 时间 × 精英 |
| `MetaGameService` | 存档、体力、货币、签到/离线/抽奖 API |
| `SkillProgressionService` | 技能等级、专卡/通用卡消耗 |
| `AttrProgressionService` | 四维等级、属性卡消耗 |
| `SkillUnlockService` | `total_play_seconds` 与解锁表 |
| `RewardCalculator` | 结算打包 |
| `StaminaService` | 扣减、恢复、开局校验 |
| `ShopService` | 购买、限购、免费钻石 |
| `GachaService` | 权重随机、保底、钻石直购 |
| `OfflinePatrolService` | 8h 封顶领取 |
| `SignInService` | 7 日周期 |
| `AdRewardService` | 广告回调、频控 |

### 6.2 工程已实现对照（v0.3 代码骨架）

| 设计模块 | 当前脚本 | 状态 |
| --- | --- | --- |
| 局外存档/体力/结算/经济 API | `MetaGameService`, `GameSaveData`, `GameIds` | 已实现 |
| 单局控制/经验/Boss 钩子 | `BattleRunController` | 已实现 |
| 敌人时间缩放 | `EnemyRuntimeScaling` | 已实现（需 Prefab 挂载） |
| 局内 Buff | `PlayerRunBuffState`, `RunBasicBuffCatalog` | 已实现（暂自动选一项） |
| 局外进局属性 | `MetaCombatStatsLoader` | 已实现 |
| 刷怪加压 | `EnemyGenerateManager` 改间隔 | 已实现 |
| 击杀/死亡结算 | `Enemy_Health`, `Player_Health` | 已实现 |
| 调试面板 | `MetaDevPanel`（F1） | 已实现 |
| Buff 三选一 UI | — | **未实现** |
| 里程碑技能 Buff | — | **占位** |
| 闪电/火雨等多技能战斗体 | — | **未实现** |
| 装备 | — | **二期** |

> 场景与 Prefab 挂载步骤见 **[IMPLEMENTATION.md](IMPLEMENTATION.md)**。

---

## 7. 关键事件与数据流

```
[每帧] BattleRunController.Update
    → AddExp(时间项)

[OnEnemyKilled] Enemy_Health
    → RegisterEnemyKill → AddExp(击杀项)

[Exp >= NeedExp]
    → Level++
    → OnLevelUp
    → buffEventCounter++
    → if counter == 4 → BuffOffer（三选一）→ PlayerRunBuffState

[Time milestone]
    → Spawn Boss（若配置 prefab）

[Player HP <= 0]
    → Die → EndRunPlayerDead
    → MetaGameService.ApplyRunSettlement
    → total_play_seconds += 本局时长
```

```
[开局] MetaGameService.TryConsumeStaminaForRun
    → MetaCombatStatsLoader.ApplyToPlayer

[商店] TryClaimFreeDiamond（12h / 5 钻）

[养成] TryUpgradeSkill / TryUpgradeAttr（整卡消耗校验）
```

---

## 8. 存档字段（最小集）

存档文件：`Application.persistentDataPath/attack_barbarians_save.json`

```text
saveVersion
gold, diamond, gachaTickets
staminaCurrent, staminaLastUtcSeconds
total_play_seconds
buffUpgradeCounterPersist, buffUpgradeCounterTowardsFour
skillLevels[]          // key=skillId, value=level
attrLevels[]           // key=attrId, value=level
skillCards[]           // key=card_skill_{skillId}
attrCards[]            // key=card_attr_{attrId}
universalSkillCards
lastFreeDiamondClaimUtcSeconds
signInCycleIndex, signInDayInCycle, lastSignInUtcDay
lastOfflineUtcSeconds
gachaPityCounter
```

---

## 9. 配置表清单（程序对接）

| 表名 | 用途 | 当前实现 |
| --- | --- | --- |
| `EnemyScaling4D` | 时间 → 敌人四维倍率 | `BattleRunController` 序列化字段 |
| `EliteModeMultipliers` | 精英倍率 | 同上 `eliteHp/Atk/Ms/As` |
| `ExpCurve` / `ExpIncome` | 升级经验、时间/击杀系数 | 同上 |
| `BuffPoolBasic` | Buff 选项与递减 | `PlayerRunBuffState` |
| `SkillUnlockByTotalTime` | 时长解锁 | `MetaGameService.IsSkillUnlockedByPlayTime` |
| `SkillLevelCost` | 技能升级卡数 | `MetaGameService.SkillUpgradeCardCost` |
| `AttrLevelCost` | 属性升级卡数 | `MetaGameService.AttrUpgradeCardCost` |
| `RewardByDuration` | 结算档位 | `ApplyRunSettlement` 内硬编码（待迁表） |
| `ShopCatalog` / `FreeDiamondConfig` | 商品与 12h/5 钻 | `MetaGameService` 常量 |
| `GachaTable` / `PityConfig` | 抽奖 | `RollGachaWithDiamondCost` + Dev 示例表 |
| `StaminaConfig` | 体力 | `MetaGameService` 常量 |
| `OfflinePatrolTable` | 离线 | `TryClaimOfflinePatrol` |
| `SignInRewards` | 签到 | `TryDailySignIn` |

**后续建议**：将硬编码迁为 `ScriptableObject`，版本号写入 `saveVersion`。

---

## 10. 测试建议

### 10.1 单元测试

- `NeedExp(L)` 单调递增  
- `buffEventCounter`：3→4 触发；连升 8 级触发 2 次  
- 免费钻石冷却边界（11h59m / 12h）  
- 通用卡 + 专卡混合凑满 `N`

### 10.2 集成测试

- 整局推进至 10 分钟：经验等级、敌人倍率、刷怪间隔  
- 精英模式倍率 vs 普通  
- 死亡后存档：金币/卡/时长增加

### 10.3 存档测试

- 杀进程重启：字段不丢  
- `saveVersion` 升级：缺字段默认值

### 10.4 手动验收（编辑器）

- 挂载 `BattleRunController`、`PlayerRunBuffState`、`EnemyRuntimeScaling`  
- `MetaDevPanel` F1：免费钻、签到、离线、抽奖

---

## 11. 技能 ID 与解锁阈值（代码常量，可改表）

| skillId | 说明 | 解锁累计秒数 |
| --- | --- | --- |
| `skill_shoot` | 基础射击 | 0（默认） |
| `skill_lightning` | 闪电 | 600 |
| `skill_thunder_fall` | 落雷 | 1800 |
| `skill_fire_rain` | 火雨 | 3600 |
| `skill_water_slow` | 水波纹减速 | 7200 |
| `skill_fire_ring` | 火环禁锢 | 10800 |

属性 ID：`attr_hp`, `attr_damage`, `attr_move_speed`, `attr_attack_speed`

---

## 12. 文档索引

| 文档 | 内容 |
| --- | --- |
| [设计文档.md](设计文档.md) | GDD、玩法循环、经济规则、数值公式 |
| [IMPLEMENTATION.md](IMPLEMENTATION.md) | Unity 挂载、已实现/未实现边界 |
| `GDD_PRD_游戏机制与数值设计.md` | 历史合并稿（可与设计文档同步维护） |
| `xxx.md` | 历史合并稿（建议以本文档与设计文档为准） |

---

**文件路径**：`AttackBarbarians/需求文档.md`
