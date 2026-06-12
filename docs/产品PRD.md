# Attack Barbarians — 游戏产品需求文档（PRD）

> **文档版本**：v2.0  
> **文档类型**：小型 PRD（约 20 章，细节见 `docs/sdd/` 系统设计文档）  
> **关联**：[项目需求文档](项目需求文档.md) · [SDD 索引](sdd/README.md) · [Design_document.md](../Design_document.md)

---

# 1. 项目概述

## 1.1 基础信息

| 项目 | 内容 |
| ---- | ---- |
| 游戏名称 | Attack Barbarians（攻击野蛮人） |
| 项目代号 | AttackBarbarians |
| 游戏类型 | 2D 竖屏无限防守 Roguelike 单机 |
| 开发平台 | iOS / Android（竖屏） |
| 开发引擎 | Unity 6000.3 LTS |
| 开发语言 | C# |
| 开发周期 | 一期 MVP（进行中） |
| 开发团队 | 独立/小团队（程序 + 策划 + 美术） |

---

## 1.2 游戏简介

### 一句话描述

> 玩家在城墙后方操控野蛮人自动射击，通过局内三选一构筑不同技能流派，在无限波次怪物进攻中守卫城墙，并以局外升级卡与天赋实现长期成长。

---

### 游戏定位

* **目标市场**：全球移动端休闲 / 中度 Roguelike 市场，优先华语区
* **目标用户**：通勤碎片时间玩家、构筑爱好者、成长型收集玩家
* **核心体验**：爽快自动战斗 · 可见构筑成长 · 10 分钟休闲门槛 · 局外整卡兜底
* **参考竞品**：《向僵尸开炮》（波次+构筑）、《佣兵的战争》（局外成长）—— 仅借鉴体验结构

---

## 1.3 核心卖点

### Feature 01 — 竖屏无限防守 + 自动射击

玩家无需复杂操作，野蛮人在射程内自动锁定敌人（优先靠近城墙目标）并射击；敌人从上方压境攻击城墙，形成「守线」紧张感。

**实现状态**：✅

---

### Feature 02 — Roguelike 三选一构筑

每波结束或升级时从奖励池抽取 3 个 Buff/技能选项，玩家择一强化本局 Build；支持互斥组、叠层上限与多元素技能联动。

**实现状态**：✅ 主流程；⚠️ GDD「四循环属性 Buff」计数器未实现

---

### Feature 03 — 局外整卡成长 + Meta 运营

多局结算、商城宝箱、签到、抽奖、离线/在线奖励发放升级卡；体力控制节奏，广告与免费钻提供非付费路径。

**实现状态**：⚠️ 发卡完整，卡消耗养成待完成

---

# 2. 核心玩法循环

## 2.1 Gameplay Loop

```
MainScene 大厅
    ↓ 消耗体力
进入 BattleScene 战斗
    ↓ 自动射击 + 技能
击杀怪物 → 获得经验
    ↓ 升级
升级选择 Buff（三选一）
    ↓ 强化 Build
波次推进 → 挑战 Boss（配置驱动）
    ↓ 本局结束
获得奖励（金币/钻石/升级卡）
    ↓ 局外成长（天赋/升级卡/商城）
下一局
```

---

## 2.2 战斗目标

### 胜利条件

* 一期为**无限生存模式**，无传统「通关胜利」
* 隐性目标：存活更久、推进更高波次、击败 Boss、获得更高结算档位

### 失败条件

* 玩家生命值归零（敌人攻击城墙，伤害传导至玩家）
* 触发 `GameOver` → 局末结算 → 返回 MainScene

**实现状态**：✅

---

## 2.3 核心资源

| 资源 | 用途 | 实现 |
| ---- | ---- | ---- |
| 金币（废料金） | 商城购买、天赋升级 | ✅ |
| 钻石（量子钻） | 宝箱、抽奖、高级商品 | ✅ |
| 经验 | 局内升级、触发三选一 | ✅ |
| 体力 | 限制每局开局次数 | ✅ |
| 广告券 | 兑换金币/钻石、广告补给 | ✅ |
| 升级卡 | 局外技能/属性成长（消耗待实现） | ⚠️ |

