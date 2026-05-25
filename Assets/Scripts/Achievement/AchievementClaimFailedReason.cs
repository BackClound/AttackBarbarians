/// <summary>
/// 成就领取失败原因。
/// </summary>
public enum AchievementClaimFailedReason
{
    None = 0,
    NotInitialized = 1,
    NotFound = 2,
    NotCompleted = 3,
    AlreadyClaimed = 4,
    InvalidConfiguration = 5,
    RewardGrantFailed = 6,
}
