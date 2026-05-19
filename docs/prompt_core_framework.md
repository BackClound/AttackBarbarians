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

## 验收标准
- 进入场景后能完成所有核心 Manager 初始化。
- 暂停、恢复、游戏结束流程有统一入口。
- 后续模块不需要直接查找全局对象即可获取依赖。