---

# 3. 玩家系统

> 详细设计见 [SDD-02-玩家系统](sdd/SDD-02-玩家系统.md)

## 3.1 玩家属性

| 属性 | 描述 | 配置来源 | 状态 |
| ---- | ---- | -------- | ---- |
| HP (MaxHp) | 最大生命 | `PlayerDataSO.baseStats` | ✅ |
| Attack (Damage) | 基础攻击 | 同上 | ✅ |
| Defense (Armor) | 护甲 | 同上 | ✅ |
| CritRate | 暴击率 | 同上 | ✅ |
| CritDamage (CritPower) | 暴击伤害加成 | 同上 | ✅ |
| AttackSpeed | 攻速 | 同上 | ✅ |
| MoveSpeed | 移速 | 同上 | ✅ |
| Range (AttackRadius) | 攻击/扫描半径 | `PlayerDataSO.attackRadius` | ✅ |
| 元素伤害 | 火/冰/雷附加 | `FireDamage` 等 Stat | ✅ |
| Luck | — | 未单独实现 | ❌ |

局内最终属性 = 基础 + 局内 Buff + 天赋 + 装备 + 局外属性等级（后两者待完整接入）。

---

## 3.2 玩家状态

### 状态机（表现层）

| 状态 | 说明 | 状态 |
| ---- | ---- | ---- |
| Idle | 待机 | ✅ |
| Shoot | 射击动画（逻辑已迁移至 SkillShoot） | ⚠️ |
| Dead | 死亡 | ✅ |

**说明**：实际战斗逻辑由 `AutoAttackController` + `SkillManager` 驱动，状态机主要负责动画。

### 目标选择策略

`Nearest` · `LowestHealth` · `NearestToWall`（默认）· `BossFirst`

---

## 3.3 成长方式

### 局内成长

* 击杀获得经验 → 升级 → 三选一 Buff
* 波次完成 → 三选一 Buff
* 局内 Buff 仅本局有效，写入 `RunProgressData`

**状态**：✅

### 局外成长

* 天赋永久属性（`TalentManager`）
* 装备穿戴强化（`EquipmentManager`，二期 UI）
* 升级卡提升技能/属性等级（**消耗逻辑待实现**）
* 累计游玩时长解锁新技能（`SkillUnlockService`）

**状态**：⚠️

---

# 4. 战斗系统

> 详细设计见 [SDD-03-战斗与伤害系统](sdd/SDD-03-战斗与伤害系统.md)

## 4.1 战斗流程

```
索敌（PlayerTargetScanner）
    ↓
锁定目标（TargetPolicy）
    ↓
发射攻击（AutoAttack / SkillEffect）
    ↓
命中检测（CollisionManager / Trigger）
    ↓
伤害计算（DamagePipeline → DamageSystem）
    ↓
死亡判定（Entity_Health）
```

---

## 4.2 伤害公式

```text
最终伤害 =
  基础伤害 × 技能倍率
  + 元素附加（攻击方元素 Stat × ElementStatScale）
  × 护甲减免：100 / (100 + effectiveArmor)    // effectiveArmor = 目标护甲 - 攻击方破甲
  × 元素倍率（当前恒为 1，抗性未实现）
  × 暴击倍率（1 + CritPower，默认 +50%）
```

**配置**：`DamageCalculationSO`（`Assets/Resources/Config/Damage/`）

---

## 4.3 战斗机制

| 机制 | 描述 | 状态 |
| ---- | ---- | ---- |
| 普攻 | 射击技能自动释放，弹道池化 | ✅ |
| 技能 | 7 元素技能自动/冷却释放 | ⚠️ |
| DOT | `DamageType.Dot`，可配置是否暴击 | ✅ |
| 暴击 | 掷骰 `CritChance` 或强制 `Critical` 类型 | ✅ |
| 穿透 | `DamageTag.Pierce` + 射击 Buff | ⚠️ |
| 弹射 | 射击 `Bounce` Buff | ⚠️ |
| 分裂 | 射击 `SplitOnHit` Buff | ⚠️ |

