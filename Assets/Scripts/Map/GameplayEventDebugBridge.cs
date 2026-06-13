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

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>订阅事件并标记为已初始化。</summary>
    public void Initialize()
    {
        GameEvents.SubscribeGameplayEventStarted(OnGameplayEventStarted);
        GameEvents.SubscribeGameplayEventEnded(OnGameplayEventEnded);
        isInitialized = true;
    }

    /// <summary>每帧更新（当前无逻辑）。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消订阅并重置初始化状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeGameplayEventStarted(OnGameplayEventStarted);
        GameEvents.UnsubscribeGameplayEventEnded(OnGameplayEventEnded);
        isInitialized = false;
    }

    /// <summary>输出事件开始日志。</summary>
    /// <param name="ctx">事件上下文。</param>
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

    /// <summary>输出事件结束日志。</summary>
    /// <param name="ctx">事件上下文。</param>
    private void OnGameplayEventEnded(GameEventContext ctx)
    {
        if (!logToConsole || ctx.Payload is not GameplayEventArgs args)
        {
            return;
        }

        Debug.Log($"[GameplayEvent] Ended id={args.EventConfigId}");
    }
}
