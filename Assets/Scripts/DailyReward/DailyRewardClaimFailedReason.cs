/// <summary>
/// 每日签到领取失败原因。
/// </summary>
public enum DailyRewardClaimFailedReason
{
    None = 0,
    NotInitialized = 1,
    AlreadyClaimedToday = 2,
    EntryNotFound = 3,
    InvalidConfiguration = 4,
    RewardGrantFailed = 5,
    InsufficientFunds = 6,
    MakeupNotAllowed = 7,
}
