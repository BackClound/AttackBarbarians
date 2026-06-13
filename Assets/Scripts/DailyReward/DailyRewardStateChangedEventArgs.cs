/// <summary>
/// 每日签到状态变更事件负载（用于 UI 红点刷新）。
/// </summary>
public readonly struct DailyRewardStateChangedEventArgs
{
    /// <summary>今日是否可签到。</summary>
    public bool CanClaimToday { get; }
    /// <summary>当前连续签到天数。</summary>
    public int StreakDay { get; }
    /// <summary>下一次应签到的天数索引。</summary>
    public int NextClaimDay { get; }

    /// <summary>
    /// 创建每日签到状态变更事件负载。
    /// </summary>
    /// <param name="canClaimToday">今日是否可签到。</param>
    /// <param name="streakDay">当前连续签到天数。</param>
    /// <param name="nextClaimDay">下一次应签到的天数索引。</param>
    public DailyRewardStateChangedEventArgs(bool canClaimToday, int streakDay, int nextClaimDay)
    {
        CanClaimToday = canClaimToday;
        StreakDay = streakDay;
        NextClaimDay = nextClaimDay;
    }
}
