/// <summary>
/// 每日签到领取失败原因。
/// </summary>
public enum DailyRewardClaimFailedReason
{
    /// <summary>无错误。</summary>
    None = 0,
    /// <summary>签到系统尚未初始化。</summary>
    NotInitialized = 1,
    /// <summary>今日已签到。</summary>
    AlreadyClaimedToday = 2,
    /// <summary>未找到对应天数的奖励配置。</summary>
    EntryNotFound = 3,
    /// <summary>签到或奖励配置无效。</summary>
    InvalidConfiguration = 4,
    /// <summary>发放奖励失败。</summary>
    RewardGrantFailed = 5,
    /// <summary>补签所需资源不足。</summary>
    InsufficientFunds = 6,
    /// <summary>不允许补签或补签条件不满足。</summary>
    MakeupNotAllowed = 7,
}
