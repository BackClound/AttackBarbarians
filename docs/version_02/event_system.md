
### 2.6 `event-system.mdc`

```yaml
---
description: "C# 事件与 UnityEvent 使用规范"
globs: ["Assets/Scripts/**/*.cs"]
alwaysApply: false
---

# 事件系统规范

## 设计原则
- 使用 C# 内置事件（`event Action<T>`）实现模块间解耦，避免直接引用。
- 事件应定义在独立的静态类 `GameEvents` 中集中管理，便于追踪和维护。
- 使用 `UnityEvent` 暴露可在 Inspector 中绑定的 UI/动画事件。

## 游戏事件清单（示例）
```csharp
public static class GameEvents
{
    // 游戏状态事件
    public static event Action<GameState> OnGameStateChanged;
    public static event Action OnGameStarted;
    public static event Action OnGameOver;
    public static event Action OnWaveCompleted;
    public static event Action OnGamePaused;
    public static event Action OnGameResumed;
    
    // 波次事件
    public static event Action<int> OnWaveStarted;      // 参数：波次编号
    public static event Action<int> OnWaveCompleted;    // 参数：波次编号
    
    // 塔相关事件
    public static event Action<Tower> OnTowerPlaced;
    public static event Action<Tower> OnTowerUpgraded;
    public static event Action<Tower> OnTowerDestroyed;
    
    // 敌人事件
    public static event Action<Enemy> OnEnemySpawned;
    public static event Action<Enemy> OnEnemyDied;      // 参数：被击杀的敌人
    public static event Action<Enemy> OnEnemyReachedEnd;
    
    // 肉鸽升级事件
    public static event Action<RuneSO> OnRuneSelected;
    public static event Action<List<RuneSO>> OnRuneOptionsDisplayed;
    
    // 资源事件
    public static event Action<int> OnGoldChanged;      // 参数：当前金币数
    public static event Action<int> OnScoreChanged;
}