using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 广告 SDK 工厂注册表。接入新广告商时实现 <see cref="IAdSdkFactory"/> 并调用 <see cref="Register"/>。
/// </summary>
public static class AdSdkRegistry
{
    private static readonly Dictionary<AdNetworkKind, IAdSdkFactory> Factories =
        new Dictionary<AdNetworkKind, IAdSdkFactory>(4);

    /// <summary>
    /// 静态构造函数：注册内置 Mock 与 Unity Ads SDK 工厂。
    /// </summary>
    static AdSdkRegistry()
    {
        Register(new MockAdSdkFactory());
        Register(new UnityAdsAdSdkFactory());
    }

    /// <summary>
    /// 注册广告 SDK 工厂。
    /// </summary>
    /// <param name="factory">工厂实例；为 null 时忽略。</param>
    public static void Register(IAdSdkFactory factory)
    {
        if (factory == null)
        {
            return;
        }

        Factories[factory.NetworkKind] = factory;
    }

    /// <summary>
    /// 按广告网络类型创建 SDK 实例。
    /// </summary>
    /// <param name="kind">广告网络类型。</param>
    /// <param name="coroutineHost">协程宿主 MonoBehaviour。</param>
    /// <param name="sdk">创建的 SDK 实例；失败时为 null。</param>
    /// <returns>创建成功返回 true，否则 false。</returns>
    public static bool TryCreate(AdNetworkKind kind, MonoBehaviour coroutineHost, out IAdSdk sdk)
    {
        sdk = null;
        if (coroutineHost == null)
        {
            return false;
        }

        if (!Factories.TryGetValue(kind, out IAdSdkFactory factory))
        {
            Debug.LogWarning($"[AdSdkRegistry] 未注册的广告网络: {kind}");
            return false;
        }

        sdk = factory.Create(coroutineHost);
        return sdk != null;
    }

    /// <summary>
    /// 检查指定广告网络是否已注册工厂。
    /// </summary>
    /// <param name="kind">广告网络类型。</param>
    /// <returns>已注册返回 true，否则 false。</returns>
    public static bool IsSupported(AdNetworkKind kind) => Factories.ContainsKey(kind);
}
