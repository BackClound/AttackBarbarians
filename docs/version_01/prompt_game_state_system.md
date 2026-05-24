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

## 验收标准补充
- 本模块完成后必须形成最小可运行闭环，可以在当前 Unity 场景中通过手动挂载或现有入口验证核心流程。
- 相关配置、运行时数据、事件发布和事件订阅必须有明确入口，异常或缺失配置时要给出可定位的问题表现。
- 高频逻辑不得引入明显 GC 分配；涉及生成、销毁、特效、投射物、敌人或 UI 飘字时必须优先接入对象池。
- 完成实现后必须说明受影响脚本、Prefab/Scene 挂载要求、测试步骤和未迁移的旧逻辑边界。

## 模块依赖边界
- 本模块只能依赖已完成或同阶段明确约定的公共接口、配置数据和事件 Key，不直接依赖后续阶段的具体实现类。
- 跨模块通信优先通过 `EventBus`、`IGameSystem`、`ServiceLocator`、ScriptableObject 配置或明确的 Controller API 完成。
- 禁止从低层模块反向引用高层模块；例如 Damage、Pool、Config 不应依赖 UI、Upgrade、Shop 等外围系统。
- 禁止在核心逻辑中散落 `FindObjectOfType`、硬编码场景路径或直接访问无关单例；必须通过启动流程、序列化引用或服务注册注入依赖。
- 数据结构与事件 Payload 必须保持小而稳定，避免为了单个功能暴露整个 Manager 或运行时对象内部状态。

## 旧逻辑兼容与迁移约束
- 保持当前可运行逻辑，不一次性删除或重写现有 `Player`、`Enemy`、`SkillShoot`、`EnemyGenerateManager` 等已在场景中使用的脚本。
- 新系统先以适配器、旁路入口或可切换开关接入；确认新流程可运行后，再逐步迁移旧职责。
- 每次迁移只替换一个清晰职责，例如目标选择、伤害结算、对象生成、UI 刷新或配置读取，避免跨系统大改。
- 迁移期间必须保持旧 Prefab、Animator、Collider、Layer、Tag 和 Inspector 序列化字段不失效。
- 删除旧代码前必须确认没有场景引用、Prefab 引用和运行时调用；无法确认时保留兼容层并标注后续清理任务。

