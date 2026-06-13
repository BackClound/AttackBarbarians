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

    /// <summary>是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>在线奖励所需累计游戏时长（秒）。</summary>
    public int OnlineRewardIntervalSeconds => Mathf.Max(60, onlineRewardIntervalSeconds);
    /// <summary>离线奖励累计时长上限（秒）。</summary>
    public int OfflineRewardCapSeconds => Mathf.Max(OnlineRewardIntervalSeconds, offlineRewardCapSeconds);

    /// <summary>初始化依赖并累计离线时长。</summary>
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

    /// <summary>累计在线游戏时长并写入存档。</summary>
    /// <param name="deltaTime">距上一帧的秒数。</param>
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

    /// <summary>取消事件订阅并重置初始化状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
        isInitialized = false;
    }

    /// <summary>判断在线奖励是否可领取。</summary>
    /// <param name="remaining">剩余等待时间。</param>
    /// <returns>可领取返回 <c>true</c>。</returns>
    public bool CanClaimOnlineReward(out TimeSpan remaining)
    {
        remaining = GetOnlineRewardRemaining();
        return remaining <= TimeSpan.Zero;
    }

    /// <summary>判断离线奖励是否可领取。</summary>
    /// <param name="remaining">剩余等待时间。</param>
    /// <param name="timerText">当前离线累计时长文本。</param>
    /// <returns>可领取返回 <c>true</c>。</returns>
    public bool CanClaimOfflineReward(out TimeSpan remaining, out string timerText)
    {
        remaining = GetOfflineRewardRemaining();
        timerText = FormatDuration(GetAccumulatedOfflineSeconds());
        return remaining <= TimeSpan.Zero && GetAccumulatedOfflineSeconds() >= offlineRewardIntervalSeconds;
    }

    /// <summary>判断抽奖是否可免费进行。</summary>
    /// <param name="remaining">剩余冷却时间。</param>
    /// <returns>可抽奖返回 <c>true</c>。</returns>
    public bool CanClaimLottery(out TimeSpan remaining)
    {
        remaining = GetLotteryRemaining();
        return remaining <= TimeSpan.Zero;
    }

    /// <summary>判断关卡/阶段奖励是否可领取。</summary>
    /// <param name="remaining">剩余冷却时间。</param>
    /// <returns>可领取返回 <c>true</c>。</returns>
    public bool CanClaimStageReward(out TimeSpan remaining)
    {
        remaining = GetStageRewardRemaining();
        return remaining <= TimeSpan.Zero;
    }

    /// <summary>获取在线奖励倒计时展示文本。</summary>
    /// <returns>倒计时文本（可领取时为 "00:00:00"）。</returns>
    public string GetOnlineTimerText()
    {
        TimeSpan remaining = GetOnlineRewardRemaining();
        return remaining <= TimeSpan.Zero ? "00:00:00" : FormatDuration((int)remaining.TotalSeconds);
    }

    /// <summary>获取离线累计时长展示文本。</summary>
    /// <returns>时长文本（HH:MM:SS）。</returns>
    public string GetOfflineTimerText()
    {
        return FormatDuration(GetAccumulatedOfflineSeconds());
    }

    /// <summary>获取抽奖冷却倒计时展示文本。</summary>
    /// <returns>倒计时文本；可免费时返回 "免费"。</returns>
    public string GetLotteryTimerText()
    {
        TimeSpan remaining = GetLotteryRemaining();
        return remaining <= TimeSpan.Zero ? "免费" : FormatDuration((int)remaining.TotalSeconds);
    }

    /// <summary>获取关卡/阶段奖励冷却展示文本。</summary>
    /// <returns>倒计时文本；可领取时返回 "可领取"。</returns>
    public string GetStageRewardTimerText()
    {
        TimeSpan remaining = GetStageRewardRemaining();
        return remaining <= TimeSpan.Zero ? "可领取" : FormatDuration((int)remaining.TotalSeconds);
    }

    /// <summary>领取在线时长奖励。</summary>
    /// <returns>领取成功返回 <c>true</c>。</returns>
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

    /// <summary>领取离线巡逻奖励。</summary>
    /// <returns>领取成功返回 <c>true</c>。</returns>
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

    /// <summary>执行免费抽奖并发放奖励。</summary>
    /// <returns>抽奖成功返回 <c>true</c>。</returns>
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

    /// <summary>领取关卡/阶段奖励。</summary>
    /// <returns>领取成功返回 <c>true</c>。</returns>
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

    /// <summary>记录会话结束时间（用于离线时长计算）。</summary>
    public void RecordSessionEnd()
    {
        if (saveManager?.Current == null)
        {
            return;
        }

        saveManager.Current.lastSessionEndUtcTicks = DateTime.UtcNow.Ticks;
        saveManager.MarkDirty();
    }

    /// <summary>存档加载后重新累计离线时长。</summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnSaveLoaded(GameEventContext ctx) => AccumulateOfflineTimeOnLoad();

    /// <summary>根据上次会话结束时间计算并写入离线累计时长。</summary>
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

    /// <summary>获取当前累计离线时长（秒）。</summary>
    /// <returns>离线秒数。</returns>
    private int GetAccumulatedOfflineSeconds()
    {
        if (saveManager?.Current == null)
        {
            return 0;
        }

        return Mathf.Clamp(saveManager.Current.accumulatedOfflineSeconds, 0, OfflineRewardCapSeconds);
    }

    /// <summary>计算在线奖励剩余等待时间。</summary>
    /// <returns>剩余时间；可领取时返回 <see cref="TimeSpan.Zero"/>。</returns>
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

    /// <summary>计算离线奖励剩余等待时间。</summary>
    /// <returns>剩余时间；可领取时返回 <see cref="TimeSpan.Zero"/>。</returns>
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

    /// <summary>计算抽奖剩余冷却时间。</summary>
    /// <returns>剩余时间；可抽奖时返回 <see cref="TimeSpan.Zero"/>。</returns>
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

    /// <summary>计算关卡/阶段奖励剩余冷却时间。</summary>
    /// <returns>剩余时间；可领取时返回 <see cref="TimeSpan.Zero"/>。</returns>
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

    /// <summary>从指定奖池发放升级卡奖励。</summary>
    /// <param name="poolId">奖池配置 ID。</param>
    /// <param name="drawCount">抽取次数。</param>
    /// <param name="source">奖励来源。</param>
    /// <returns>发放成功返回 <c>true</c>。</returns>
    private bool GrantPool(string poolId, int drawCount, UpgradeCardRewardSource source)
    {
        if (upgradeCardManager == null)
        {
            Debug.LogWarning("[MetaRewardService] UpgradeCardManager 未就绪");
            return false;
        }

        return upgradeCardManager.TryGrantFromPool(poolId, drawCount, source, out _);
    }

    /// <summary>将秒数格式化为 HH:MM:SS 文本。</summary>
    /// <param name="totalSeconds">总秒数。</param>
    /// <returns>格式化后的时长文本。</returns>
    private static string FormatDuration(int totalSeconds)
    {
        totalSeconds = Mathf.Max(0, totalSeconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        return $"{hours:00}:{minutes:00}:{seconds:00}";
    }
}
