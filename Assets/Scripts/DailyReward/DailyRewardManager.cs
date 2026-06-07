using System;
using UnityEngine;

/// <summary>
/// 每日签到管理：本地日期校验、连续签到、补签与奖励发放。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 AchievementManager 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c>。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;DailyRewardManager&gt;()</c>。</para>
/// <para><b>时间源：</b>当前使用 UTC 日期；后续可替换为 <see cref="GetTodayUtc"/> 的服务器时间实现。</para>
/// </remarks>
public class DailyRewardManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private DailyRewardCatalogSO catalog;

    private SaveManager saveManager;
    private ResourceManager resourceManager;
    private ConfigManager configManager;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public DailyRewardCatalogSO Catalog => catalog;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (catalog == null)
        {
            catalog = Resources.Load<DailyRewardCatalogSO>(GameConstants.ResourcePaths.DailyRewardCatalog);
        }

        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out resourceManager);
        ServiceLocator.TryGet(out configManager);

        GameEvents.SubscribeSaveLoaded(OnSaveLoaded);
        PublishStateChanged();
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
        isInitialized = false;
    }

    public int GetStreakDay()
    {
        return saveManager?.Current != null ? Mathf.Max(0, saveManager.Current.dailyRewardStreak) : 0;
    }

    public bool HasClaimedToday()
    {
        if (saveManager?.Current == null)
        {
            return false;
        }

        long lastTicks = saveManager.Current.lastDailyRewardClaimUtcTicks;
        if (lastTicks <= 0)
        {
            return false;
        }

        DateTime lastDate = new DateTime(lastTicks, DateTimeKind.Utc).Date;
        return lastDate == GetTodayUtc().Date;
    }

    public bool CanClaimToday()
    {
        if (!isInitialized || saveManager?.Current == null || catalog == null)
        {
            return false;
        }

        if (HasClaimedToday())
        {
            return false;
        }

        return catalog.TryGetEntry(ResolveNextClaimDay(), out _);
    }

    public int ResolveNextClaimDay()
    {
        if (saveManager?.Current == null || catalog == null)
        {
            return 1;
        }

        SaveData save = saveManager.Current;
        long lastTicks = save.lastDailyRewardClaimUtcTicks;
        if (lastTicks <= 0)
        {
            return 1;
        }

        DateTime lastDate = new DateTime(lastTicks, DateTimeKind.Utc).Date;
        DateTime today = GetTodayUtc().Date;
        if (lastDate == today)
        {
            return save.dailyRewardStreak;
        }

        if (lastDate == today.AddDays(-1))
        {
            int next = save.dailyRewardStreak + 1;
            return next > catalog.MaxDay ? 1 : next;
        }

        return 1;
    }

    public bool TryGetEntry(int dayIndex, out DailyRewardEntrySO entry)
    {
        entry = null;
        if (catalog != null && catalog.TryGetEntry(dayIndex, out entry))
        {
            return true;
        }

        if (configManager != null && configManager.TryGetDailyRewardEntry(dayIndex, out entry))
        {
            return true;
        }

        return false;
    }

    public bool TryClaimToday()
    {
        int dayToClaim = ResolveNextClaimDay();
        return TryClaimDay(dayToClaim, isMakeup: false);
    }

    public bool TryMakeupClaim(int dayIndex)
    {
        if (catalog == null || !catalog.AllowMakeup)
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.MakeupNotAllowed, "当前配置不允许补签");
            return false;
        }

        if (dayIndex < 1 || dayIndex >= ResolveNextClaimDay())
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.MakeupNotAllowed, "只能补签已错过的天数");
            return false;
        }

        if (catalog.MakeupDiamondCost > 0 &&
            !resourceManager.CanAfford(CurrencyType.Diamond, catalog.MakeupDiamondCost))
        {
            string message = ResourceManager.FormatInsufficientFunds(
                CurrencyType.Diamond,
                catalog.MakeupDiamondCost,
                resourceManager.GetAmount(CurrencyType.Diamond));
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.InsufficientFunds, message);
            return false;
        }

        if (catalog.MakeupDiamondCost > 0 &&
            !resourceManager.TrySpend(
                CurrencyType.Diamond,
                catalog.MakeupDiamondCost,
                ResourceChangeReason.DailyReward,
                out string spendFailure))
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.InsufficientFunds, spendFailure);
            return false;
        }

        return TryClaimDay(dayIndex, isMakeup: true);
    }

    private bool TryClaimDay(int dayIndex, bool isMakeup)
    {
        if (!isInitialized || saveManager?.Current == null || resourceManager == null)
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.NotInitialized, "签到系统未就绪");
            return false;
        }

        if (!isMakeup && HasClaimedToday())
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.AlreadyClaimedToday, "今日已签到");
            return false;
        }

        if (!isMakeup && dayIndex != ResolveNextClaimDay())
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.InvalidConfiguration, "签到天数不匹配");
            return false;
        }

        if (!TryGetEntry(dayIndex, out DailyRewardEntrySO entry))
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.EntryNotFound, $"未找到第 {dayIndex} 日奖励配置");
            return false;
        }

        if (entry.RewardAmount <= 0)
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.InvalidConfiguration, "奖励配置无效");
            return false;
        }

        if (!GrantReward(entry))
        {
            RaiseFailed(dayIndex, DailyRewardClaimFailedReason.RewardGrantFailed, "发放奖励失败");
            return false;
        }

        SaveData save = saveManager.Current;
        if (!isMakeup)
        {
            save.dailyRewardStreak = dayIndex;
            save.lastDailyRewardClaimUtcTicks = DateTime.UtcNow.Ticks;
        }

        saveManager.MarkDirty();

        GameEvents.RaiseDailyRewardClaimed(
            this,
            new DailyRewardClaimedEventArgs(
                dayIndex,
                entry.RewardType,
                entry.RewardAmount,
                save.dailyRewardStreak,
                isMakeup));

        PublishStateChanged();

        if (configManager != null && configManager.ShouldLog())
        {
            Debug.Log(
                $"[DailyRewardManager] 签到成功 day={dayIndex} makeup={isMakeup} " +
                $"reward={entry.RewardType}x{entry.RewardAmount}");
        }

        return true;
    }

    private bool GrantReward(DailyRewardEntrySO entry)
    {
        CurrencyType currency = MapRewardToCurrency(entry.RewardType);
        return resourceManager.TryAdd(
            currency,
            entry.RewardAmount,
            ResourceChangeReason.DailyReward,
            out _);
    }

    private void OnSaveLoaded(GameEventContext ctx) => PublishStateChanged();

    private void PublishStateChanged()
    {
        GameEvents.RaiseDailyRewardStateChanged(
            this,
            new DailyRewardStateChangedEventArgs(
                CanClaimToday(),
                GetStreakDay(),
                ResolveNextClaimDay()));
    }

    private void RaiseFailed(int dayIndex, DailyRewardClaimFailedReason reason, string message)
    {
        if (configManager != null && configManager.ShouldLog() && !string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"[DailyRewardManager] day={dayIndex} 失败 ({reason}): {message}");
        }

        GameEvents.RaiseDailyRewardClaimFailed(
            this,
            new DailyRewardClaimFailedEventArgs(dayIndex, reason, message));
    }

    /// <summary>预留服务器时间接口：当前返回 UTC 日期。</summary>
    public static DateTime GetTodayUtc() => DateTime.UtcNow;

    private static CurrencyType MapRewardToCurrency(ShopRewardType rewardType) =>
        rewardType switch
        {
            ShopRewardType.Gold => CurrencyType.Gold,
            ShopRewardType.Diamond => CurrencyType.Diamond,
            ShopRewardType.Energy => CurrencyType.Energy,
            _ => CurrencyType.Gold,
        };

    [ContextMenu("Debug/Try Claim Today")]
    private void DebugTryClaimToday() => TryClaimToday();
}
