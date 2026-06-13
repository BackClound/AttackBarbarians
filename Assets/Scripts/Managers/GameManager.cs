using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局游戏状态管理器：对外 API、状态机持有与 <see cref="GameEvents"/> 广播。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 初始化，不应单独在场景中重复创建多个实例。</para>
/// <para><b>推荐挂载对象：</b>挂在 <c>GameSystems</c> 根物体上，或作为其子物体 <c>GameManager</c> 的唯一组件。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、UI 面板；避免与 <see cref="Player"/> 单例混在同一物体上。</para>
/// <para><b>获取方式：</b>优先 <c>ServiceLocator.Get&lt;GameManager&gt;()</c>；Bootstrap 前可用 <see cref="MonoSingleton{T}.Instance"/>（Awake 后）。</para>
/// <para><b>时间缩放：</b>由内部 <see cref="GameStateMachine"/> 在状态进入时统一设置，本类不再直接写 <c>Time.timeScale</c>（<see cref="Shutdown"/> 除外）。</para>
/// </remarks>
public class GameManager : MonoSingleton<GameManager>, IGameSystem
{
    [SerializeField] private GameState initialState = GameState.Bootstrapping;
    [SerializeField] private bool enableStateDebugLogs;

    private GameStateMachine stateMachine;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized { get; private set; }
    /// <summary>当前游戏状态。</summary>
    public GameState CurrentState => stateMachine != null ? stateMachine.CurrentState : initialState;
    /// <summary>上一游戏状态。</summary>
    public GameState PreviousState => stateMachine != null ? stateMachine.PreviousState : initialState;
    /// <summary>是否处于暂停状态。</summary>
    public bool IsPaused => CurrentState == GameState.Paused;

    /// <summary>战斗与移动类输入是否允许（Playing 时为 true）。</summary>
    public bool IsGameplayInputEnabled => CurrentState == GameState.Playing;

    /// <summary>是否处于可结算伤害的战斗阶段。</summary>
    public bool IsCombatActive => CurrentState == GameState.Playing;

    /// <summary>创建状态机并完成初始化。</summary>
    public void Initialize()
    {
        bool debugLogs = enableStateDebugLogs;
        if (ServiceLocator.TryGet(out ConfigManager config) && config.GameConfig != null)
        {
            debugLogs |= config.GameConfig.EnableRuntimeLogs;
        }

        stateMachine = new GameStateMachine(initialState, debugLogs);
        IsInitialized = true;
    }

    /// <summary>每帧更新（当前无逻辑）。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>关闭状态机并重置初始化标志。</summary>
    public void Shutdown()
    {
        stateMachine?.Shutdown();
        IsInitialized = false;
    }

    /// <summary>切换到主菜单状态。</summary>
    public void OpenMainMenu()
    {
        TryChange(GameState.MainMenu);
    }

    /// <summary>切换到加载状态。</summary>
    public void BeginLoading()
    {
        TryChange(GameState.Loading);
    }

    /// <summary>切换到 Playing 并广播 GameStarted。</summary>
    public void StartGame()
    {
        if (CurrentState != GameState.Playing && !TryChange(GameState.Playing))
        {
            return;
        }

        GameEvents.RaiseGameStarted(this);
    }

    /// <summary>切换到暂停状态并广播 GamePaused。</summary>
    public void PauseGame()
    {
        if (!TryChange(GameState.Paused))
        {
            return;
        }

        GameEvents.RaiseGamePaused(this);
    }

    /// <summary>从暂停恢复并广播 GameResumed。</summary>
    public void ResumeGame()
    {
        if (!TryChange(GameState.Playing))
        {
            return;
        }

        GameEvents.RaiseGameResumed(this);
    }

    /// <summary>切换到波次过渡状态。</summary>
    public void BeginWaveTransition()
    {
        TryChange(GameState.WaveTransition);
    }

    /// <summary>切换到升级三选一状态。</summary>
    public void BeginUpgradeChoosing()
    {
        TryChange(GameState.UpgradeChoosing);
    }

    /// <summary>升级完成后返回 Playing。</summary>
    public void CompleteUpgradeAndResume()
    {
        TryChange(GameState.Playing);
    }

    /// <summary>切换到 GameOver 并广播 GameOver。</summary>
    public void GameOver()
    {
        if (CurrentState == GameState.GameOver)
        {
            return;
        }

        if (!TryChange(GameState.GameOver))
        {
            return;
        }

        GameEvents.RaiseGameOver(this);
    }

    /// <summary>切换到 Restarting 并重新加载当前场景。</summary>
    public void RestartGame()
    {
        if (!TryChange(GameState.Restarting))
        {
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>切换到 Exiting 并退出应用。</summary>
    public void QuitGame()
    {
        TryChange(GameState.Exiting);
        Application.Quit();
    }

    /// <summary>尝试切换游戏状态（公开 API）。</summary>
    /// <param name="newState">目标状态。</param>
    /// <returns>切换成功返回 true，否则返回 false。</returns>
    public bool TryChangeState(GameState newState) => TryChange(newState);

    /// <summary>检查是否允许切换到目标状态。</summary>
    /// <param name="newState">目标状态。</param>
    /// <returns>允许切换返回 true，否则返回 false。</returns>
    public bool CanTransitionTo(GameState newState) =>
        stateMachine != null && stateMachine.CanTransitionTo(newState);

    /// <summary>内部状态切换并广播 GameStateChanged。</summary>
    /// <param name="newState">目标状态。</param>
    /// <returns>切换成功返回 true，否则返回 false。</returns>
    private bool TryChange(GameState newState)
    {
        if (stateMachine == null)
        {
            Debug.LogError("[GameManager] State machine not initialized.");
            return false;
        }

        if (!stateMachine.TryChangeState(newState, out string reason))
        {
            if (enableStateDebugLogs && !string.IsNullOrEmpty(reason))
            {
                Debug.LogWarning($"[GameManager] {reason}");
            }

            return false;
        }

        GameEvents.RaiseGameStateChanged(this, new GameStateChange(stateMachine.PreviousState, stateMachine.CurrentState));
        return true;
    }
}
