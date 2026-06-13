/// <summary>
/// 每日签到领取失败事件负载。
/// </summary>
public readonly struct DailyRewardClaimFailedEventArgs
{
    /// <summary>签到天数索引（1～7）。</summary>
    public int DayIndex { get; }
    /// <summary>失败原因枚举。</summary>
    public DailyRewardClaimFailedReason Reason { get; }
    /// <summary>面向 UI 的失败说明文案。</summary>
    public string Message { get; }

    /// <summary>
    /// 创建每日签到领取失败事件负载。
    /// </summary>
    /// <param name="dayIndex">签到天数索引（1～7）。</param>
    /// <param name="reason">失败原因。</param>
    /// <param name="message">失败说明文案。</param>
    public DailyRewardClaimFailedEventArgs(int dayIndex, DailyRewardClaimFailedReason reason, string message)
    {
        DayIndex = dayIndex;
        Reason = reason;
        Message = message ?? string.Empty;
    }
}
