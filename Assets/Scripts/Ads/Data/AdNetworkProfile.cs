using System;
using UnityEngine;

/// <summary>
/// 单个广告网络的 Game ID 与 Placement 配置。
/// </summary>
[Serializable]
public struct AdNetworkProfile
{
    /// <summary>广告网络类型。</summary>
    public AdNetworkKind network;

    [Header("Credentials")]
    /// <summary>Android 平台 Game ID。</summary>
    public string androidGameId;

    /// <summary>iOS 平台 Game ID。</summary>
    public string iosGameId;

    /// <summary>是否启用测试模式。</summary>
    public bool testMode;

    [Header("Placements")]
    /// <summary>激励视频广告位 ID。</summary>
    public string rewardedPlacementId;

    /// <summary>插屏广告位 ID。</summary>
    public string interstitialPlacementId;

    /// <summary>横幅广告位 ID。</summary>
    public string bannerPlacementId;

    /// <summary>广告网络类型。</summary>
    public AdNetworkKind Network => network;

    /// <summary>激励视频广告位 ID。</summary>
    public string RewardedPlacementId => rewardedPlacementId ?? string.Empty;

    /// <summary>插屏广告位 ID。</summary>
    public string InterstitialPlacementId => interstitialPlacementId ?? string.Empty;

    /// <summary>横幅广告位 ID。</summary>
    public string BannerPlacementId => bannerPlacementId ?? string.Empty;

    /// <summary>
    /// 解析当前运行平台对应的 Game ID。
    /// </summary>
    /// <returns>当前平台的 Game ID；未配置时返回空字符串。</returns>
    public string ResolveGameId()
    {
#if UNITY_IOS
        return iosGameId ?? string.Empty;
#elif UNITY_ANDROID
        return androidGameId ?? string.Empty;
#else
        return !string.IsNullOrWhiteSpace(androidGameId) ? androidGameId : iosGameId ?? string.Empty;
#endif
    }
}
