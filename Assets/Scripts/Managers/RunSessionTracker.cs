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

    public bool IsInitialized => isInitialized;
    public float SessionDurationSeconds => sessionDurationSeconds;

    public void Initialize()
    {
        gameManager = ServiceLocator.TryGet(out GameManager gm) ? gm : null;
        GameEvents.SubscribeGameStarted(OnGameStarted);
        GameEvents.SubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.SubscribeGameOver(OnGameOver);
        isInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isTracking)
        {
            return;
        }

        sessionDurationSeconds += deltaTime;
    }

    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        GameEvents.UnsubscribeGameStateChanged(OnGameStateChanged);
        GameEvents.UnsubscribeGameOver(OnGameOver);
        isInitialized = false;
        isTracking = false;
    }

    public void ResetSession()
    {
        sessionDurationSeconds = 0f;
    }

    private void OnGameStarted(GameEventContext ctx)
    {
        ResetSession();
        isTracking = true;
    }

    private void OnGameOver(GameEventContext ctx)
    {
        isTracking = false;
    }

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