---

# 5. 技能系统

> 详细设计见 [SDD-04-技能系统](sdd/SDD-04-技能系统.md)

## 5.1 技能分类

| 分类 | 本项目对应 | 状态 |
| ---- | ---------- | ---- |
| 主武器 | Shoot（射击） | ✅ |
| 主动技能 | Lightning / Thunder / FireRain / WaterWave / Ice | ⚠️ |
| 被动/恢复 | Heal | ⚠️ |
| 召唤技能 | 敌人 Summon 能力；玩家侧无独立召唤技能 | — |

---

## 5.2 技能品质

局内升级选项与升级卡支持稀有度；技能本体按 `SkillDataSO` + Buff 叠层区分强度。

| 品质 | 颜色（UI） | 状态 |
| ---- | ---------- | ---- |
| 普通 | `UiRarityVisual` 配置 | ⚠️ 占位 |
| 稀有 | 同上 | ⚠️ |
| 史诗 | 同上 | ⚠️ |
| 传说 | 同上 | ⚠️ |

---

## 5.3 技能升级规则

* **局内**：通过三选一 `SkillBuff` / `SkillLevelUp` 叠层
* **局外**：消耗升级卡提升 `skillLevels`（**待实现**）
* 解锁：累计游玩时长门槛（见下表）

| 技能 | 解锁累计时长 |
| ---- | ------------ |
| Shoot | 0（默认） |
| Lightning | 10 min |
| Heal | 15 min |
| Thunder | 30 min |
| FireRain | 60 min |
| Ice | 90 min |
| WaterWave | 120 min |

---

## 5.4 技能数据结构

| 字段 | 类型 | 代码字段 |
| ---- | ---- | -------- |
| SkillID | string | `configId`（如 `skill.shoot`） |
| Name | string | `displayName` |
| Damage | float | `baseStats.Damage` + 技能倍率 |
| Cooldown | float | `SkillDataSO.cooldown` |
| ProjectileCount | int | Buff `VolleyCount` 等 |
| Description | string | `description` |

---

# 6. Roguelike Buff 系统

> 详细设计见 [SDD-07-Roguelike升级Buff系统](sdd/SDD-07-Roguelike升级Buff系统.md)

## 6.1 Buff 池

由 `RewardPoolSO` + `UpgradeOptionSO` 配置；效果类型包括 `StatBuff`、`SkillBuff`、`SkillUnlock`、`ResourceGold` 等。

**配置状态**：⚠️ `ConfigDatabase` 中 `rewardPools` / `upgradeOptions` 可能为空，需执行 Editor Bootstrap。

---

## 6.2 Buff 品质

与升级选项 / BuffDataSO 稀有度字段绑定；UI 用 `UiRarityVisual` 展示。

---

## 6.3 Buff 刷新规则

升级 / 波次完成时：

* 随机 3 选 1（`pool.ChoiceCount`）
* 权重抽取（`UpgradeManager.TryPickWeighted`）
* 互斥组排他（`ExcludeMutualGroup`）
* 叠层达上限的选项过滤
* **GDD 四循环**：每 4 次升级强制基础属性池 — **❌ 未实现**

---

## 6.4 Build 流派设计（目标）

| 流派 | 组成（示例） | 状态 |
| ---- | ------------ | ---- |
| 暴击流 | 暴击率 + 暴击伤害 + 射击双发 | ⚠️ Buff 枚举已有 |
| 闪电链流 | 闪电多道 + 连锁 + 爆炸 | ⚠️ |
| 火雨持续流 | 火雨范围 + 持续 + 击杀传递 | ⚠️ |
| 减速控制流 | 水浪 + 冰霜冰冻 | ⚠️ |

