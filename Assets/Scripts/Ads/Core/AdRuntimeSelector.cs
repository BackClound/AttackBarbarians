using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据 <see cref="AdConfigSO"/> 中的条件选择主用/备用广告网络，并构建展示重试链。
/// </summary>
public static class AdRuntimeSelector
{
    public const int DefaultMaxShowAttempts = 2;

    public static AdNetworkKind ResolvePrimaryNetwork(AdConfigSO config)
    {
        if (config == null)
        {
            return AdNetworkKind.Mock;
        }

        if (config.ShouldForceMock())
        {
            return AdNetworkKind.Mock;
        }

        switch (config.SelectionMode)
        {
            case AdNetworkSelectionMode.Fixed:
                return Normalize(config.FixedNetwork);

            case AdNetworkSelectionMode.AutoByPlatform:
            default:
#if UNITY_EDITOR
                return Normalize(config.EditorNetwork);
#elif UNITY_IOS
                return Normalize(config.IosNetwork);
#elif UNITY_ANDROID
                return Normalize(config.AndroidNetwork);
#else
                return Normalize(config.AndroidNetwork);
#endif
        }
    }

    public static AdNetworkKind ResolveSecondaryNetwork(AdConfigSO config)
    {
        if (config == null)
        {
            return AdNetworkKind.Mock;
        }

        AdNetworkKind primary = ResolvePrimaryNetwork(config);
        AdNetworkKind secondary = Normalize(config.FallbackNetwork);
        return secondary == primary ? AdNetworkKind.Mock : secondary;
    }

    public static AdNetworkKind ResolveFallbackNetwork(AdConfigSO config)
    {
        if (config == null || !config.EnableFallbackOnInitFailure)
        {
            return AdNetworkKind.Mock;
        }

        return ResolveSecondaryNetwork(config);
    }

    /// <summary>
    /// 构建激励广告展示尝试链：默认主用 + 备用，最多 <see cref="DefaultMaxShowAttempts"/> 个不同广告商。
    /// </summary>
    public static AdNetworkKind[] BuildRewardedShowChain(AdConfigSO config)
    {
        int maxAttempts = config != null ? config.MaxRewardedShowAttempts : DefaultMaxShowAttempts;
        if (config != null && !config.EnableFallbackOnLoadFailure)
        {
            maxAttempts = 1;
        }

        var chain = new List<AdNetworkKind>(maxAttempts);
        TryAddNetwork(chain, ResolvePrimaryNetwork(config), maxAttempts);
        TryAddNetwork(chain, ResolveSecondaryNetwork(config), maxAttempts);

        if (chain.Count == 0)
        {
            chain.Add(AdNetworkKind.Mock);
        }

        return chain.ToArray();
    }

    /// <summary>加载或展示失败时是否应切换下一家广告商（跳过、播放中不重试）。</summary>
    public static bool ShouldRetryOnLoadFailure(AdShowResult result) =>
        result == AdShowResult.Failed || result == AdShowResult.NotReady;

    private static void TryAddNetwork(List<AdNetworkKind> chain, AdNetworkKind network, int maxAttempts)
    {
        if (chain.Count >= maxAttempts || !AdSdkRegistry.IsSupported(network))
        {
            return;
        }

        for (int i = 0; i < chain.Count; i++)
        {
            if (chain[i] == network)
            {
                return;
            }
        }

        chain.Add(network);
    }

    private static AdNetworkKind Normalize(AdNetworkKind kind) =>
        AdSdkRegistry.IsSupported(kind) ? kind : AdNetworkKind.Mock;
}
