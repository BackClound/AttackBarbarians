using System;
using UnityEngine;

/// <summary>
/// Meta 每日签到管理器：负责连续签到、断签重置、补签与奖励发放。
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

    /// <summary>签到服务是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前绑定的签到目录配置。</summary>
    public DailyRewardCatalogSO Catalog => catalog;

    /// <summary>
    /// 初始化签到系统：加载目录、订阅存档事件并广播当前状态。
    /// </summary>
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

    /// <summary>
    /// 每帧更新（签到系统无逐帧逻辑）。
    /// </summary>
    /// <param name="deltaTime">距上一帧的时间间隔（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭签到系统并取消事件订阅。
    /// </summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
        isInitialized = false;
    }

    /// <summary>
    /// 获取当前连续签到天数。
    /// </summary>
    /// <returns>连续签到天数；存档未就绪时返回 0。</returns>
    public int GetStreakDay()
    {
        return saveManager?.Current != null ? Mathf.Max(0, saveManager.Current.dailyRewardStreak) : 0;
    }

    /// <summary>
    /// 判断今日是否已签到。
    /// </summary>
    /// <returns>今日已签到时返回 true。</returns>
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

    /// <summary>
    /// 判断今日是否可签到。
    /// </summary>
    /// <returns>系统就绪、今日未签且存在对应奖励配置时返回 true。</returns>
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

    /// <summary>
    /// 解析下一次应签到的天数索引（含断签重置与周期回绕）。
    /// </summary>
    /// <returns>下一次签到天数（1～7）。</returns>
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

    /// <summary>
    /// 按天数索引查找签到奖励配置。
    /// </summary>
    /// <param name="dayIndex">签到天数索引（1～7）。</param>
    /// <param name="entry">找到的奖励配置。</param>
    /// <returns>解析成功时返回 true。</returns>
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

    /// <summary>
    /// 尝试领取今日签到奖励。
    /// </summary>
    /// <returns>领取成功时返回 true。</returns>
    public bool TryClaimToday()
    {
        int dayToClaim = ResolveNextClaimDay();
        return TryClaimDay(dayToClaim, isMakeup: false);
    }

    /// <summary>
    /// 尝试补签指定天数的奖励（可能消耗钻石）。
    /// </summary>
    /// <param name="dayIndex">要补签的天数索引。</param>
    /// <returns>补签成功时返回 true。</returns>
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

    /// <summary>
    /// 领取指定天数的签到奖励（普通签到或补签）。
    /// </summary>
    /// <param name="dayIndex">签到天数索引。</param>
    /// <param name="isMakeup">是否为补签。</param>
    /// <returns>领取成功时返回 true。</returns>
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

        if (!entry.HasUpgradeCardReward && entry.RewardAmount <= 0)
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

    /// <summary>
    /// 发放单日签到奖励（货币与/或升级卡）。
    /// </summary>
    /// <param name="entry">签到奖励配置。</param>
    /// <returns>全部奖励发放成功时返回 true。</returns>
    private bool GrantReward(DailyRewardEntrySO entry)
    {
        bool granted = true;

        if (entry.RewardAmount > 0 && entry.RewardType != ShopRewardType.UpgradeCard)
        {
            CurrencyType currency = MapRewardToCurrency(entry.RewardType);
            granted = resourceManager.TryAdd(
                currency,
                entry.RewardAmount,
                ResourceChangeReason.DailyReward,
                out _);
        }

        if (entry.HasUpgradeCardReward &&
            ServiceLocator.TryGet(out UpgradeCardManager upgradeCardManager))
        {
            granted &= upgradeCardManager.TryGrantFromPool(
                entry.UpgradeCardPoolConfigId,
                entry.UpgradeCardDrawCount,
                UpgradeCardRewardSource.DailyReward,
                out _);
        }

        return granted;
    }

    /// <summary>
    /// 存档加载完成后广播签到状态变更。
    /// </summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnSaveLoaded(GameEventContext ctx) => PublishStateChanged();

    /// <summary>
    /// 广播当前签到状态（可领、连续天数、下一签到日）。
    /// </summary>
    private void PublishStateChanged()
    {
        GameEvents.RaiseDailyRewardStateChanged(
            this,
            new DailyRewardStateChangedEventArgs(
                CanClaimToday(),
                GetStreakDay(),
                ResolveNextClaimDay()));
    }

    /// <summary>
    /// 记录签到失败日志并广播失败事件。
    /// </summary>
    /// <param name="dayIndex">签到天数索引。</param>
    /// <param name="reason">失败原因。</param>
    /// <param name="message">失败说明文案。</param>
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

    /// <summary>
    /// 获取当前 UTC 日期（预留服务器时间接口）。
    /// </summary>
    /// <returns>当前 UTC 时间。</returns>
    public static DateTime GetTodayUtc() => DateTime.UtcNow;

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
    /// 调试菜单：尝试领取今日签到奖励。
    /// </summary>
    [ContextMenu("Debug/Try Claim Today")]
    private void DebugTryClaimToday() => TryClaimToday();
}
