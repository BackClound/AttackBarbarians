# Buff System 模块需求提示词

## 目标
实现可配置 Buff 系统，支持叠加、永久 Buff、临时 Buff、被动 Buff、光环 Buff、技能 Buff、敌人减益和局外成长。

## 当前基础
`SkillBuff核心机制.md` 已描述升级选单和 BuffSO，但代码中尚未形成 Modifier 管线，`Stat.GetFinalValue()` 仍偏基础值返回。

## 输出要求
- 生成 `BuffDataSO`：ID、名称、图标、描述、类型、目标、持续时间、最大层数、数值 Modifier。
- 生成 `BuffRuntime`：当前层数、剩余时间、来源、目标。
- 生成 `BuffManager`：添加、刷新、叠加、移除、Tick、清理。
- 生成 `IBuffEffect`：应用属性、技能参数、DOT、控制、光环等效果。
- 支持 Player Buff、Enemy Debuff、Skill Buff、Permanent Buff。

## 实现流程
1. 定义 Buff 类型和叠加规则。
2. 将属性加成接入 `Entity_Stats` 或新的 RuntimeStats。
3. DOT 和控制类 Buff 通过 Tick 或事件触发。
4. Buff 添加/移除时发布事件，UI 展示图标和层数。
5. 存档只保存需要持久化的 Buff ID、等级和层数。

## 数据流
`UpgradeManager/Skill/DamageSystem -> BuffManager -> RuntimeStats/SkillRuntime -> GameEvents.OnBuffChanged`

## 验收标准
- 同类 Buff 可按配置叠加或刷新。
- 临时 Buff 到期自动移除并还原属性。
- 永久 Buff 可保存并在游戏启动时恢复。

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

