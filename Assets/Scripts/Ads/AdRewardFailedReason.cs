/// <summary>
/// 激励广告请求失败原因。
/// </summary>
public enum AdRewardFailedReason
{
    /// <summary>无失败（默认值）。</summary>
    None = 0,

    /// <summary>广告服务尚未初始化。</summary>
    NotInitialized = 1,

    /// <summary>广告 SDK 或广告位未就绪。</summary>
    NotReady = 2,

    /// <summary>已有广告正在播放。</summary>
    AlreadyShowing = 3,

    /// <summary>广告素材加载失败。</summary>
    LoadFailed = 4,

    /// <summary>广告展示失败。</summary>
    ShowFailed = 5,

    /// <summary>用户跳过或未完整观看。</summary>
    Skipped = 6,

    /// <summary>观看成功但业务发奖校验失败。</summary>
    RewardValidationFailed = 7,

    /// <summary>当前业务上下文不允许播放（如限购已满）。</summary>
    ContextNotAllowed = 8,
}
