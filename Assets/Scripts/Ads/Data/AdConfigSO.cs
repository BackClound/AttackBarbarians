using System;
using UnityEngine;

/// <summary>
/// 广告运行时配置：网络选择策略、各 SDK 参数、Mock 与体力广告奖励。
/// </summary>
/// <remarks>
/// <para><b>路径：</b><c>Assets/Resources/Config/Ad/AdConfig_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "AdConfig", menuName = "Attack Barbarians/Ad/Ad Config")]
public class AdConfigSO : ScriptableObject
{
    [Header("Network Selection")]
    [SerializeField] private AdNetworkSelectionMode selectionMode = AdNetworkSelectionMode.AutoByPlatform;
    [SerializeField] private AdNetworkKind fixedNetwork = AdNetworkKind.Mock;
    [SerializeField] private AdNetworkKind editorNetwork = AdNetworkKind.Mock;
    [SerializeField] private AdNetworkKind androidNetwork = AdNetworkKind.UnityAds;
    [SerializeField] private AdNetworkKind iosNetwork = AdNetworkKind.UnityAds;
    [SerializeField] private bool forceMock;
    [SerializeField] private bool useMockInEditor = true;
    [SerializeField] private AdNetworkKind fallbackNetwork = AdNetworkKind.Mock;
    [SerializeField] private bool enableFallbackOnInitFailure = true;
    [Tooltip("激励广告加载/展示失败时，是否切换备用广告商重试。")]
    [SerializeField] private bool enableFallbackOnLoadFailure = true;
    [Tooltip("单次打开广告最多尝试的广告商数量（主用 + 备用，共 2 即切换 1 次）。")]
    [SerializeField] private int maxRewardedShowAttempts = 2;

    [Header("Default Placements (fallback)")]
    [SerializeField] private string defaultRewardedPlacementId = "Rewarded_Android";
    [SerializeField] private string defaultInterstitialPlacementId = "Interstitial_Android";
    [SerializeField] private string defaultBannerPlacementId = "Banner_Android";

    [Header("Per-Network Profiles")]
    [SerializeField] private AdNetworkProfile[] networkProfiles = Array.Empty<AdNetworkProfile>();

    [Header("Legacy Unity Ads (used when profile missing)")]
    [SerializeField] private bool testMode = true;
    [SerializeField] private string androidGameId = string.Empty;
    [SerializeField] private string iosGameId = string.Empty;

    [Header("Mock")]
    [SerializeField] private float mockAdDelaySeconds = 0.35f;
    [SerializeField] private bool mockSimulateSkip;

    [Header("Ad Ticket Reward")]
    [Tooltip("观看激励广告成功后增加的广告券数量。")]
    [SerializeField] private int rewardAdTicketAmount = AdTicketConstants.DefaultRewardPerAd;

    public AdNetworkSelectionMode SelectionMode => selectionMode;
    public AdNetworkKind FixedNetwork => fixedNetwork;
    public AdNetworkKind EditorNetwork => editorNetwork;
    public AdNetworkKind AndroidNetwork => androidNetwork;
    public AdNetworkKind IosNetwork => iosNetwork;
    public AdNetworkKind FallbackNetwork => fallbackNetwork;
    public bool EnableFallbackOnInitFailure => enableFallbackOnInitFailure;
    public bool EnableFallbackOnLoadFailure => enableFallbackOnLoadFailure;
    public int MaxRewardedShowAttempts => Mathf.Clamp(maxRewardedShowAttempts, 1, 4);
    public string DefaultRewardedPlacementId => string.IsNullOrWhiteSpace(defaultRewardedPlacementId) ? "Rewarded_Android" : defaultRewardedPlacementId;
    public string DefaultInterstitialPlacementId => string.IsNullOrWhiteSpace(defaultInterstitialPlacementId) ? "Interstitial_Android" : defaultInterstitialPlacementId;
    public string DefaultBannerPlacementId => string.IsNullOrWhiteSpace(defaultBannerPlacementId) ? "Banner_Android" : defaultBannerPlacementId;
    public float MockAdDelaySeconds => Mathf.Max(0f, mockAdDelaySeconds);
    public bool MockSimulateSkip => mockSimulateSkip;
    public int RewardAdTicketAmount => Mathf.Max(1, rewardAdTicketAmount);
    public bool TestMode => testMode;
    public string AndroidGameId => androidGameId ?? string.Empty;
    public string IosGameId => iosGameId ?? string.Empty;

    public bool ShouldForceMock()
    {
        if (forceMock)
        {
            return true;
        }

#if UNITY_EDITOR
        return useMockInEditor;
#else
        return false;
#endif
    }

    public bool TryGetNetworkProfile(AdNetworkKind kind, out AdNetworkProfile profile)
    {
        if (networkProfiles != null)
        {
            for (int i = 0; i < networkProfiles.Length; i++)
            {
                if (networkProfiles[i].Network == kind)
                {
                    profile = networkProfiles[i];
                    return true;
                }
            }
        }

        if (kind == AdNetworkKind.UnityAds)
        {
            profile = new AdNetworkProfile
            {
                network = AdNetworkKind.UnityAds,
                androidGameId = AndroidGameId,
                iosGameId = IosGameId,
                testMode = testMode,
                rewardedPlacementId = DefaultRewardedPlacementId,
                interstitialPlacementId = DefaultInterstitialPlacementId,
                bannerPlacementId = DefaultBannerPlacementId,
            };
            return true;
        }

        profile = default;
        return kind == AdNetworkKind.Mock;
    }

    public string ResolveGameId(AdNetworkKind kind)
    {
        if (TryGetNetworkProfile(kind, out AdNetworkProfile profile))
        {
            return profile.ResolveGameId();
        }

        return ResolveLegacyGameId();
    }

    public bool ResolveTestMode(AdNetworkKind kind)
    {
        if (TryGetNetworkProfile(kind, out AdNetworkProfile profile))
        {
            return profile.testMode;
        }

        return testMode;
    }

    private string ResolveLegacyGameId()
    {
#if UNITY_IOS
        return IosGameId;
#elif UNITY_ANDROID
        return AndroidGameId;
#else
        return !string.IsNullOrWhiteSpace(AndroidGameId) ? AndroidGameId : IosGameId;
#endif
    }
}
