/// <summary>
/// 成就领取失败事件负载。
/// </summary>
public readonly struct AchievementClaimFailedEventArgs
{
    /// <summary>成就配置 ID。</summary>
    public string ConfigId { get; }
    /// <summary>失败原因枚举。</summary>
    public AchievementClaimFailedReason Reason { get; }
    /// <summary>面向 UI 的失败说明文案。</summary>
    public string Message { get; }

    /// <summary>
    /// 创建成就领取失败事件负载。
    /// </summary>
    /// <param name="configId">成就配置 ID。</param>
    /// <param name="reason">失败原因。</param>
    /// <param name="message">失败说明文案。</param>
    public AchievementClaimFailedEventArgs(string configId, AchievementClaimFailedReason reason, string message)
    {
        ConfigId = configId ?? string.Empty;
        Reason = reason;
        Message = message ?? string.Empty;
    }
}
