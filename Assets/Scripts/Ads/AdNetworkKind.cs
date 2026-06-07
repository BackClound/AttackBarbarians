/// <summary>
/// 广告网络/SDK 标识。新增广告商时扩展枚举并在 <see cref="AdSdkRegistry"/> 注册工厂。
/// </summary>
public enum AdNetworkKind
{
    Mock = 0,
    UnityAds = 1,
}
