/// <summary>
/// 激励广告请求失败原因。
/// </summary>
public enum AdRewardFailedReason
{
    None = 0,
    NotInitialized = 1,
    NotReady = 2,
    AlreadyShowing = 3,
    LoadFailed = 4,
    ShowFailed = 5,
    Skipped = 6,
    RewardValidationFailed = 7,
    ContextNotAllowed = 8,
}
