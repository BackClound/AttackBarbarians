/// <summary>
/// 每日签到领取失败事件负载。
/// </summary>
public readonly struct DailyRewardClaimFailedEventArgs
{
    public int DayIndex { get; }
    public DailyRewardClaimFailedReason Reason { get; }
    public string Message { get; }

    public DailyRewardClaimFailedEventArgs(int dayIndex, DailyRewardClaimFailedReason reason, string message)
    {
        DayIndex = dayIndex;
        Reason = reason;
        Message = message ?? string.Empty;
    }
}
