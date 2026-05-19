# Singleton Framework 模块需求提示词

## 目标
统一 MonoBehaviour 单例的认领、去重、释放与访问方式，减少散落的 `FindObjectOfType`、重复 Awake 模板，并与 `ServiceLocator` 协同。

## 当前基础
- 已有 `ServiceLocator` 与 `GameBootstrapper` 注册 Manager。
- `Player`、`WallControlManager`、`DamageNumberController` 等仍使用各自风格的 `sInstance` / 懒查找。

## 输出要求
- `SingletonHost<T>`：供 `Entity` 子类等无法继承单例基类的类型使用。
- `MonoSingleton<T>` / `PersistentMonoSingleton<T>`：Manager 类基类。
- `SingletonOptions`、`SingletonDuplicatePolicy`：可配置跨场景与重复实例策略。
- 文档 `singleton_framework.md`：API、迁移指南、与 ServiceLocator 优先级。

## 实现流程
1. 新增 Core/Singleton 目录与基类，不破坏现有场景引用。
2. 迁移 `GameBootstrapper`、`GameManager` 至 `MonoSingleton`。
3. 迁移 `Player`、`WallControlManager`、`DamageNumberController` 至 `SingletonHost`，保留 `sInstance` 等旧名别名。
4. 禁止默认 `FindObjectOfType` 懒查找。

## 访问优先级
1. Bootstrap 完成后：`ServiceLocator.Get<T>()`（Manager）。
2. 场景单例：`X.Instance` / `SingletonHost<X>.Instance`。
3. 避免：跨模块 `FindObjectOfType`、无关静态单例链式调用。

## 验收标准
- 重复挂载同类型单例时，仅保留一个实例且无静默失败。
- `WallControlManager` 不再在 getter 中 `FindFirstObjectByType`。
- 旧代码 `Player.sInstance`、`WallControlManager.sInstance` 仍可编译运行。

## 模块依赖边界
- 仅依赖 `ServiceLocator`（可选注册），不依赖 UI、战斗具体实现。
- 后续模块新增单例须选用 Host 或 MonoSingleton，不得再复制 Awake 去重代码。

## 旧逻辑兼容
- 保留 `sInstance`、`numberControllerInstance` 等只读别名指向 `Instance`。
- 不强制一次性替换所有调用方。
