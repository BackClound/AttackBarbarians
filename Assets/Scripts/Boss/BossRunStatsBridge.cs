using UnityEngine;

/// <summary>
/// Boss 击败统计桥接：将 <see cref="GameEvents.RaiseBossDefeated"/> 写入 Debug / 预留存档字段。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 上。</para>
/// </remarks>
public class BossRunStatsBridge : MonoBehaviour, IGameSystem
{
    private int bossKillCount;
    private bool isInitialized;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>本局 Boss 累计击杀数。</summary>
    public int BossKillCount => bossKillCount;

    /// <summary>订阅 Boss 击败与游戏开始事件。</summary>
    public void Initialize()
    {
        GameEvents.SubscribeBossDefeated(OnBossDefeated);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        isInitialized = true;
    }

    /// <summary>每帧更新（当前无逻辑）。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消订阅并重置初始化状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeBossDefeated(OnBossDefeated);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        isInitialized = false;
    }

    /// <summary>新局开始时重置击杀计数。</summary>
    /// <param name="ctx">游戏开始事件上下文。</param>
    private void OnGameStarted(GameEventContext ctx)
    {
        bossKillCount = 0;
    }

    /// <summary>Boss 击败回调：累计击杀并输出日志。</summary>
    /// <param name="ctx">Boss 击败事件上下文。</param>
    private void OnBossDefeated(GameEventContext ctx)
    {
        if (ctx.Payload is not BossDefeatedEventArgs args)
        {
            return;
        }

        bossKillCount++;
        Debug.Log(
            $"[BossRunStatsBridge] Boss 击杀累计={bossKillCount} id={args.BossConfigId} drop={args.DropTableId}");
    }
}
