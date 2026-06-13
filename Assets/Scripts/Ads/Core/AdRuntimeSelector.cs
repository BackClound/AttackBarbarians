using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据 <see cref="AdConfigSO"/> 中的条件选择主用/备用广告网络，并构建展示重试链。
/// </summary>
public static class AdRuntimeSelector
{
    /// <summary>默认单次激励广告最多尝试的广告商数量。</summary>
    public const int DefaultMaxShowAttempts = 2;

    /// <summary>
    /// 解析当前环境应使用的主用广告网络。
    /// </summary>
    /// <param name="config">广告运行时配置；为 null 时返回 Mock。</param>
    /// <returns>主用广告网络类型。</returns>
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

    /// <summary>
    /// 解析备用广告网络（与主用不同）。
    /// </summary>
    /// <param name="config">广告运行时配置；为 null 时返回 Mock。</param>
    /// <returns>备用广告网络类型。</returns>
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

    /// <summary>
    /// 解析 SDK 初始化失败时应切换到的备用网络。
    /// </summary>
    /// <param name="config">广告运行时配置。</param>
    /// <returns>备用广告网络类型；未启用回退时返回 Mock。</returns>
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
    /// <param name="config">广告运行时配置。</param>
    /// <returns>按尝试顺序排列的广告网络数组。</returns>
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

    /// <summary>
    /// 加载或展示失败时是否应切换下一家广告商（跳过、播放中不重试）。
    /// </summary>
    /// <param name="result">广告展示结果。</param>
    /// <returns>应重试返回 true，否则 false。</returns>
    public static bool ShouldRetryOnLoadFailure(AdShowResult result) =>
        result == AdShowResult.Failed || result == AdShowResult.NotReady;

    /// <summary>
    /// 将广告网络加入尝试链（去重且不超过上限）。
    /// </summary>
    /// <param name="chain">当前尝试链。</param>
    /// <param name="network">待加入的广告网络。</param>
    /// <param name="maxAttempts">最大尝试数量。</param>
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

    /// <summary>
    /// 将广告网络规范化为已注册类型，不支持时回退 Mock。
    /// </summary>
    /// <param name="kind">原始广告网络类型。</param>
    /// <returns>规范化后的广告网络类型。</returns>
    private static AdNetworkKind Normalize(AdNetworkKind kind) =>
        AdSdkRegistry.IsSupported(kind) ? kind : AdNetworkKind.Mock;
}
