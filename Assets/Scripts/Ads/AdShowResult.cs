/// <summary>
/// 单次广告展示结果。
/// </summary>
public enum AdShowResult
{
    /// <summary>用户完整观看，可发奖。</summary>
    Completed = 0,

    /// <summary>用户提前关闭或未完整观看。</summary>
    Skipped = 1,

    /// <summary>加载或展示过程失败。</summary>
    Failed = 2,

    /// <summary>广告 SDK 或广告位尚未就绪。</summary>
    NotReady = 3,

    /// <summary>已有广告正在播放。</summary>
    AlreadyShowing = 4,
}
