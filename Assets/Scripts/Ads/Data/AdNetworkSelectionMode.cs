/// <summary>
/// 广告网络选择策略。
/// </summary>
public enum AdNetworkSelectionMode
{
    /// <summary>按平台使用 Editor / Android / iOS 覆盖项。</summary>
    AutoByPlatform = 0,

    /// <summary>始终使用 <see cref="AdConfigSO.FixedNetwork"/>。</summary>
    Fixed = 1,
}
