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

    /// <summary>系统是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>订阅击杀事件并缓存玩家控制器。</summary>
    public void Initialize()
    {
        PlayerSceneAccess.TryGetController(out playerController);
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = true;
    }

    /// <summary>每帧 Tick（本服务无逐帧逻辑）。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消订阅并重置状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = false;
    }

    /// <summary>敌人击杀回调：向玩家授予经验。</summary>
    /// <param name="ctx">击杀事件上下文。</param>
    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (ctx.Payload is not EnemyEventArgs args || args.ExperienceReward <= 0)
        {
            return;
        }

        if (playerController == null)
        {
            PlayerSceneAccess.TryGetController(out playerController);
        }

        if (playerController == null || !playerController.IsReady)
        {
            return;
        }

        playerController.GrantExperience(args.ExperienceReward, ctx.Sender);
    }
}
