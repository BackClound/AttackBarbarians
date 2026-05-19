# Core Framework 场景挂载说明

本文说明第一阶段核心框架各类的 Unity 挂载方式。实现新 Manager 时须同步更新本表与各类上的 `///` 注释。

## 挂载总览

| 类型 | 是否需要挂场景物体 | 推荐挂载位置 | 获取方式 |
|------|-------------------|-------------|----------|
| `GameBootstrapper` | **是** | `GameSystems` 根物体 | `Instance` / `ServiceLocator` |
| `ConfigManager` | **是** | `GameSystems` 根或子物体 | `ServiceLocator.Get<ConfigManager>()` |
| `GameManager` | **是** | `GameSystems` 根或子物体 `GameManager` | `ServiceLocator.Get<GameManager>()` |
| `PoolManager` | **是** | `GameSystems/PoolRoot` | `ServiceLocator.Get<PoolManager>()` |
| `EventBus` | **否** | 由 Bootstrapper 代码创建 | `ServiceLocator.Get<EventBus>()` |
| `ServiceLocator` | **否** | 静态类 | `Get` / `TryGet` |
| `IGameSystem` | **否** | 接口 | — |
| `GameConstants` | **否** | 静态常量 | 直接引用 |
| `MonoSingleton<T>` / `SingletonHost<T>` | **否** | 单例基类 / 宿主 | `Instance` / `TryGet` |
| `GameState` | **否** | 枚举 | — |
| `GameStateChange` | **否** | 事件 Payload | — |
| `GameEventContext` | **否** | 事件 Payload | — |
| `GameConfig` | **否**（SO 资产） | `Assets/Resources/Config/GameConfig.asset` | `ConfigManager.GameConfig` |
| `ConfigDatabaseSO` | **否**（SO 资产） | `Assets/Resources/Config/ConfigDatabase.asset` | `ConfigManager.Database` |

**不要**将上述 Manager 挂在 Player、Enemy、Wall、UI Canvas、子弹 Prefab 上。

## 推荐 Hierarchy

```
GameSystems                    ← 空物体，挂 GameBootstrapper（可同挂 ConfigManager、GameManager）
├── PoolRoot                   ← 空物体，挂 PoolManager
│   ├── Pool_Enemy             ← 可选，作敌人池 Parent
│   ├── Pool_Bullet
│   └── Pool_DamageNumber
├── Player                     ← 已有玩法物体，保持原样
├── EnemyGenerateManager       ← 旧逻辑，暂不删除
└── …（相机、UI 等）
```

也可将 `ConfigManager`、`GameManager`、`PoolManager` 全部挂在 `GameSystems` 同一物体上；Bootstrapper 的 Inspector 槽位留空时会自动 `GetComponentInChildren` 或 `AddComponent`。

## 配置资产

1. 右键 Create → **Attack Barbarians → Config → Game Config**
2. 保存为 `Assets/Resources/Config/GameConfig.asset`
3. 拖到 `ConfigManager` 的 **Game Config** 字段（或依赖 Resources 自动加载）
4. 创建 **Config Database**（`Assets/Resources/Config/ConfigDatabase.asset`），填入各类 Data SO，并拖到 `GameConfig.Config Database`（详见 `docs/config_system.md`）

## GameBootstrapper Inspector

| 字段 | 说明 |
|------|------|
| Initialize On Awake | 进入场景自动 Bootstrap，一般保持开启 |
| Dont Destroy On Load | 跨场景保留，单例重复时销毁多余实例 |
| Config Manager | 可拖引用；留空自动解析 |
| Pool Manager | 可拖引用；留空自动解析 |
| Game Manager | 可拖引用；留空自动解析 |

## 启动顺序

`ConfigManager.Initialize` → `EventBus.Initialize` → `PoolManager.Initialize` → `GameManager.Initialize` →（若配置允许）`GameManager.StartGame`

## 手动验证步骤

1. 在可玩场景创建 `GameSystems` 并按上表挂载组件。
2. 创建并配置 `GameConfig.asset`。
3. 运行场景：Console 无重复 Bootstrap 报错；`ServiceLocator.Get<GameManager>().CurrentState` 为 `Playing`（默认配置下）。
4. 令 Player 死亡：应进入 `GameOver` 且 `timeScale == 0`。
5. 确认 Player、Enemy 生成、射击仍与改造前一致（旧逻辑未移除）。

## 业务代码接入示例

```csharp
// 玩家死亡（已在 Player_Health 中示例）
if (ServiceLocator.TryGet(out GameManager gameManager))
{
    gameManager.GameOver();
}

// 订阅状态变化（UI 等，须在 OnDestroy 取消订阅）
GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
// 取消订阅：GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
```

## 与旧逻辑的关系

- `Player`、`EnemyGenerateManager`、`SkillShoot` 等**保持原场景引用**，不要求迁到 `GameSystems`。
- 新系统通过 `ServiceLocator` / `EventBus` 旁路接入；迁移完成后再逐步下线旧入口。

## 单例框架

- Manager 优先 `ServiceLocator`；场景对象用 `MonoSingleton` / `SingletonHost` 的 `Instance`。
- 详见 **`docs/singleton_framework.md`**。
