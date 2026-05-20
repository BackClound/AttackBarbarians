using UnityEngine;

/// <summary>
/// 击杀敌人时授予玩家经验并处理升级；不处理局末金币/钻石结算。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 上。</para>
/// </remarks>
public class PlayerExperienceService : MonoBehaviour, IGameSystem
{
    private PlayerController playerController;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = false;
    }

    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (ctx.Payload is not EnemyEventArgs args || args.ExperienceReward <= 0)
        {
            return;
        }

        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerController == null || !playerController.IsReady)
        {
            return;
        }

        playerController.GrantExperience(args.ExperienceReward, ctx.Sender);
    }
}
