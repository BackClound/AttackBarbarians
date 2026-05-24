# Core Framework 模块需求提示词

## 目标
搭建 Attack Barbarians 的基础框架，为后续 Player、Enemy、Wave、Skill、Buff、UI、Save 等系统提供稳定入口和统一生命周期。

## 当前基础
项目已有 `Entity`、`EntityState`、`StateMachine`、`Entity_Health`、`Entity_Stats` 等通用脚本，但缺少统一 `GameManager`、服务注册、启动流程和模块生命周期。

## 依赖规则
遵循 `Manager + Controller + Config`、ScriptableObject 配置驱动、EventBus 解耦、对象池优先和移动端性能约束。

## 输出要求
- 生成 `GameManager`：管理游戏启动、暂停、恢复、失败、重开和退出。
- 生成 `GameBootstrapper`：负责初始化 Manager、加载配置、预热对象池、进入主流程。
- 生成 `IGameSystem`：定义 `Initialize()`、`Tick(float deltaTime)`、`Shutdown()`。
- 生成 `ServiceLocator` 或轻量系统注册表：避免散落的 `FindObjectOfType`。
- 生成 `GameConstants`：集中管理层级、Tag、事件 Key、路径等常量。

## 实现流程
1. 分析当前场景中的常驻对象和脚本依赖。
2. 确认启动顺序：Config -> Save -> Pool -> Event -> GameState -> UI -> Gameplay。
3. 将可独立系统抽象为 `IGameSystem`。
4. 保持当前可运行逻辑，不一次性迁移所有旧脚本。
5. 增加日志开关，避免移动端高频日志。

## 数据流
配置数据由 `ConfigManager` 读取后提供给各 Manager；运行时数据由 `GameManager` 和具体系统维护；跨模块状态变化通过 `GameEvents` 广播。

## Unity 场景挂载（必须输出）

实现或修改 Core Framework 时，必须在交付说明中包含挂载表，并同步更新 `docs/core_framework_scene_setup.md`。

| 类 | 挂载 | 说明 |
|----|------|------|
| `GameBootstrapper` | `GameSystems` 根物体 | 场景唯一入口，可 DontDestroyOnLoad |
| `ConfigManager` | `GameSystems` | 引用 `GameConfig.asset` |
| `GameManager` | `GameSystems` 或子物体 | 勿挂 Player |
| `PoolManager` | `GameSystems/PoolRoot` | Inspector 配置 Pools 列表 |
| `EventBus` / `ServiceLocator` | 不挂载 | Bootstrap 注册 |
| `GameConfig` | Resources 资产 | `Assets/Resources/Config/GameConfig.asset` |

详细 Hierarchy、Inspector 字段与验证步骤见 **`docs/core_framework_scene_setup.md`**。每个新增类须在源码中添加 `///` 类注释，写明是否挂载、推荐物体名、获取方式。

## 验收标准
- 进入场景后能完成所有核心 Manager 初始化。
- 暂停、恢复、游戏结束流程有统一入口。
- 后续模块不需要直接查找全局对象即可获取依赖。

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

