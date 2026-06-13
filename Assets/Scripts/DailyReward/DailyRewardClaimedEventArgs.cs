/// <summary>
/// 每日签到领取成功事件负载。
/// </summary>
public readonly struct DailyRewardClaimedEventArgs
{
    /// <summary>签到天数索引（1～7）。</summary>
    public int DayIndex { get; }
    /// <summary>发放的奖励类型。</summary>
    public ShopRewardType RewardType { get; }
    /// <summary>发放的奖励数量。</summary>
    public long RewardAmount { get; }
    /// <summary>领取后的连续签到天数。</summary>
    public int StreakDay { get; }
    /// <summary>是否为补签。</summary>
    public bool IsMakeup { get; }

    /// <summary>
    /// 创建每日签到领取成功事件负载。
    /// </summary>
    /// <param name="dayIndex">签到天数索引（1～7）。</param>
    /// <param name="rewardType">奖励类型。</param>
    /// <param name="rewardAmount">奖励数量。</param>
    /// <param name="streakDay">领取后的连续签到天数。</param>
    /// <param name="isMakeup">是否为补签。</param>
    public DailyRewardClaimedEventArgs(
        int dayIndex,
        ShopRewardType rewardType,
        long rewardAmount,
        int streakDay,
        bool isMakeup)
    {
        DayIndex = dayIndex;
        RewardType = rewardType;
        RewardAmount = rewardAmount;
        StreakDay = streakDay;
        IsMakeup = isMakeup;
    }
}
