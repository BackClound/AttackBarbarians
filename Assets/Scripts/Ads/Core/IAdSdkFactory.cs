using UnityEngine;

/// <summary>
/// 按广告网络类型创建 <see cref="IAdSdk"/> 实例。
/// </summary>
public interface IAdSdkFactory
{
    AdNetworkKind NetworkKind { get; }

    IAdSdk Create(MonoBehaviour coroutineHost);
}
