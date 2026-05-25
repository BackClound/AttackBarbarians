/// <summary>
/// 每日签到领取成功事件负载。
/// </summary>
public readonly struct DailyRewardClaimedEventArgs
{
    public int DayIndex { get; }
    public ShopRewardType RewardType { get; }
    public long RewardAmount { get; }
    public int StreakDay { get; }
    public bool IsMakeup { get; }

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