---

# 7. 敌人系统

> 详细设计见 [SDD-05-敌人与波次系统](sdd/SDD-05-敌人与波次系统.md)

## 7.1 敌人分类

| 类型 | 特点 | 状态 |
| ---- | ---- | ---- |
| 普通怪 | Bat 系 5 档，权重刷怪 | ✅ |
| 精英怪 | `eliteSpawnChance` 额外刷怪 + 属性倍率 | ✅ |
| 特殊怪 | 冲锋/护盾/分裂/召唤/远程 | ✅ |
| Boss | 独立配置，阶段与技能 | ⚠️ |

---

## 7.2 AI 逻辑

### 普通怪状态

* Idle → Move → Attack → Dead

敌人自上方生成，向下移动，进入攻击范围后攻击城墙。

### Boss 状态

* 阶段 Phase1 / Phase2（血量比或时间触发）
* 技能：Charge / Summon / AreaAttack / Shield / Barrage

**状态**：⚠️ 技能配置未完全串联

---

## 7.3 刷怪规则

| 时间/波次 | 内容 | 状态 |
| --------- | ---- | ---- |
| 波次内间隔刷怪 | `spawnInterval` 驱动 | ✅ |
| 波次超时 | `waveDuration` 完成本波 | ✅ |
| Boss @20s | `WaveData_01` 配置 `bossSpawnAtElapsed` | ⚠️ 仅 1 份 Wave 资产 |
| 精英概率刷怪 | 每次普怪后 `eliteSpawnChance` | ✅ |
| GDD 5/10/15/20 分钟里程碑 | 时间轴 Director | ❌ |

---

# 8. Boss 系统

> 详细设计见 [SDD-06-Boss系统](sdd/SDD-06-Boss系统.md)

## Boss 01 — Bat King（bat_king）

| 项 | 内容 |
| ---- | ---- |
| 名称 | Bat King |
| 配置 | `BossData_Bat.asset` |
| 出场 | 波次内 `bossSpawnAtElapsed` 秒 |
| 技能 | AreaSlam、Summon（资产存在但未挂到 BossData） |
| 阶段 | 支持血量比 / 时间切换 |

**实现状态**：⚠️ 框架完整，内容与技能联动待补

## Boss 02+

一期目标至少 1 个可玩 Boss；更多 Boss 为内容扩展项。

---

# 9. 装备系统

> 详细设计见 [SDD-09-装备与天赋系统](sdd/SDD-09-装备与天赋系统.md)  
> **一期范围**：后端实现，**商城与成长页不展示**（二期）

## 装备部位

* Weapon · Helmet(Chest) · Boots · Ring（代码：`EquipmentSlot`）

当前配置 4 件战士套装 + 套装加成 `set.warrior`。

## 品质

Common / Rare / Epic / Legendary（`UiRarityVisual`）

## 强化规则

`EquipmentManager` 支持等级强化，消耗金币，修正合并至 `PlayerRuntimeStats`。

**状态**：✅ 后端 · ❌ UI · 🔜 二期主交付

---

# 10. 局外成长系统

> 详细设计见 [SDD-08-局外成长与升级卡](sdd/SDD-08-局外成长与升级卡.md)

| 系统 | 描述 | 状态 |
| ---- | ---- | ---- |
| 玩家等级 | 局内等级；局外 Profile 经验条 | ✅ / ⚠️ |
| 天赋树 | 2 条天赋（生命/攻击），金币升级 | ⚠️ 无 UI |
| 科技树 | 占位 Tab，未实现 | ❌ |
| 升级卡 | 13 张卡 + 8 奖池，多来源发卡 | ⚠️ |
| 成就系统 | `AchievementManager` + 基础面板 | ⚠️ |
| 技能解锁 | 累计游玩时长 | ✅ |

---

# 11. 经济系统

> 详细设计见 [SDD-10-经济与商城系统](sdd/SDD-10-经济与商城系统.md)

## 货币

