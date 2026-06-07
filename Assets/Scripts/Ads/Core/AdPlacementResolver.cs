/// <summary>
/// 按当前广告网络解析 Placement ID。
/// </summary>
public static class AdPlacementResolver
{
    public static string ResolveRewardedPlacement(AdConfigSO config, AdNetworkKind network) =>
        Resolve(config, network, AdPlacementType.Rewarded);

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

    private static string GetDefaultPlacement(AdPlacementType placementType) =>
        placementType switch
        {
            AdPlacementType.Rewarded => "Rewarded_Android",
            AdPlacementType.Interstitial => "Interstitial_Android",
            AdPlacementType.Banner => "Banner_Android",
            _ => string.Empty,
        };
}
