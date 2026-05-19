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

    public bool IsInitialized { get; private set; }
    public GameState CurrentState => stateMachine != null ? stateMachine.CurrentState : initialState;
    public GameState PreviousState => stateMachine != null ? stateMachine.PreviousState : initialState;
    public bool IsPaused => CurrentState == GameState.Paused;

    /// <summary>战斗与移动类输入是否允许（Playing 时为 true）。</summary>
    public bool IsGameplayInputEnabled => CurrentState == GameState.Playing;

    /// <summary>是否处于可结算伤害的战斗阶段。</summary>
    public bool IsCombatActive => CurrentState == GameState.Playing;

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

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        stateMachine?.Shutdown();
        IsInitialized = false;
    }

    public void OpenMainMenu()
    {
        TryChange(GameState.MainMenu);
    }

    public void BeginLoading()
    {
        TryChange(GameState.Loading);
    }

    public void StartGame()
    {
        if (CurrentState != GameState.Playing && !TryChange(GameState.Playing))
        {
            return;
        }

        GameEvents.RaiseGameStarted(this);
    }

    public void PauseGame()
    {
        if (!TryChange(GameState.Paused))
        {
            return;
        }

        GameEvents.RaiseGamePaused(this);
    }

    public void ResumeGame()
    {
        if (!TryChange(GameState.Playing))
        {
            return;
        }

        GameEvents.RaiseGameResumed(this);
    }

    public void BeginWaveTransition()
    {
        TryChange(GameState.WaveTransition);
    }

    public void BeginUpgradeChoosing()
    {
        TryChange(GameState.UpgradeChoosing);
    }

    public void CompleteUpgradeAndResume()
    {
        TryChange(GameState.Playing);
    }

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

    public void RestartGame()
    {
        if (!TryChange(GameState.Restarting))
        {
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        TryChange(GameState.Exiting);
        Application.Quit();
    }

    public bool TryChangeState(GameState newState) => TryChange(newState);

    public bool CanTransitionTo(GameState newState) =>
        stateMachine != null && stateMachine.CanTransitionTo(newState);

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
