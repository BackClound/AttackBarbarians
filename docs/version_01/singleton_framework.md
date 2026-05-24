# Singleton Framework

## 概述

统一 Attack Barbarians 中 MonoBehaviour 单例的生命周期，与 **ServiceLocator**（Bootstrap 后 Manager）配合使用。

| 类型 | 挂载 | 用途 |
|------|------|------|
| `SingletonHost<T>` | 否 | 静态宿主，`Entity` 子类（如 `Player`） |
| `MonoSingleton<T>` | 否（基类） | Manager（如 `GameManager`） |
| `PersistentMonoSingleton<T>` | 否（基类） | 跨场景常驻（可选；`GameBootstrapper` 用 `MonoSingleton` + 可配置 `dontDestroyOnLoad`） |
| `SingletonOptions` | 否 | 跨场景 / 去重 / 是否注册到 Locator |
| `SingletonDuplicatePolicy` | 否 | `DestroyNewest`（默认）/ `DestroyOldest` |

## 访问优先级

1. **Manager（Bootstrap 后）**：`ServiceLocator.Get<GameManager>()` 或 `TryGet`
2. **场景单例**：`Player.Instance`、`WallControlManager.Instance`
3. **避免**：`FindObjectOfType`、`Player.sInstance` 链到其他单例（旧代码可逐步替换）

## API 速查

### MonoSingleton（Manager）

```csharp
public class GameManager : MonoSingleton<GameManager>, IGameSystem { }

// Awake 后
GameManager.Instance;
GameManager.TryGet(out var gm);
```

子类重写 `OnSingletonAwake()` / `OnSingletonDestroy()`，不要重复写 `Instance` 字段与去重 `Destroy`。

### SingletonHost（Entity 等）

```csharp
public override void Awake()
{
    base.Awake();
    if (!SingletonHost<Player>.TryClaim(this, this, SingletonOptions.SceneDefault, out bool destroyed) || destroyed)
        return;
    // 初始化…
}

private void OnDestroy() => SingletonHost<Player>.Release(this);
```

### 跨场景

```csharp
SingletonHost<T>.TryClaim(this, this, SingletonOptions.PersistentDefault, out _);
// 或 GameBootstrapper：Inspector「Dont Destroy On Load」为 true 时等同 PersistentDefault
```

### 可选注册到 ServiceLocator

仅在**未**由 `GameBootstrapper` 注册、且需 `TryGet` 的类型上设置：

```csharp
var options = SingletonOptions.SceneDefault;
options.RegisterWithServiceLocator = true;
```

Manager 仍由 Bootstrap 统一 `Register`，勿重复开启。

## 已迁移类型

| 类型 | 机制 | 旧别名 |
|------|------|--------|
| `GameBootstrapper` | `MonoSingleton` + 可配置持久化 | — |
| `GameManager` | `MonoSingleton` | — |
| `Player` | `SingletonHost` | `sInstance` |
| `WallControlManager` | `SingletonHost`（已移除 Find 懒查找） | `sInstance` |
| `DamageNumberController` | `SingletonHost` | `numberControllerInstance` |

## 文件结构

```
Assets/Scripts/Core/Singleton/
├── SingletonDuplicatePolicy.cs
├── SingletonOptions.cs
├── SingletonHost.cs
├── MonoSingleton.cs
└── PersistentMonoSingleton.cs
```

## 手动验证

1. 运行可玩场景：无重复 Bootstrap / 单例报错。
2. `Player.sInstance` 与 `Player.Instance` 指向同一对象。
3. 敌人攻击城墙：`WallControlManager.Instance.TakeDamage` 正常。
4. 伤害飘字：订阅事件仍显示（`DamageNumberController` 单例唯一）。
5. （可选）复制带 `GameBootstrapper` 的物体进场景：重复实例应被销毁。

## 后续任务

- 新 Manager 继承 `MonoSingleton`，禁止手写 `static Instance`。
- 逐步将 `Player.sInstance` 调用改为 `Player.Instance` 或事件 / ServiceLocator。
- `EnemyGenerateManager` 等若需单例，按上表选用 Host 或 MonoSingleton。
