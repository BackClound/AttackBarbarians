using UnityEngine;

/// <summary>
/// 局末奖励结算：根据单局时长档位与难度发放金币/钻石（敌人死亡不掉落资源）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 上。</para>
/// </remarks>
public class RunRewardSettlementService : MonoBehaviour, IGameSystem
{
    [SerializeField] private RunRewardSettlementSO settlementConfig;

    private RunSessionTracker sessionTracker;
    private SaveManager saveManager;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    public void Initialize()
    {
        if (settlementConfig == null)
        {
            settlementConfig = Resources.Load<RunRewardSettlementSO>(GameConstants.ResourcePaths.RunRewardSettlement);
        }

        ServiceLocator.TryGet(out sessionTracker);
        if (sessionTracker == null)
        {
            sessionTracker = FindFirstObjectByType<RunSessionTracker>();
        }

        ServiceLocator.TryGet(out saveManager);
        GameEvents.SubscribeGameOver(OnGameOver);
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeGameOver(OnGameOver);
        isInitialized = false;
    }

    public RunRewardResult SettleNow(int difficultyLevel)
    {
        if (settlementConfig == null)
        {
            Debug.LogError("[RunRewardSettlementService] 缺少 RunRewardSettlementSO 配置。");
            return new RunRewardResult(0, 0, 0);
        }

        float duration = sessionTracker != null ? sessionTracker.SessionDurationSeconds : 0f;
        return settlementConfig.Calculate(duration, difficultyLevel);
    }

    private void OnGameOver(GameEventContext ctx)
    {
        if (settlementConfig == null || saveManager == null || saveManager.Current == null)
        {
            return;
        }

        int difficulty = ResolveDifficulty(saveManager.Current);
        float duration = sessionTracker != null ? sessionTracker.SessionDurationSeconds : 0f;
        RunRewardResult result = settlementConfig.Calculate(duration, difficulty);

        saveManager.Current.gold += result.Gold;
        saveManager.Current.diamonds += result.Diamonds;

        if (saveManager.Current.statistics != null)
        {
            saveManager.Current.statistics.totalRuns++;
            saveManager.Current.statistics.totalPlayTimeSeconds += Mathf.RoundToInt(duration);
        }

        GameEvents.RaiseRunRewardSettled(this, new RunRewardSettledEventArgs(
            duration,
            result.Tier,
            difficulty,
            result.Gold,
            result.Diamonds));

        saveManager.MarkDirty();
        saveManager.SaveImmediate();

        if (ServiceLocator.TryGet(out SkillUnlockService skillUnlockService))
        {
            skillUnlockService.RefreshMetaUnlocks();
        }

        if (ServiceLocator.TryGet(out ConfigManager config) && config.ShouldLog())
        {
            Debug.Log(
                $"[RunRewardSettlement] tier={result.Tier} duration={duration:F1}s " +
                $"difficulty={difficulty} gold+={result.Gold} diamonds+={result.Diamonds}");
        }
    }

    private static int ResolveDifficulty(SaveData save)
    {
        if (save?.settings == null)
        {
            return 1;
        }

        return Mathf.Clamp(save.settings.gameDifficulty, 0, 2);
    }
}
