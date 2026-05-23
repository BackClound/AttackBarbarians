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

    public bool IsInitialized => isInitialized;
    public int BossKillCount => bossKillCount;

    public void Initialize()
    {
        GameEvents.SubscribeBossDefeated(OnBossDefeated);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeBossDefeated(OnBossDefeated);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        isInitialized = false;
    }

    private void OnGameStarted(GameEventContext ctx)
    {
        bossKillCount = 0;
    }

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
