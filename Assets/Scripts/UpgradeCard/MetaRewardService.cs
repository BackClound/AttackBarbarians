using System;
using UnityEngine;

/// <summary>
/// Meta 奖励服务：在线时长、离线巡逻、抽奖、通关奖励等升级卡发放。
/// </summary>
/// <remarks>
/// <para><b>挂载：</b><c>GameSystems</c>。</para>
/// </remarks>
public class MetaRewardService : MonoBehaviour, IGameSystem
{
    [Header("Timing (seconds)")]
    [SerializeField] private int onlineRewardIntervalSeconds = 900;
    [SerializeField] private int offlineRewardIntervalSeconds = 28800;
    [SerializeField] private int offlineRewardCapSeconds = 28800;
    [SerializeField] private int lotteryCooldownSeconds = 3600;
    [SerializeField] private int stageRewardIntervalSeconds = 86400;

    [Header("Pools")]
    [SerializeField] private string onlineRewardPoolId = UpgradeCardConstants.PoolIds.OnlineReward;
    [SerializeField] private string offlineRewardPoolId = UpgradeCardConstants.PoolIds.OfflineReward;
    [SerializeField] private string lotteryPoolId = UpgradeCardConstants.PoolIds.Lottery;
    [SerializeField] private string stageRewardPoolId = UpgradeCardConstants.PoolIds.StageReward;

    private SaveManager saveManager;
    private UpgradeCardManager upgradeCardManager;
    private bool isInitialized;
    private float onlineTickAccumulator;

