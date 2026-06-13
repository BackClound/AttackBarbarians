using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Meta 成就管理器：监听全局统计事件累计进度，校验完成状态并发放成就奖励。
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

    /// <summary>成就服务是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前绑定的成就目录配置。</summary>
    public AchievementCatalogSO Catalog => catalog;

    /// <summary>
    /// 初始化成就系统：加载目录、订阅事件并评估当前进度。
    /// </summary>
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

    /// <summary>
    /// 每帧更新（成就系统无逐帧逻辑）。
    /// </summary>
    /// <param name="deltaTime">距上一帧的时间间隔（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭成就系统并取消事件订阅。
    /// </summary>
    public void Shutdown()
    {
        UnsubscribeEvents();
        isInitialized = false;
    }

    /// <summary>
    /// 获取全部成就配置列表。
    /// </summary>
    /// <returns>成就配置只读列表；目录未加载时返回空数组。</returns>
    public IReadOnlyList<AchievementDataSO> GetAchievements()
    {
        return catalog != null ? catalog.Achievements : Array.Empty<AchievementDataSO>();
    }

    /// <summary>
    /// 获取指定成就的当前进度值。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <returns>当前进度；未找到成就时返回 0。</returns>
    public int GetProgress(string configId)
    {
        if (!TryResolveAchievement(configId, out AchievementDataSO achievement))
        {
            return 0;
        }

        return GetProgressForAchievement(achievement);
    }

    /// <summary>
    /// 判断指定成就是否已完成。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <returns>进度达到目标值时返回 true。</returns>
    public bool IsCompleted(string configId)
    {
        if (!TryResolveAchievement(configId, out AchievementDataSO achievement))
        {
            return false;
        }

        return GetProgressForAchievement(achievement) >= achievement.TargetValue;
    }

    /// <summary>
    /// 判断指定成就的奖励是否已领取。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <returns>已领取时返回 true。</returns>
    public bool IsClaimed(string configId)
    {
        if (saveManager?.Current == null || string.IsNullOrWhiteSpace(configId))
        {
            return false;
        }

        return ConfigIdIntPairListUtility.GetValue(saveManager.Current.achievementClaimed, configId) > 0;
    }

    /// <summary>
    /// 校验指定成就是否可领取奖励。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <param name="failureReason">不可领取时的失败说明文案。</param>
    /// <param name="reason">不可领取时的失败原因枚举。</param>
    /// <returns>可领取时返回 true。</returns>
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

    /// <summary>
    /// 是否存在已完成且未领取的成就奖励。
    /// </summary>
    /// <returns>存在可领取奖励时返回 true。</returns>
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

    /// <summary>
    /// 尝试领取指定成就的奖励。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <returns>领取成功时返回 true。</returns>
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

    /// <summary>
    /// 订阅游戏事件以累计成就进度。
    /// </summary>
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

    /// <summary>
    /// 取消游戏事件订阅。
    /// </summary>
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

    /// <summary>
    /// 存档加载完成后重新评估全部成就进度。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnSaveLoaded(GameEventContext ctx) => EvaluateAllAchievements();

    /// <summary>
    /// 敌人被击杀时累计击杀统计并评估相关成就。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
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

    /// <summary>
    /// 波次完成时累计波次统计并评估相关成就。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
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

    /// <summary>
    /// Boss 被击败时累计击败统计并评估相关成就。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
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

    /// <summary>
    /// 玩家升级时更新等级类成就进度。
    /// </summary>
    /// <param name="ctx">游戏事件上下文；Payload 为新等级。</param>
    private void OnPlayerLevelUp(GameEventContext ctx)
    {
        if (ctx.Payload is not int newLevel)
        {
            return;
        }

        UpdateCounterProgress(AchievementTargetType.PlayerLevelReached, newLevel);
    }

    /// <summary>
    /// 单局结算后评估游玩次数相关成就。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnRunRewardSettled(GameEventContext ctx)
    {
        EvaluateAchievementsForTarget(AchievementTargetType.TotalRunsPlayed);
    }

    /// <summary>
    /// 金币增加时累计 lifetime 金币并评估相关成就。
    /// </summary>
    /// <param name="ctx">游戏事件上下文；Payload 为资源变更参数。</param>
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

    /// <summary>
    /// 评估目录中全部成就的进度并广播变更。
    /// </summary>
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

    /// <summary>
    /// 评估指定目标类型的全部成就进度。
    /// </summary>
    /// <param name="targetType">成就目标类型。</param>
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

    /// <summary>
    /// 更新计数型成就的存档进度（取当前值与已有进度的较大者）。
    /// </summary>
    /// <param name="targetType">成就目标类型。</param>
    /// <param name="value">新的进度值。</param>
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

    /// <summary>
    /// 广播指定成就的当前进度与完成状态。
    /// </summary>
    /// <param name="achievement">成就配置。</param>
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

    /// <summary>
    /// 根据目标类型从存档统计中读取成就进度。
    /// </summary>
    /// <param name="achievement">成就配置。</param>
    /// <returns>当前进度值。</returns>
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

    /// <summary>
    /// 从目录或配置管理器解析成就配置。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <param name="achievement">找到的成就配置。</param>
    /// <returns>解析成功时返回 true。</returns>
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

    /// <summary>
    /// 将成就奖励发放到玩家资源。
    /// </summary>
    /// <param name="achievement">成就配置。</param>
    /// <returns>发放成功时返回 true。</returns>
    private bool GrantReward(AchievementDataSO achievement)
    {
        CurrencyType currency = MapRewardToCurrency(achievement.RewardType);
        return resourceManager.TryAdd(
            currency,
            achievement.RewardAmount,
            ResourceChangeReason.AchievementReward,
            out _);
    }

    /// <summary>
    /// 将商店奖励类型映射为货币类型。
    /// </summary>
    /// <param name="rewardType">商店奖励类型。</param>
    /// <returns>对应的货币类型。</returns>
    private static CurrencyType MapRewardToCurrency(ShopRewardType rewardType) =>
        rewardType switch
        {
            ShopRewardType.Gold => CurrencyType.Gold,
            ShopRewardType.Diamond => CurrencyType.Diamond,
            ShopRewardType.Energy => CurrencyType.AdTicket,
            _ => CurrencyType.Gold,
        };

    /// <summary>
    /// 记录成就领取失败日志。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <param name="message">失败说明文案。</param>
    /// <param name="reason">失败原因。</param>
    private void LogFailure(string configId, string message, AchievementClaimFailedReason reason)
    {
        if (configManager != null && configManager.ShouldLog() && !string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"[AchievementManager] {configId} 失败 ({reason}): {message}");
        }
    }

    /// <summary>
    /// 调试菜单：重新评估全部成就进度。
    /// </summary>
    [ContextMenu("Debug/Evaluate All Achievements")]
    private void DebugEvaluateAll() => EvaluateAllAchievements();
}
