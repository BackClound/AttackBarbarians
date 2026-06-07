using System;

/// <summary>
/// 统一广告 SDK 接口。各广告商实现本接口，由 <see cref="AdSdkRegistry"/> 按 <see cref="AdNetworkKind"/> 创建实例。
/// </summary>
public interface IAdSdk
{
    AdNetworkKind NetworkKind { get; }

    string DisplayName { get; }

    bool IsInitialized { get; }

    bool IsShowing { get; }

    void Initialize(AdConfigSO config, Action<bool> onInitialized);

    bool IsRewardedReady(string placementId);

    void ShowRewarded(string placementId, Action<AdShowResult, string> onFinished);

    void Shutdown();
}
