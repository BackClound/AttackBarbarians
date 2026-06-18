using UnityEngine;

/// <summary>
/// 击杀敌人时授予玩家经验并处理升级；V2 下额外提供时间经验。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 上。</para>
/// </remarks>
public class PlayerExperienceService : MonoBehaviour, IGameSystem
{
    private PlayerController playerController;
    private GameManager gameManager;
    private float passiveExpAccumulator;
    private bool isInitialized;

    /// <summary>系统是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>订阅击杀事件并缓存玩家控制器。</summary>
    public void Initialize()
    {
        PlayerSceneAccess.TryGetController(out playerController);
        ServiceLocator.TryGet(out gameManager);
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = true;
    }

    /// <summary>驱动被动时间经验（V2）。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void Tick(float deltaTime)
    {
        if (!isInitialized || !RunProgressionContext.IsActive || RunProgressionContext.Config == null)
        {
            return;
        }

        if (gameManager == null)
        {
            ServiceLocator.TryGet(out gameManager);
        }

        if (gameManager == null || gameManager.CurrentState != GameState.Playing)
        {
            return;
        }

        float passiveRate = RunProgressionContext.Config.PassiveExpPerSecond *
                            RunDifficultyContext.ExpGainDifficultyMult;
        if (passiveRate <= 0f)
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

        passiveExpAccumulator += passiveRate * deltaTime;
        if (passiveExpAccumulator < 1f)
        {
            return;
        }

        int grant = Mathf.FloorToInt(passiveExpAccumulator);
        passiveExpAccumulator -= grant;
        playerController.GrantExperience(grant, this);
    }

    /// <summary>取消订阅并重置状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        passiveExpAccumulator = 0f;
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
