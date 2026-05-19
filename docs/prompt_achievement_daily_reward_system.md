# Achievement 与 Daily Reward System 模块需求提示词

## 目标
实现成就与每日奖励系统，增强长期留存，并与资源、存档、UI 和事件系统解耦。

## 当前基础
`work_flow.md` 第五阶段包含 Achievement 和 Daily Reward，当前代码尚未实现。

## 输出要求
- 生成 `AchievementDataSO`：ID、名称、描述、目标类型、目标值、奖励。
- 生成 `AchievementManager`：监听事件、累计进度、领取奖励。
- 生成 `DailyRewardDataSO`：天数、奖励类型、数量、补签规则。
- 生成 `DailyRewardManager`：日期校验、签到、领取、保存。
- UI 通过事件展示红点、进度和可领取状态。

## 实现流程
1. 成就进度来自事件，如击杀数、波次数、Boss 击杀、累计金币。
2. 每日奖励使用本地日期，预留服务器时间接口。
3. 奖励发放统一走 RandomReward/ResourceManager。
4. 所有领取状态写入 SaveData。

## 数据流
`GameEvents -> AchievementManager/DailyRewardManager -> RewardManager -> ResourceManager -> SaveManager/UI`

## 验收标准
- 成就进度能随战斗事件增长。
- 每日奖励同一天不可重复领取。
- 奖励领取后资源和存档同步更新。