| 货币 | 用途 |
| ---- | ---- |
| 金币 | 天赋、普通商品 |
| 钻石 | 宝箱、抽奖、高级商品 |
| 体力 | 开战消耗 |
| 广告券 | 兑换、广告补给 |

## 获取来源

局末结算 · 商城购买 · 免费钻（12h）· 广告 · 签到 · 离线/在线 · 抽奖 · 通关奖励

## 消耗来源

开战体力 · 商城购买 · 宝箱单抽/十连 · 抽奖 · 天赋升级

---

# 12. UI 系统

> 详细设计见 [SDD-11-UI系统](sdd/SDD-11-UI系统.md)

## 页面结构

```
MainScene（大厅）
├── 首页 Battle Page（开战 / Meta 奖励 / 卡牌区）
├── 商城 Shop Page（补给 / 宝箱 / 兑换）
└── BottomNav（Home / Shop / 占位）

BattleScene
├── Gameplay HUD
├── Pause / Upgrade / WaveTransition / GameOver
└── 局内 Shop / SignIn / Achievement 弹层
```

## 主界面（MainScene）

* 顶栏资源（钻石/金币/体力/广告券）
* 开战按钮（体力校验）
* Meta 奖励入口
* 商城 Tab

## 战斗界面

* HP / EXP / 波次 / 计时
* Buff 行（可选）
* 暂停

## 结算界面

* 生存时间 / 波次 / 击杀
* 金币 / 钻石
* 升级卡列表（**待实现**）

---

# 13. 音频系统

> 详细设计见 [SDD-12-音频系统](sdd/SDD-12-音频系统.md)

## BGM

* 主界面 · 战斗 · Boss · 暂停 · GameOver · 升级

## 音效

* UI 点击/确认 · 玩家受伤 · 敌人命中/击杀 · 技能施法 · Boss 警告

**状态**：⚠️ `AudioManager` 已实现，`AudioDatabase` 资产可能缺失

---

# 14. 美术规范

## 风格

**科技废土** — 详见 `docs/version_02/prompt_ui_tech_wasteland_visual_design.md`

## UI 风格

`UiTechWastelandPalette` 配色；中文 Noto Sans SC TMP；稀有度色框。

## 特效风格

技能 VFX 占位 + `SkillCastVfxPlayer`；伤害飘字池化。

## 资源规格（建议）

| 类型 | 尺寸 |
| ---- | ---- |
| Icon | 128×128 |
| SkillIcon | 128×128 |
| Character | 按 URP 2D 精灵规范 |
| Enemy | Bat 系列 Prefab 已有 |

**状态**：⚠️ 大量占位色块，待美术替换

---

# 15. 数值设计

> 详细公式与曲线见 [Design_document.md](../Design_document.md)

## 玩家成长曲线

局内：`PlayerDataSO.experiencePerLevel` 驱动；具体等级表由 SO 配置。

## 怪物成长曲线

```
波次倍率 = 1 + (waveIndex - 1) × statScalePerWave
最终属性 = BaseStats × 波次倍率 × 精英倍率 × 地图倍率
```

## 掉落表

| 奖励 | 来源 | 状态 |
| ---- | ---- | ---- |
| 金币/钻石 | 局末 `RunRewardSettlementSO` | ✅ |
| 升级卡 | 8 个奖池加权随机 | ✅ |
| 局内金币 | HUD 展示（局内临时） | ✅ |

---

# 16. 存档系统

> 详细设计见 [SDD-13-存档与配置系统](sdd/SDD-13-存档与配置系统.md)

## 存储内容

玩家资源 · 天赋/装备/技能等级 · 升级卡库存 · 签到/商城限购 · Meta 时间戳 · 设置 · 统计 · 局内进度

## 存档方案

| 方案 | 状态 |
| ---- | ---- |
| JSON 本地 | ✅ `save.json` + backup |
| Binary | ❌ |
| Cloud Save | 🔜 二期 |

---

# 17. 技术架构

