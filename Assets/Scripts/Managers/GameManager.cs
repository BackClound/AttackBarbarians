using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 全局游戏状态管理器：启动、暂停、恢复、失败、重开、退出及状态变更事件。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 初始化，不应单独在场景中重复创建多个实例。</para>
/// <para><b>推荐挂载对象：</b>挂在 <c>GameSystems</c> 根物体上，或作为其子物体 <c>GameManager</c> 的唯一组件。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、UI 面板；避免与 <see cref="Player"/> 单例混在同一物体上。</para>
/// <para><b>获取方式：</b>优先 <c>ServiceLocator.Get&lt;GameManager&gt;()</c>；Bootstrap 前可用 <c>GameManager.Instance</c>（Awake 后）。</para>
/// <para><b>调用示例：</b>玩家死亡时 <c>ServiceLocator.Get&lt;GameManager&gt;().GameOver()</c>；暂停菜单 <c>PauseGame()</c> / <c>ResumeGame()</c>。</para>
/// </remarks>
public class GameManager : MonoBehaviour, IGameSystem
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState initialState = GameState.Bootstrapping;

    public bool IsInitialized { get; private set; }
    public GameState CurrentState { get; private set; }
    public GameState PreviousState { get; private set; }
    public bool IsPaused => CurrentState == GameState.Paused;

    private EventBus eventBus;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Initialize()
    {
        eventBus = ServiceLocator.TryGet(out EventBus bus) ? bus : null;
        CurrentState = initialState;
        PreviousState = initialState;
        IsInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        Time.timeScale = 1f;
        IsInitialized = false;
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (CurrentState != GameState.Playing)
        {
            return;
        }

        Time.timeScale = 0f;
        ChangeState(GameState.Paused);
        eventBus?.Publish(GameConstants.EventKeys.GamePaused, this);
    }

    public void ResumeGame()
    {
        if (CurrentState != GameState.Paused)
        {
            return;
        }

        Time.timeScale = 1f;
        ChangeState(GameState.Playing);
        eventBus?.Publish(GameConstants.EventKeys.GameResumed, this);
    }

    public void GameOver()
    {
        if (CurrentState == GameState.GameOver)
        {
            return;
        }

        Time.timeScale = 0f;
        ChangeState(GameState.GameOver);
        eventBus?.Publish(GameConstants.EventKeys.GameOver, this);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        ChangeState(GameState.Restarting);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        ChangeState(GameState.Exiting);
        Application.Quit();
    }

    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        GameState oldState = CurrentState;
        PreviousState = oldState;
        CurrentState = newState;
        eventBus?.Publish(GameConstants.EventKeys.GameStateChanged, this, new GameStateChange(oldState, newState));
    }
}

/// <summary>
/// 游戏状态切换事件负载，通过 <see cref="GameConstants.EventKeys.GameStateChanged"/> 发布。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public struct GameStateChange
{
    public GameState OldState { get; }
    public GameState NewState { get; }

    public GameStateChange(GameState oldState, GameState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}
