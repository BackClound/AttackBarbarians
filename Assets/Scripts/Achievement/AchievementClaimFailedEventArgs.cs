/// <summary>
/// 成就领取失败事件负载。
/// </summary>
public readonly struct AchievementClaimFailedEventArgs
{
    public string ConfigId { get; }
    public AchievementClaimFailedReason Reason { get; }
    public string Message { get; }

    public AchievementClaimFailedEventArgs(string configId, AchievementClaimFailedReason reason, string message)
    {
        ConfigId = configId ?? string.Empty;
        Reason = reason;
        Message = message ?? string.Empty;
    }
}
