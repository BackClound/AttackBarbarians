/// <summary>
/// 成就领取失败原因。
/// </summary>
public enum AchievementClaimFailedReason
{
    /// <summary>无错误。</summary>
    None = 0,
    /// <summary>成就系统尚未初始化。</summary>
    NotInitialized = 1,
    /// <summary>未找到对应成就配置。</summary>
    NotFound = 2,
    /// <summary>成就尚未完成。</summary>
    NotCompleted = 3,
    /// <summary>奖励已领取。</summary>
    AlreadyClaimed = 4,
    /// <summary>成就或奖励配置无效。</summary>
    InvalidConfiguration = 5,
    /// <summary>发放奖励失败。</summary>
    RewardGrantFailed = 6,
}
