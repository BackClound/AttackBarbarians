/// <summary>
/// 每日签到状态变更事件负载（用于 UI 红点刷新）。
/// </summary>
public readonly struct DailyRewardStateChangedEventArgs
{
    public bool CanClaimToday { get; }
    public int StreakDay { get; }
    public int NextClaimDay { get; }

    public DailyRewardStateChangedEventArgs(bool canClaimToday, int streakDay, int nextClaimDay)
    {
        CanClaimToday = canClaimToday;
        StreakDay = streakDay;
        NextClaimDay = nextClaimDay;
    }
}
