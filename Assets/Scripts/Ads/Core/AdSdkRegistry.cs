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

    static AdSdkRegistry()
    {
        Register(new MockAdSdkFactory());
        Register(new UnityAdsAdSdkFactory());
    }

    public static void Register(IAdSdkFactory factory)
    {
        if (factory == null)
        {
            return;
        }

        Factories[factory.NetworkKind] = factory;
    }

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

    public static bool IsSupported(AdNetworkKind kind) => Factories.ContainsKey(kind);
}