    public bool IsInitialized => isInitialized;
    public int OnlineRewardIntervalSeconds => Mathf.Max(60, onlineRewardIntervalSeconds);
    public int OfflineRewardCapSeconds => Mathf.Max(OnlineRewardIntervalSeconds, offlineRewardCapSeconds);

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out upgradeCardManager);
        GameEvents.SubscribeSaveLoaded(OnSaveLoaded);
        AccumulateOfflineTimeOnLoad();
        isInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!isInitialized || saveManager?.Current == null)
        {
            return;
        }

        onlineTickAccumulator += deltaTime;
        if (onlineTickAccumulator < 1f)
        {
            return;
        }

        int seconds = Mathf.FloorToInt(onlineTickAccumulator);
        onlineTickAccumulator -= seconds;
        saveManager.Current.onlinePlayTimeSeconds += seconds;
        saveManager.MarkDirty();
    }

    public void Shutdown()
    {
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
        isInitialized = false;
    }

    public bool CanClaimOnlineReward(out TimeSpan remaining)
    {
        remaining = GetOnlineRewardRemaining();
        return remaining <= TimeSpan.Zero;
    }

    public bool CanClaimOfflineReward(out TimeSpan remaining, out string timerText)
    {
        remaining = GetOfflineRewardRemaining();
        timerText = FormatDuration(GetAccumulatedOfflineSeconds());
        return remaining <= TimeSpan.Zero && GetAccumulatedOfflineSeconds() >= offlineRewardIntervalSeconds;
    }

    public bool CanClaimLottery(out TimeSpan remaining)
    {
        remaining = GetLotteryRemaining();
        return remaining <= TimeSpan.Zero;
    }

    public bool CanClaimStageReward(out TimeSpan remaining)
    {
        remaining = GetStageRewardRemaining();
        return remaining <= TimeSpan.Zero;
    }

    public string GetOnlineTimerText()
    {
        TimeSpan remaining = GetOnlineRewardRemaining();
        return remaining <= TimeSpan.Zero ? "00:00:00" : FormatDuration((int)remaining.TotalSeconds);
    }

    public string GetOfflineTimerText()
    {
        return FormatDuration(GetAccumulatedOfflineSeconds());
    }

    public string GetLotteryTimerText()
    {
        TimeSpan remaining = GetLotteryRemaining();
        return remaining <= TimeSpan.Zero ? "免费" : FormatDuration((int)remaining.TotalSeconds);
    }

    public string GetStageRewardTimerText()
    {
        TimeSpan remaining = GetStageRewardRemaining();
        return remaining <= TimeSpan.Zero ? "可领取" : FormatDuration((int)remaining.TotalSeconds);
    }

    public bool TryClaimOnlineReward()
    {
        if (!CanClaimOnlineReward(out _))
        {
            return false;
        }

        if (!GrantPool(onlineRewardPoolId, 1, UpgradeCardRewardSource.OnlineReward))
        {
            return false;
        }

        SaveData save = saveManager.Current;
        save.onlinePlayTimeSeconds = 0;
        save.lastOnlineRewardClaimUtcTicks = DateTime.UtcNow.Ticks;
        saveManager.MarkDirty();
        return true;
    }

    public bool TryClaimOfflineReward()
    {
        if (!CanClaimOfflineReward(out _, out _))
        {
            return false;
        }

        if (!GrantPool(offlineRewardPoolId, 1, UpgradeCardRewardSource.OfflineReward))
        {
            return false;
        }

        SaveData save = saveManager.Current;
        save.lastOfflineRewardClaimUtcTicks = DateTime.UtcNow.Ticks;
        save.lastSessionEndUtcTicks = DateTime.UtcNow.Ticks;
        saveManager.MarkDirty();
        return true;
    }

    public bool TryClaimLottery()
    {
        if (!CanClaimLottery(out _))
        {
            return false;
        }

        if (!GrantPool(lotteryPoolId, 1, UpgradeCardRewardSource.Lottery))
        {
            return false;
        }

        saveManager.Current.lastLotteryUtcTicks = DateTime.UtcNow.Ticks;
        saveManager.MarkDirty();
        return true;
    }

    public bool TryClaimStageReward()
    {
        if (!CanClaimStageReward(out _))
        {
            return false;
        }

        if (!GrantPool(stageRewardPoolId, 1, UpgradeCardRewardSource.StageReward))
        {
            return false;
        }

        saveManager.Current.lastStageRewardClaimUtcTicks = DateTime.UtcNow.Ticks;
        saveManager.MarkDirty();
        return true;
    }

    public void RecordSessionEnd()
    {
        if (saveManager?.Current == null)
        {
            return;
        }

        saveManager.Current.lastSessionEndUtcTicks = DateTime.UtcNow.Ticks;
        saveManager.MarkDirty();
    }

    private void OnSaveLoaded(GameEventContext ctx) => AccumulateOfflineTimeOnLoad();

    private void AccumulateOfflineTimeOnLoad()
    {
        if (saveManager?.Current == null)
        {
            return;
        }

        SaveData save = saveManager.Current;
        if (save.lastSessionEndUtcTicks <= 0)
        {
            save.lastSessionEndUtcTicks = DateTime.UtcNow.Ticks;
            return;
        }

        DateTime lastEnd = new DateTime(save.lastSessionEndUtcTicks, DateTimeKind.Utc);
        int offlineSeconds = Mathf.Clamp(
            (int)(DateTime.UtcNow - lastEnd).TotalSeconds,
            0,
            OfflineRewardCapSeconds);
        save.accumulatedOfflineSeconds = offlineSeconds;
    }

    private int GetAccumulatedOfflineSeconds()
    {
        if (saveManager?.Current == null)
        {
            return 0;
        }

        return Mathf.Clamp(saveManager.Current.accumulatedOfflineSeconds, 0, OfflineRewardCapSeconds);
    }

    private TimeSpan GetOnlineRewardRemaining()
    {
        if (saveManager?.Current == null)
        {
            return TimeSpan.FromSeconds(OnlineRewardIntervalSeconds);
        }

        int elapsed = saveManager.Current.onlinePlayTimeSeconds;
        int remaining = OnlineRewardIntervalSeconds - elapsed;
        return remaining > 0 ? TimeSpan.FromSeconds(remaining) : TimeSpan.Zero;
    }

    private TimeSpan GetOfflineRewardRemaining()
    {
        if (saveManager?.Current == null)
        {
            return TimeSpan.FromSeconds(offlineRewardIntervalSeconds);
        }

        SaveData save = saveManager.Current;
        if (save.lastOfflineRewardClaimUtcTicks <= 0)
        {
            int offline = GetAccumulatedOfflineSeconds();
            int remaining = offlineRewardIntervalSeconds - offline;
            return remaining > 0 ? TimeSpan.FromSeconds(remaining) : TimeSpan.Zero;
        }

        DateTime lastClaim = new DateTime(save.lastOfflineRewardClaimUtcTicks, DateTimeKind.Utc);
        DateTime readyAt = lastClaim.AddSeconds(offlineRewardIntervalSeconds);
        if (DateTime.UtcNow >= readyAt)
        {
            return TimeSpan.Zero;
        }

        return readyAt - DateTime.UtcNow;
    }

    private TimeSpan GetLotteryRemaining()
    {
        if (saveManager?.Current == null || saveManager.Current.lastLotteryUtcTicks <= 0)
        {
            return TimeSpan.Zero;
        }

        DateTime last = new DateTime(saveManager.Current.lastLotteryUtcTicks, DateTimeKind.Utc);
        DateTime readyAt = last.AddSeconds(lotteryCooldownSeconds);
        return DateTime.UtcNow >= readyAt ? TimeSpan.Zero : readyAt - DateTime.UtcNow;
    }

    private TimeSpan GetStageRewardRemaining()
    {
        if (saveManager?.Current == null || saveManager.Current.lastStageRewardClaimUtcTicks <= 0)
        {
            return TimeSpan.Zero;
        }

        DateTime last = new DateTime(saveManager.Current.lastStageRewardClaimUtcTicks, DateTimeKind.Utc);
        DateTime readyAt = last.AddSeconds(stageRewardIntervalSeconds);
        return DateTime.UtcNow >= readyAt ? TimeSpan.Zero : readyAt - DateTime.UtcNow;
    }

    private bool GrantPool(string poolId, int drawCount, UpgradeCardRewardSource source)
    {
        if (upgradeCardManager == null)
        {
            Debug.LogWarning("[MetaRewardService] UpgradeCardManager 未就绪");
            return false;
        }

        return upgradeCardManager.TryGrantFromPool(poolId, drawCount, source, out _);
    }

    private static string FormatDuration(int totalSeconds)
    {
        totalSeconds = Mathf.Max(0, totalSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
}
