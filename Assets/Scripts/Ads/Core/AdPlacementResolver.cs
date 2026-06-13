/// <summary>
/// 按当前广告网络解析 Placement ID。
/// </summary>
public static class AdPlacementResolver
{
    /// <summary>
    /// 解析指定广告网络的激励视频广告位 ID。
    /// </summary>
    /// <param name="config">广告运行时配置。</param>
    /// <param name="network">目标广告网络。</param>
    /// <returns>激励视频 Placement ID。</returns>
    public static string ResolveRewardedPlacement(AdConfigSO config, AdNetworkKind network) =>
        Resolve(config, network, AdPlacementType.Rewarded);

    /// <summary>
    /// 按广告网络与广告位类型解析 Placement ID。
    /// </summary>
    /// <param name="config">广告运行时配置；为 null 时使用内置默认值。</param>
    /// <param name="network">目标广告网络。</param>
    /// <param name="placementType">广告位类型。</param>
    /// <returns>解析到的 Placement ID；未配置时返回空字符串或默认值。</returns>
    public static string Resolve(AdConfigSO config, AdNetworkKind network, AdPlacementType placementType)
    {
        if (config != null && config.TryGetNetworkProfile(network, out AdNetworkProfile profile))
        {
            string fromProfile = placementType switch
            {
                AdPlacementType.Rewarded => profile.RewardedPlacementId,
                AdPlacementType.Interstitial => profile.InterstitialPlacementId,
                AdPlacementType.Banner => profile.BannerPlacementId,
                _ => string.Empty,
            };

            if (!string.IsNullOrWhiteSpace(fromProfile))
            {
                return fromProfile;
            }
        }

        if (config == null)
        {
            return GetDefaultPlacement(placementType);
        }

        return placementType switch
        {
            AdPlacementType.Rewarded => config.DefaultRewardedPlacementId,
            AdPlacementType.Interstitial => config.DefaultInterstitialPlacementId,
            AdPlacementType.Banner => config.DefaultBannerPlacementId,
            _ => string.Empty,
        };
    }

    /// <summary>
    /// 获取无配置时的内置默认 Placement ID。
    /// </summary>
    /// <param name="placementType">广告位类型。</param>
    /// <returns>默认 Placement ID。</returns>
    private static string GetDefaultPlacement(AdPlacementType placementType) =>
        placementType switch
        {
            AdPlacementType.Rewarded => "Rewarded_Android",
            AdPlacementType.Interstitial => "Interstitial_Android",
            AdPlacementType.Banner => "Banner_Android",
            _ => string.Empty,
        };
}
