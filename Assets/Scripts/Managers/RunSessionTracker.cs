using UnityEngine;

/// <summary>
/// 单局游玩时长统计（仅在 <see cref="GameState.Playing"/> 累计）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 上。</para>
/// </remarks>
public class RunSessionTracker : MonoBehaviour, IGameSystem
{
    private GameManager gameManager;
    private float sessionDurationSeconds;
    private bool isTracking;
    private bool isInitialized;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>本局累计游玩时长（秒）。</summary>
    public float SessionDurationSeconds => sessionDurationSeconds;

    /// <summary>订阅游戏事件并开始追踪。</summary>
    public void Initialize()
    {
        gameManager = ServiceLocator.TryGet(out GameManager gm) ? gm : null;
        GameEvents.SubscribeGameStarted(OnGameStarted);
        GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.SubscribeGameOver(OnGameOver);
        isInitialized = true;
    }

    /// <summary>在 Playing 状态下累计时长。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!isTracking)
        {
            return;
        }

        sessionDurationSeconds += deltaTime;
    }

    /// <summary>取消订阅并重置追踪状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeGameOver(OnGameOver);
        isInitialized = false;
        isTracking = false;
    }

    /// <summary>清零本局时长计数。</summary>
    public void ResetSession()
    {
        sessionDurationSeconds = 0f;
    }

    /// <summary>游戏开始时重置并开始追踪。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnGameStarted(GameEventContext ctx)
    {
        ResetSession();
        isTracking = true;
    }

    /// <summary>游戏结束时停止追踪。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnGameOver(GameEventContext ctx)
    {
        isTracking = false;
    }

    /// <summary>根据状态切换追踪开关并在新局开始时重置。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnGameStateChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not GameStateChange change)
        {
            return;
        }

        isTracking = change.NewState == GameState.Playing;

        if (change.NewState == GameState.Playing &&
            (change.OldState == GameState.MainMenu || change.OldState == GameState.Bootstrapping))
        {
            ResetSession();
        }
    }
}
