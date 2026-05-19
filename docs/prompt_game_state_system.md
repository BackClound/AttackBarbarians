# Game State Machine 模块需求提示词

## 目标
实现游戏级状态机，统一管理 MainMenu、Loading、Playing、Paused、UpgradeChoosing、WaveTransition、GameOver 等状态。

## 当前基础
实体级 `StateMachine` 已存在，但游戏流程层尚未统一，UI、暂停、升级、波次推进和存档触发缺少共同状态源。

## 输出要求
- 生成 `GameState` 枚举。
- 生成 `GameStateMachine`：支持状态切换、进入/退出回调和当前状态查询。
- 生成 `GameFlowManager`：连接 Wave、Upgrade、UI、Save、Audio。
- 通过 `GameEvents.OnGameStateChanged` 通知 UI 和系统。
- 明确 `Time.timeScale` 的管理归属。

## 实现流程
1. 梳理现有游戏入口和 UI 切换方式。
2. 定义允许的状态转换表，禁止非法跳转。
3. 将暂停、升级三选一、游戏失败统一纳入状态机。
4. 每次状态切换发布事件并记录 Debug 日志。

## 状态流
`MainMenu -> Loading -> Playing -> WaveTransition -> UpgradeChoosing -> Playing -> GameOver`

暂停流：
`Playing -> Paused -> Playing`

## 验收标准
- 任意时刻只有一个游戏状态。
- UI 面板切换、时间暂停、音效暂停和输入启停由状态驱动。
- 波次结束和升级选择不会互相抢状态。
