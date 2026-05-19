# Upgrade 与 Random Reward System 模块需求提示词

## 目标
实现局内升级和随机奖励系统，每波结束或经验升级时提供三选一 Buff/技能/属性强化，并支持权重、稀有度、前置条件和堆叠。

## 当前基础
`SkillBuff核心机制.md` 和 `level_upgrade_system.md` 已描述三选一升级，但代码层尚未有完整 `UpgradeManager`、随机池、稀有度、过滤条件和 UI 数据流。

## 输出要求
- 生成 `UpgradeOptionSO`：ID、名称、描述、图标、稀有度、权重、类型、前置条件。
- 生成 `RewardPoolSO`：奖励池、权重、波次限制、互斥规则。
- 生成 `UpgradeManager`：生成候选、选择升级、应用效果、记录已选。
- 生成 `RandomRewardManager`：负责三选一、掉落、结算奖励和稀有度规则。
- 支持基础属性 Buff、技能升级、新技能解锁、武器强化、金币/钻石奖励。

## 实现流程
1. Wave/Exp 事件触发 `ShowUpgradeChoices`。
2. RandomReward 根据当前状态过滤不可用选项。
3. 按权重和稀有度抽取 3 个不重复选项。
4. 玩家选择后应用到 Buff/Skill/Weapon/Resource 系统。
5. 保存局内已选升级，避免异常退出丢失进度。

## 数据流
`WaveCompleted/LevelUp -> RandomRewardManager -> UpgradeManager -> UI -> Buff/Skill/Weapon/Save`

## 验收标准
- 三选一不会出现重复或不可用选项。
- 已选 Buff 可堆叠并立即影响 Player。
- 奖励池可通过配置调整，不需要修改代码。
