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

    /// <summary>广告网络选择策略。</summary>
    public AdNetworkSelectionMode SelectionMode => selectionMode;

    /// <summary>固定模式下使用的广告网络。</summary>
    public AdNetworkKind FixedNetwork => fixedNetwork;

    /// <summary>编辑器环境下使用的广告网络。</summary>
    public AdNetworkKind EditorNetwork => editorNetwork;

    /// <summary>Android 平台使用的广告网络。</summary>
    public AdNetworkKind AndroidNetwork => androidNetwork;

    /// <summary>iOS 平台使用的广告网络。</summary>
    public AdNetworkKind IosNetwork => iosNetwork;

    /// <summary>备用广告网络。</summary>
    public AdNetworkKind FallbackNetwork => fallbackNetwork;

    /// <summary>SDK 初始化失败时是否启用备用网络。</summary>
    public bool EnableFallbackOnInitFailure => enableFallbackOnInitFailure;

    /// <summary>广告加载/展示失败时是否切换备用网络重试。</summary>
    public bool EnableFallbackOnLoadFailure => enableFallbackOnLoadFailure;

    /// <summary>单次激励广告最多尝试的广告商数量（1–4）。</summary>
    public int MaxRewardedShowAttempts => Mathf.Clamp(maxRewardedShowAttempts, 1, 4);

    /// <summary>默认激励视频广告位 ID。</summary>
    public string DefaultRewardedPlacementId => string.IsNullOrWhiteSpace(defaultRewardedPlacementId) ? "Rewarded_Android" : defaultRewardedPlacementId;

    /// <summary>默认插屏广告位 ID。</summary>
    public string DefaultInterstitialPlacementId => string.IsNullOrWhiteSpace(defaultInterstitialPlacementId) ? "Interstitial_Android" : defaultInterstitialPlacementId;

    /// <summary>默认横幅广告位 ID。</summary>
    public string DefaultBannerPlacementId => string.IsNullOrWhiteSpace(defaultBannerPlacementId) ? "Banner_Android" : defaultBannerPlacementId;

    /// <summary>Mock 广告模拟播放延迟（秒）。</summary>
    public float MockAdDelaySeconds => Mathf.Max(0f, mockAdDelaySeconds);

    /// <summary>Mock 广告是否模拟用户跳过。</summary>
    public bool MockSimulateSkip => mockSimulateSkip;

    /// <summary>观看激励广告成功后发放的广告券数量。</summary>
    public int RewardAdTicketAmount => Mathf.Max(1, rewardAdTicketAmount);

    /// <summary>Unity Ads 是否启用测试模式（旧版字段）。</summary>
    public bool TestMode => testMode;

    /// <summary>Android 平台 Game ID（旧版字段）。</summary>
    public string AndroidGameId => androidGameId ?? string.Empty;

    /// <summary>iOS 平台 Game ID（旧版字段）。</summary>
    public string IosGameId => iosGameId ?? string.Empty;

    /// <summary>
    /// 判断当前环境是否应强制使用 Mock 广告。
    /// </summary>
    /// <returns>强制 Mock 返回 true，否则 false。</returns>
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

    /// <summary>
    /// 获取指定广告网络的配置档案。
    /// </summary>
    /// <param name="kind">广告网络类型。</param>
    /// <param name="profile">找到的配置档案。</param>
    /// <returns>找到或能合成默认档案时返回 true，否则 false。</returns>
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

    /// <summary>
    /// 解析指定广告网络在当前平台的 Game ID。
    /// </summary>
    /// <param name="kind">广告网络类型。</param>
    /// <returns>Game ID；未配置时返回空字符串。</returns>
    public string ResolveGameId(AdNetworkKind kind)
    {
        if (TryGetNetworkProfile(kind, out AdNetworkProfile profile))
        {
            return profile.ResolveGameId();
        }

        return ResolveLegacyGameId();
    }

    /// <summary>
    /// 解析指定广告网络是否启用测试模式。
    /// </summary>
    /// <param name="kind">广告网络类型。</param>
    /// <returns>启用测试模式返回 true，否则 false。</returns>
    public bool ResolveTestMode(AdNetworkKind kind)
    {
        if (TryGetNetworkProfile(kind, out AdNetworkProfile profile))
        {
            return profile.testMode;
        }

        return testMode;
    }

    /// <summary>
    /// 解析旧版 Unity Ads 字段中的 Game ID。
    /// </summary>
    /// <returns>当前平台的 Game ID。</returns>
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
