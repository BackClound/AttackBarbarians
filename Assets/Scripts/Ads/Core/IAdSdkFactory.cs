using UnityEngine;

/// <summary>
/// 按广告网络类型创建 <see cref="IAdSdk"/> 实例。
/// </summary>
public interface IAdSdkFactory
{
    /// <summary>工厂对应的广告网络类型。</summary>
    AdNetworkKind NetworkKind { get; }

    /// <summary>
    /// 创建广告 SDK 实例。
    /// </summary>
    /// <param name="coroutineHost">用于协程的 MonoBehaviour 宿主（Mock SDK 需要）。</param>
    /// <returns>广告 SDK 实例。</returns>
    IAdSdk Create(MonoBehaviour coroutineHost);
}
