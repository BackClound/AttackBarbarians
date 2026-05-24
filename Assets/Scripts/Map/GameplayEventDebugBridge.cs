using UnityEngine;

/// <summary>
/// 局内随机事件调试桥：在运行时日志中输出事件开始/结束，便于验证闭环。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>可选。挂在 <c>GameSystems</c>；未挂载时不影响玩法。</para>
/// </remarks>
public class GameplayEventDebugBridge : MonoBehaviour, IGameSystem
{
    [SerializeField] private bool logToConsole = true;

    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        GameEvents.SubscribeGameplayEventStarted(OnGameplayEventStarted);
        GameEvents.SubscribeGameplayEventEnded(OnGameplayEventEnded);
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeGameplayEventStarted(OnGameplayEventStarted);
        GameEvents.UnsubscribeGameplayEventEnded(OnGameplayEventEnded);
        isInitialized = false;
    }

    private void OnGameplayEventStarted(GameEventContext ctx)
    {
        if (!logToConsole || ctx.Payload is not GameplayEventArgs args)
        {
            return;
        }

        Debug.Log(
            $"[GameplayEvent] Started id={args.EventConfigId} wave={args.WaveIndex} " +
            $"duration={args.DurationSeconds:F1}s");
    }

    private void OnGameplayEventEnded(GameEventContext ctx)
    {
        if (!logToConsole || ctx.Payload is not GameplayEventArgs args)
        {
            return;
        }

        Debug.Log($"[GameplayEvent] Ended id={args.EventConfigId}");
    }
}
