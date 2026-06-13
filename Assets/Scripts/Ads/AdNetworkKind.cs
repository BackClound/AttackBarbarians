/// <summary>
/// 广告网络/SDK 标识。新增广告商时扩展枚举并在 <see cref="AdSdkRegistry"/> 注册工厂。
/// </summary>
public enum AdNetworkKind
{
    /// <summary>本地 Mock 广告（编辑器与测试）。</summary>
    Mock = 0,

    /// <summary>Unity Ads 广告网络。</summary>
    UnityAds = 1,
}
