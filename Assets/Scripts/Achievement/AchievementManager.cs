using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 成就管理：监听战斗/资源事件累计进度，校验并发放成就奖励。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 ShopManager 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c>。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;AchievementManager&gt;()</c>。</para>
/// </remarks>
public class AchievementManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private AchievementCatalogSO catalog;

    private SaveManager saveManager;
    private ResourceManager resourceManager;
    private ConfigManager configManager;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public AchievementCatalogSO Catalog => catalog;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (catalog == null)
        {
            catalog = Resources.Load<AchievementCatalogSO>(GameConstants.ResourcePaths.AchievementCatalog);
        }

        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out resourceManager);
        ServiceLocator.TryGet(out configManager);

        SubscribeEvents();
        EvaluateAllAchievements();
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        UnsubscribeEvents();
        isInitialized = false;
    }

    public IReadOnlyList<AchievementDataSO> GetAchievements()
    {
        return catalog != null ? catalog.Achievements : Array.Empty<AchievementDataSO>();
    }

    public int GetProgress(string configId)
    {
        if (!TryResolveAchievement(configId, out AchievementDataSO achievement))
        {
            return 0;
        }

        return GetProgressForAchievement(achievement);
    }

    public bool IsCompleted(string configId)
    {
        if (!TryResolveAchievement(configId, out AchievementDataSO achievement))
        {
            return false;
        }

        return GetProgressForAchievement(achievement) >= achievement.TargetValue;
    }

    public bool IsClaimed(string configId)
    {
        if (saveManager?.Current == null || string.IsNullOrWhiteSpace(configId))
        {
            return false;
        }

        return ConfigIdIntPairListUtility.GetValue(saveManager.Current.achievementClaimed, configId) > 0;
    }

    public bool CanClaim(string configId, out string failureReason, out AchievementClaimFailedReason reason)
    {
        failureReason = null;
        reason = AchievementClaimFailedReason.None;

        if (!isInitialized || saveManager?.Current == null)
        {
            failureReason = "成就系统未就绪";
            reason = AchievementClaimFailedReason.NotInitialized;
            return false;
        }

        if (!TryResolveAchievement(configId, out AchievementDataSO achievement))
        {
            failureReason = $"未找到成就: {configId}";
            reason = AchievementClaimFailedReason.NotFound;
            return false;
        }

        if (IsClaimed(configId))
        {
            failureReason = "奖励已领取";
            reason = AchievementClaimFailedReason.AlreadyClaimed;
            return false;
        }

        if (!IsCompleted(configId))
        {
            failureReason = "成就尚未完成";
            reason = AchievementClaimFailedReason.NotCompleted;
            return false;
        }

        if (achievement.RewardAmount <= 0)
        {
            failureReason = "奖励配置无效";
            reason = AchievementClaimFailedReason.InvalidConfiguration;
            return false;
        }

        return true;
    }

    public bool HasClaimableRewards()
    {
        if (!isInitialized || catalog?.Achievements == null)
        {
            return false;
        }

        IReadOnlyList<AchievementDataSO> list = catalog.Achievements;
        for (int i = 0; i < list.Count; i++)
        {
            AchievementDataSO achievement = list[i];
            if (achievement == null)
            {
                continue;
            }

            if (IsCompleted(achievement.ConfigId) && !IsClaimed(achievement.ConfigId))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryClaim(string configId)
    {
        if (!CanClaim(configId, out string failureReason, out AchievementClaimFailedReason reason))
        {
            LogFailure(configId, failureReason, reason);
            GameEvents.RaiseAchievementClaimFailed(this, new AchievementClaimFailedEventArgs(configId, reason, failureReason));
            return false;
        }

        TryResolveAchievement(configId, out AchievementDataSO achievement);

        if (!GrantReward(achievement))
        {
            failureReason = "发放奖励失败";
            GameEvents.RaiseAchievementClaimFailed(
                this,
                new AchievementClaimFailedEventArgs(configId, AchievementClaimFailedReason.RewardGrantFailed, failureReason));
            return false;
        }

        ConfigIdIntPairListUtility.SetValue(saveManager.Current.achievementClaimed, configId, 1);
        saveManager.MarkDirty();

        GameEvents.RaiseAchievementClaimed(
            this,
            new AchievementClaimedEventArgs(configId, achievement.RewardType, achievement.RewardAmount));

        if (configManager != null && configManager.ShouldLog())
        {
            Debug.Log($"[AchievementManager] 领取成功 achievement={configId} reward={achievement.RewardType}x{achievement.RewardAmount}");
        }

        return true;
    }

    private void SubscribeEvents()
    {
        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.SubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.SubscribeBossDefeated(OnBossDefeated);
        GameEvents.SubscribePlayerLevelUp(OnPlayerLevelUp);
        GameEvents.SubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.SubscribeResourceChanged(OnResourceChanged);
        GameEvents.SubscribeSaveLoaded(OnSaveLoaded);
    }

    private void UnsubscribeEvents()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        GameEvents.UnsubscribeWaveCompleted(OnWaveCompleted);
        GameEvents.UnsubscribeBossDefeated(OnBossDefeated);
        GameEvents.UnsubscribePlayerLevelUp(OnPlayerLevelUp);
        GameEvents.UnsubscribeRunRewardSettled(OnRunRewardSettled);
        GameEvents.UnsubscribeResourceChanged(OnResourceChanged);
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
    }

    private void OnSaveLoaded(GameEventContext ctx) => EvaluateAllAchievements();

    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (saveManager?.Current?.statistics == null)
        {
            return;
        }

        saveManager.Current.statistics.totalKills++;
        saveManager.MarkDirty();
        EvaluateAchievementsForTarget(AchievementTargetType.TotalEnemyKills);
    }

    private void OnWaveCompleted(GameEventContext ctx)
    {
        if (saveManager?.Current?.statistics == null)
        {
            return;
        }

        saveManager.Current.statistics.totalWavesCompleted++;
        EvaluateAchievementsForTarget(AchievementTargetType.TotalWavesCompleted);
        EvaluateAchievementsForTarget(AchievementTargetType.HighestWaveReached);
    }

    private void OnBossDefeated(GameEventContext ctx)
    {
        if (saveManager?.Current?.statistics == null)
        {
            return;
        }

        saveManager.Current.statistics.totalBossDefeats++;
        saveManager.MarkDirty();
        EvaluateAchievementsForTarget(AchievementTargetType.TotalBossDefeats);
    }

    private void OnPlayerLevelUp(GameEventContext ctx)
    {
        if (ctx.Payload is not int newLevel)
        {
            return;
        }

        UpdateCounterProgress(AchievementTargetType.PlayerLevelReached, newLevel);
    }

    private void OnRunRewardSettled(GameEventContext ctx)
    {
        EvaluateAchievementsForTarget(AchievementTargetType.TotalRunsPlayed);
    }

    private void OnResourceChanged(GameEventContext ctx)
    {
        if (ctx.Payload is not ResourceChangedEventArgs change)
        {
            return;
        }

        if (change.Currency != CurrencyType.Gold || change.Delta <= 0)
        {
            return;
        }

        if (saveManager?.Current?.statistics == null)
        {
            return;
        }

        saveManager.Current.statistics.totalGoldEarned += change.Delta;
        saveManager.MarkDirty();
        EvaluateAchievementsForTarget(AchievementTargetType.LifetimeGoldEarned);
    }

    private void EvaluateAllAchievements()
    {
        if (catalog?.Achievements == null)
        {
            return;
        }

        IReadOnlyList<AchievementDataSO> list = catalog.Achievements;
        for (int i = 0; i < list.Count; i++)
        {
            AchievementDataSO achievement = list[i];
            if (achievement != null)
            {
                NotifyProgressIfChanged(achievement);
            }
        }
    }

    private void EvaluateAchievementsForTarget(AchievementTargetType targetType)
    {
        if (catalog?.Achievements == null)
        {
            return;
        }

        IReadOnlyList<AchievementDataSO> list = catalog.Achievements;
        for (int i = 0; i < list.Count; i++)
        {
            AchievementDataSO achievement = list[i];
            if (achievement != null && achievement.TargetType == targetType)
            {
                NotifyProgressIfChanged(achievement);
            }
        }
    }

    private void UpdateCounterProgress(AchievementTargetType targetType, int value)
    {
        if (catalog?.Achievements == null)
        {
            return;
        }

        IReadOnlyList<AchievementDataSO> list = catalog.Achievements;
        for (int i = 0; i < list.Count; i++)
        {
            AchievementDataSO achievement = list[i];
            if (achievement == null || achievement.TargetType != targetType)
            {
                continue;
            }

            int previous = ConfigIdIntPairListUtility.GetValue(
                saveManager.Current.achievementProgress,
                achievement.ConfigId);
            int next = Mathf.Max(previous, value);
            if (next != previous)
            {
                ConfigIdIntPairListUtility.SetValue(
                    saveManager.Current.achievementProgress,
                    achievement.ConfigId,
                    next);
                saveManager.MarkDirty();
            }

            NotifyProgressIfChanged(achievement);
        }
    }

    private void NotifyProgressIfChanged(AchievementDataSO achievement)
    {
        int progress = GetProgressForAchievement(achievement);
        bool completed = progress >= achievement.TargetValue;
        GameEvents.RaiseAchievementProgressChanged(
            this,
            new AchievementProgressChangedEventArgs(
                achievement.ConfigId,
                progress,
                achievement.TargetValue,
                completed));
    }

    private int GetProgressForAchievement(AchievementDataSO achievement)
    {
        if (achievement == null || saveManager?.Current == null)
        {
            return 0;
        }

        SaveStatisticsData stats = saveManager.Current.statistics;
        switch (achievement.TargetType)
        {
            case AchievementTargetType.TotalEnemyKills:
                return stats != null ? (int)Mathf.Min(int.MaxValue, stats.totalKills) : 0;
            case AchievementTargetType.TotalBossDefeats:
                return stats != null ? (int)Mathf.Min(int.MaxValue, stats.totalBossDefeats) : 0;
            case AchievementTargetType.HighestWaveReached:
                return stats != null ? stats.highestWave : 0;
            case AchievementTargetType.TotalWavesCompleted:
                return stats != null ? (int)Mathf.Min(int.MaxValue, stats.totalWavesCompleted) : 0;
            case AchievementTargetType.TotalRunsPlayed:
                return stats != null ? stats.totalRuns : 0;
            case AchievementTargetType.LifetimeGoldEarned:
                return stats != null ? (int)Mathf.Min(int.MaxValue, stats.totalGoldEarned) : 0;
            case AchievementTargetType.PlayerLevelReached:
                return ConfigIdIntPairListUtility.GetValue(
                    saveManager.Current.achievementProgress,
                    achievement.ConfigId);
            default:
                return ConfigIdIntPairListUtility.GetValue(
                    saveManager.Current.achievementProgress,
                    achievement.ConfigId);
        }
    }

    private bool TryResolveAchievement(string configId, out AchievementDataSO achievement)
    {
        achievement = null;
        if (catalog != null && catalog.TryGetAchievement(configId, out achievement))
        {
            return true;
        }

        if (configManager != null && configManager.TryGetAchievement(configId, out achievement))
        {
            return true;
        }

        return false;
    }

    private bool GrantReward(AchievementDataSO achievement)
    {
        CurrencyType currency = MapRewardToCurrency(achievement.RewardType);
        return resourceManager.TryAdd(
            currency,
            achievement.RewardAmount,
            ResourceChangeReason.AchievementReward,
            out _);
    }

    private static CurrencyType MapRewardToCurrency(ShopRewardType rewardType) =>
        rewardType switch
        {
            ShopRewardType.Gold => CurrencyType.Gold,
            ShopRewardType.Diamond => CurrencyType.Diamond,
            ShopRewardType.Energy => CurrencyType.Energy,
            _ => CurrencyType.Gold,
        };

    private void LogFailure(string configId, string message, AchievementClaimFailedReason reason)
    {
        if (configManager != null && configManager.ShouldLog() && !string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"[AchievementManager] {configId} 失败 ({reason}): {message}");
        }
    }

    [ContextMenu("Debug/Evaluate All Achievements")]
    private void DebugEvaluateAll() => EvaluateAllAchievements();
}
