# Input System 模块需求提示词

## 目标
配置移动端优先的输入系统，支持暂停、返回、升级选择、设置、商店、音效开关和后续技能手动触发。

## 当前基础
`局内点击事件和输入系统.md` 已描述 Input Action Asset 和 `PlayerInputHandler`，但代码层尚未看到统一输入管理。

## 输出要求
- 生成 `GameInput.inputactions` 设计说明。
- 生成 `InputManager` 或 `PlayerInputHandler`：订阅输入并发布 GameEvents。
- Action Maps 包含 `Gameplay`、`UI`、`Debug`。
- 支持触屏点击、返回键、暂停键和 UI 导航。
- 输入启停由 `GameState` 控制。

## 实现流程
1. 定义输入事件，不让 UI 和战斗对象直接读取 Input。
2. `Playing` 状态启用 Gameplay，`Paused/UpgradeChoosing` 状态启用 UI。
3. 移动端按钮通过 Unity UI 事件调用同一套输入接口。
4. Debug 输入只在开发构建启用。

## 验收标准
- 暂停、返回、升级选择和设置按钮都能通过事件触发。
- 游戏暂停或升级选择时不会误触发战斗输入。
- 输入逻辑与 UI 展示解耦。

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

