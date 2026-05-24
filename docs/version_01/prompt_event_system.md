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