> 详细设计见 [SDD-01-核心框架](sdd/SDD-01-核心框架与启动.md) · [技术规格说明书](技术规格说明书.md)

## 项目结构

```
Assets/
├── Scripts/          # L0–L5 分层逻辑
├── Resources/Config/ # SO 配置
├── Scenes/           # MainScene, BattleScene
├── Prefabs/
├── Editor/           # 场景/UI/配置 Bootstrap
├── Graphics/
└── TextMesh Pro/
```

## 核心模块

`GameBootstrapper` · `GameManager` · `GameFlowManager` · `WaveManager` · `UIManager` · `AudioManager` · `SaveManager` · `ConfigManager` · `ResourceManager` · `ShopManager` · `UpgradeCardManager`

## 第三方插件

| 插件 | 用途 | 状态 |
| ---- | ---- | ---- |
| TextMesh Pro | UI 文字 | ✅ |
| URP 2D | 渲染 | ✅ |
| Unity Ads | 激励广告 | ✅ |
| DOTween | 面板动画 | ⚠️ 可选 |
| Odin / Addressables / UniTask | — | 未使用 |

---

# 18. 商业化设计

> 详细设计见 [SDD-10-经济与商城系统](sdd/SDD-10-经济与商城系统.md)

## 广告

* 激励广告：体力回满、广告券、广告宝箱、免费补给
* Banner：未实现

## 内购（二期）

* 月卡 · 通行证 · 首充礼包 — **❌ 无 Unity Purchasing**

## 抽卡 / 宝箱

* 商城普通/高级宝箱（单抽/十连）
* 幸运抽奖（`MetaRewardService`）
* 概率预览已实现；**保底计数待实现**

---

# 19. MVP 开发范围

## 必须实现（P0）

| 功能 | 状态 |
| ---- | ---- |
| 自动攻击（射击） | ✅ |
| 技能系统框架 + 7 技能效果类 | ⚠️ |
| Buff 三选一 | ✅ |
| 波次无限模式 | ✅ |
| 体力 / 存档 / 顶栏资源 | ✅ |
| 商城宝箱 + 奖池预览 | ✅ |
| 升级卡发卡 | ✅ |
| MainScene + BattleScene 闭环 | ✅ |

## 可延期功能（P1/P2）

| 功能 | 状态 |
| ---- | ---- |
| 升级卡消耗养成 | ❌ P0 阻断 |
| 四循环 Buff 计数 | ❌ |
| 7 日签到完整 UI | ⚠️ |
| 成长页 / 设置 / 排行榜 | ❌ |
| 装备 UI | 🔜 二期 |
| IAP / 云存档 / 新手引导 | 🔜 |

---

# 20. 开发排期

> 与 `docs/version_02/work_plan.md` 对齐

## Milestone 1 — 核心战斗 Demo

* 内容：Player、Enemy、Wave、Damage、射击
* 状态：**已完成**

## Milestone 2 — 技能与 Buff 系统

* 内容：7 技能、三选一、GameFlow
* 状态：**进行中**（配置资产与平衡待补）

## Milestone 3 — Boss 与特殊敌人

* 内容：Boss 阶段、5 种特殊能力
* 状态：**部分完成**

## Milestone 4 — Meta 与 UI

* 内容：MainScene、商城、升级卡、签到、广告
* 状态：**进行中**

## Milestone 5 — 正式上线版本

* 内容：养成闭环、美术、性能、合规、商店上架
* 状态：**未开始**

---

## 附录 A — 需求追溯

完整追溯矩阵见 [项目需求文档.md](项目需求文档.md) §4。

## 附录 B — 系统设计文档

| SDD | 路径 |
| --- | ---- |
| 索引 | [sdd/README.md](sdd/README.md) |
| 各模块 | `docs/sdd/SDD-01` ~ `SDD-13` |

---

*本 PRD 描述产品「做什么」；「怎么做」见 `docs/sdd/` 与 [技术规格说明书](技术规格说明书.md)。*
