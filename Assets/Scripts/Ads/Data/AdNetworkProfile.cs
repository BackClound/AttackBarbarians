using System;
using UnityEngine;

/// <summary>
/// 单个广告网络的 Game ID 与 Placement 配置。
/// </summary>
[Serializable]
public struct AdNetworkProfile
{
    public AdNetworkKind network;

    [Header("Credentials")]
    public string androidGameId;
    public string iosGameId;
    public bool testMode;

    [Header("Placements")]
    public string rewardedPlacementId;
    public string interstitialPlacementId;
    public string bannerPlacementId;

    public AdNetworkKind Network => network;

    public string RewardedPlacementId => rewardedPlacementId ?? string.Empty;
    public string InterstitialPlacementId => interstitialPlacementId ?? string.Empty;
    public string BannerPlacementId => bannerPlacementId ?? string.Empty;

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
