# Player System 模块需求提示词

## 目标
完善 Player 防守核心，支持基础属性、自动攻击、技能释放、受击死亡、Buff 应用、永久成长和移动端适配。

## 当前基础
`Player` 已有单例、状态机、Idle/Shoot/Dead 状态、`Player_Health`、`PlayerCombat` 和 `SkillShoot` 调用链。当前仍依赖直接引用和单例，目标扫描逻辑与技能逻辑存在重复。

## 输出要求
- 生成或改造 `PlayerController`：负责运行时行为和状态机。
- 生成 `PlayerDataSO`：配置生命、攻击、攻速、射程、暴击、初始技能。
- 生成 `PlayerRuntimeStats`：整合基础属性、局内 Buff、永久成长。
- 统一目标选择策略：最近、血量最低、距离墙体最近、Boss 优先。
- Player 事件：血量变化、死亡、攻击开始、技能释放、属性变化。

## 实现流程
1. 保留现有 Player 可运行状态，不大拆状态机。
2. 抽出目标扫描服务，供 PlayerCombat 和 Skill 使用。
3. 将属性读取统一指向 RuntimeStats。
4. 将血量 UI 和死亡流程改为事件驱动。
5. 为 Buff/Upgrade 留出 `ApplyModifier` 入口。

## 数据流
`PlayerDataSO + SaveData + BuffRuntimeData -> PlayerRuntimeStats -> PlayerController/SkillManager -> DamageSystem`

## 验收标准
- Player 可以自动选择目标并释放基础技能。
- Player 属性变化能即时影响攻击、血量、技能效果。
- UI 不直接读取 Player 单例也能刷新血量和状态。

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

