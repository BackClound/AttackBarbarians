# Event System 模块需求提示词

## 目标
建立全项目事件系统，替代模块间直接引用，让战斗、波次、升级、UI、音效、存档等系统通过事件解耦。

## 当前基础
已有文档 `event_system.md` 提到 `GameEvents`，但代码中仍大量使用单例和直接调用，例如 Player、Wall、DamageNumber、Enemy 之间存在强耦合。

## 输出要求
- 生成 `GameEvents` 静态事件类，按 Game、Wave、Player、Enemy、Damage、Skill、Buff、UI、Audio、Save 分区。
- 生成事件参数结构：`DamageEventArgs`、`EnemyEventArgs`、`WaveEventArgs`、`BuffEventArgs`。
- 生成事件调用方法，避免外部直接触发 event。
- 为订阅方提供 `OnEnable` 订阅、`OnDisable` 取消订阅模板。
- 输出事件清单文档，说明发布者、订阅者和触发时机。

## 实现流程
1. 盘点当前跨模块直接调用路径。
2. 先迁移低风险事件：敌人死亡、波次开始/结束、血量变化、伤害显示、升级选择。
3. 保留旧逻辑可运行，再逐步替换直接引用。
4. 为每个事件添加空订阅保护和 Debug 开关。

## 事件流
- `EnemyDied` -> WaveManager 统计、UpgradeManager 加经验、UI 更新、Audio 播放。
- `DamageApplied` -> Health 扣血、DamageNumber 显示、Buff/DOT 处理。
- `WaveCompleted` -> RandomReward 生成、UpgradePanel 打开、SaveManager 保存。

## 验收标准
- UI、Audio、Save 不直接依赖具体 Enemy/Player 对象。
- 所有事件订阅都有取消订阅。
- 事件参数能表达来源、目标和上下文。
