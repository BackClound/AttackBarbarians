using System;
using UnityEngine;

/// <summary>
/// 激励广告编排：按配置选择广告 SDK、展示广告、加载失败时切换备用广告商（默认最多 2 次）。
/// </summary>
/// <remarks>
/// <para>接入新广告商：实现 <see cref="IAdSdk"/> + <see cref="IAdSdkFactory"/>，在 <see cref="AdSdkRegistry"/> 注册，并在 <see cref="AdConfigSO"/> 配置网络档案。</para>
/// </remarks>
public class AdRewardService : MonoBehaviour, IGameSystem
{
    [SerializeField] private AdConfigSO config;

    private IAdSdk activeSdk;
    private AdNetworkKind activeNetwork;
    private ShopManager shopManager;
    private ResourceManager resourceManager;
    private PendingRewardedRequest pendingRequest;
    private bool isInitialized;
    private bool isProviderReady;
    private bool isShowing;

    public bool IsInitialized => isInitialized;
    public bool IsProviderReady => isProviderReady;
    public bool IsShowing => isShowing;
    public AdConfigSO Config => config;
    public AdNetworkKind ActiveNetwork => activeNetwork;
    public string ActiveSdkName => activeSdk?.DisplayName ?? "None";

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        if (config == null)
        {
            config = Resources.Load<AdConfigSO>(GameConstants.ResourcePaths.AdConfig);
        }

        ServiceLocator.TryGet(out shopManager);
        ServiceLocator.TryGet(out resourceManager);
        isInitialized = true;
        PublishStateChanged();

        AdNetworkKind primary = AdRuntimeSelector.ResolvePrimaryNetwork(config);
        TryActivateNetwork(primary, allowInitFallback: true);
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        pendingRequest = null;
        activeSdk?.Shutdown();
        activeSdk = null;
        isInitialized = false;
        isProviderReady = false;
        isShowing = false;
        PublishStateChanged();
    }

    public void TryShowRewardedForShop(string shopConfigId)
    {
        if (!EnsureServiceReady(AdRewardSource.Shop, shopConfigId, out _))
        {
            return;
        }

        if (!ServiceLocator.TryGet(out ShopManager shop))
        {
            RaiseFailed(AdRewardSource.Shop, shopConfigId, GetRewardedPlacementId(), AdRewardFailedReason.ContextNotAllowed, "商店未就绪");
            return;
        }

        if (!shop.CanClaimAdFreeSupply(shopConfigId, out string failureReason))
        {
            RaiseFailed(
                AdRewardSource.Shop,
                shopConfigId,
                GetRewardedPlacementId(),
                AdRewardFailedReason.ContextNotAllowed,
                failureReason ?? "暂不可领取");
            return;
        }

        TryShowRewardedInternal(
            AdRewardSource.Shop,
            shopConfigId,
            () => shop.TryClaimAdFreeSupply(shopConfigId));
    }

    public void TryShowRewardedForAdTicket()
    {
        if (!EnsureServiceReady(AdRewardSource.AdTicket, GameConstants.ConfigIds.AdTicketEarn, out _))
        {
            return;
        }

        TryShowRewardedInternal(
            AdRewardSource.AdTicket,
            GameConstants.ConfigIds.AdTicketEarn,
            () => true);
    }

    public string GetRewardedPlacementId() =>
        AdPlacementResolver.ResolveRewardedPlacement(config, activeNetwork);

    private bool EnsureServiceReady(AdRewardSource source, string contextId, out string blockReason)
    {
        blockReason = null;
        if (!isInitialized)
        {
            blockReason = "广告服务未就绪";
            RaiseFailed(source, contextId, string.Empty, AdRewardFailedReason.NotInitialized, blockReason);
            return false;
        }

        if (isShowing || pendingRequest != null)
        {
            blockReason = "广告播放中";
            RaiseFailed(source, contextId, string.Empty, AdRewardFailedReason.AlreadyShowing, blockReason);
            return false;
        }

        return true;
    }

    private void TryActivateNetwork(AdNetworkKind network, bool allowInitFallback)
    {
        if (config != null && config.ShouldForceMock())
        {
            network = AdNetworkKind.Mock;
        }

        if (!AdSdkRegistry.TryCreate(network, this, out IAdSdk sdk))
        {
            Debug.LogWarning($"[AdRewardService] 无法创建 SDK: {network}");
            if (allowInitFallback)
            {
                ActivateInitFallback(network);
            }

            return;
        }

        if (activeSdk != null && activeNetwork != network)
        {
            activeSdk.Shutdown();
        }

        activeNetwork = network;
        activeSdk = sdk;
        isProviderReady = false;
        PublishStateChanged();

        activeSdk.Initialize(config, success => OnSdkInitialized(network, success, allowInitFallback));
    }

    private void OnSdkInitialized(AdNetworkKind attemptedNetwork, bool success, bool allowInitFallback)
    {
        if (success)
        {
            isProviderReady = true;
            PublishStateChanged();
            return;
        }

        Debug.LogWarning($"[AdRewardService] SDK 初始化失败: {attemptedNetwork}");
        activeSdk?.Shutdown();
        activeSdk = null;
        isProviderReady = false;

        if (!allowInitFallback || config == null || !config.EnableFallbackOnInitFailure)
        {
            PublishStateChanged();
            return;
        }

        AdNetworkKind fallback = AdRuntimeSelector.ResolveFallbackNetwork(config);
        if (fallback == attemptedNetwork)
        {
            PublishStateChanged();
            return;
        }

        TryActivateNetwork(fallback, allowInitFallback: false);
    }

    private void ActivateInitFallback(AdNetworkKind failedNetwork)
    {
        AdNetworkKind fallback = AdRuntimeSelector.ResolveFallbackNetwork(config);
        if (fallback == failedNetwork)
        {
            fallback = AdNetworkKind.Mock;
        }

        TryActivateNetwork(fallback, allowInitFallback: false);
    }

    private void TryShowRewardedInternal(
        AdRewardSource source,
        string contextId,
        Func<bool> grantReward)
    {
        AdNetworkKind[] chain = AdRuntimeSelector.BuildRewardedShowChain(config);
        if (chain.Length == 0)
        {
            RaiseFailed(source, contextId, string.Empty, AdRewardFailedReason.NotReady, "无可用广告商");
            return;
        }

        pendingRequest = new PendingRewardedRequest(source, contextId, grantReward, chain);
        isShowing = true;
        PublishStateChanged();
        BeginRewardedShowAttempt();
    }

    private void BeginRewardedShowAttempt()
    {
        if (pendingRequest == null)
        {
            return;
        }

        AdNetworkKind network = pendingRequest.Networks[pendingRequest.AttemptIndex];
        string placement = AdPlacementResolver.ResolveRewardedPlacement(config, network);
        ActivateNetworkForShow(network, placement);
    }

    private void ActivateNetworkForShow(AdNetworkKind network, string placement)
    {
        if (config != null && config.ShouldForceMock())
        {
            network = AdNetworkKind.Mock;
        }

        if (activeNetwork == network && activeSdk != null && isProviderReady && activeSdk.IsInitialized)
        {
            ShowRewardedOnActiveSdk(placement);
            return;
        }

        if (activeSdk != null)
        {
            activeSdk.Shutdown();
            activeSdk = null;
            isProviderReady = false;
        }

        if (!AdSdkRegistry.TryCreate(network, this, out IAdSdk sdk))
        {
            TryNextNetworkAfterFailure(placement, "无法创建广告 SDK");
            return;
        }

        activeNetwork = network;
        activeSdk = sdk;
        isProviderReady = false;

        activeSdk.Initialize(config, success =>
        {
            if (!success)
            {
                Debug.LogWarning($"[AdRewardService] 展示前 SDK 初始化失败: {network}");
                TryNextNetworkAfterFailure(placement, "广告 SDK 初始化失败");
                return;
            }

            isProviderReady = true;
            ShowRewardedOnActiveSdk(placement);
        });
    }

    private void ShowRewardedOnActiveSdk(string placement)
    {
        if (pendingRequest == null || activeSdk == null)
        {
            return;
        }

        activeSdk.ShowRewarded(placement, (result, message) => OnRewardedShowFinished(placement, result, message));
    }

    private void OnRewardedShowFinished(string placement, AdShowResult result, string message)
    {
        if (pendingRequest == null)
        {
            return;
        }

        if (AdRuntimeSelector.ShouldRetryOnLoadFailure(result) && pendingRequest.HasNextAttempt)
        {
            Debug.Log(
                $"[AdRewardService] 广告加载失败 ({activeNetwork})，切换备用广告商 " +
                $"({pendingRequest.AttemptIndex + 1}/{pendingRequest.Networks.Length}): {message}");
            pendingRequest.AdvanceAttempt();
            BeginRewardedShowAttempt();
            return;
        }

        PendingRewardedRequest request = pendingRequest;
        pendingRequest = null;
        isShowing = false;
        PublishStateChanged();
        RestorePrimaryNetworkAfterShow();

        switch (result)
        {
            case AdShowResult.Completed:
                if (request.GrantReward == null || request.GrantReward.Invoke())
                {
                    ApplyStandardAdRewards();
                    GameEvents.RaiseAdRewardCompleted(
                        this,
                        new AdRewardCompletedEventArgs(request.Source, request.ContextId, placement));
                }
                else
                {
                    RaiseFailed(
                        request.Source,
                        request.ContextId,
                        placement,
                        AdRewardFailedReason.RewardValidationFailed,
                        "发奖失败");
                }

                break;

            case AdShowResult.Skipped:
                RaiseFailed(request.Source, request.ContextId, placement, AdRewardFailedReason.Skipped, message ?? "未完整观看广告");
                break;

            case AdShowResult.AlreadyShowing:
                RaiseFailed(request.Source, request.ContextId, placement, AdRewardFailedReason.AlreadyShowing, message ?? "广告播放中");
                break;

            default:
                string failMessage = request.Networks.Length > 1
                    ? $"广告加载失败（已尝试 {request.Networks.Length} 家广告商）"
                    : message ?? "广告播放失败";
                RaiseFailed(
                    request.Source,
                    request.ContextId,
                    placement,
                    AdRewardFailedReason.ShowFailed,
                    failMessage);
                break;
        }
    }

    private void TryNextNetworkAfterFailure(string placement, string message)
    {
        if (pendingRequest != null && pendingRequest.HasNextAttempt)
        {
            Debug.LogWarning($"[AdRewardService] {message}，切换备用广告商");
            pendingRequest.AdvanceAttempt();
            BeginRewardedShowAttempt();
            return;
        }

        PendingRewardedRequest request = pendingRequest;
        pendingRequest = null;
        isShowing = false;
        PublishStateChanged();
        RestorePrimaryNetworkAfterShow();

        if (request != null)
        {
            string failMessage = request.Networks.Length > 1
                ? $"广告加载失败（已尝试 {request.Networks.Length} 家广告商）"
                : message;
            RaiseFailed(
                request.Source,
                request.ContextId,
                placement,
                AdRewardFailedReason.ShowFailed,
                failMessage);
        }
    }

    private void RestorePrimaryNetworkAfterShow()
    {
        AdNetworkKind primary = AdRuntimeSelector.ResolvePrimaryNetwork(config);
        if (activeNetwork == primary && isProviderReady)
        {
            return;
        }

        TryActivateNetwork(primary, allowInitFallback: false);
    }

    private void RaiseFailed(
        AdRewardSource source,
        string contextId,
        string placementId,
        AdRewardFailedReason reason,
        string message)
    {
        GameEvents.RaiseAdRewardFailed(
            this,
            new AdRewardFailedEventArgs(source, contextId, placementId, reason, message));
    }

    /// <summary>
    /// 任意激励广告成功后统一发放：广告券 + 体力回满。
    /// </summary>
    private void ApplyStandardAdRewards()
    {
        if (!ServiceLocator.TryGet(out ResourceManager resources))
        {
            return;
        }

        int amount = config != null ? config.RewardAdTicketAmount : AdTicketConstants.DefaultRewardPerAd;
        resources.TryGrantAdTicketReward(amount, ResourceChangeReason.AdReward, out _);
        resources.TryRefillStaminaFromAd(out _);
    }

    private void PublishStateChanged()
    {
        bool showing = isShowing || (activeSdk != null && activeSdk.IsShowing);
        GameEvents.RaiseAdRewardStateChanged(this, new AdRewardStateChangedEventArgs(showing));
    }

    private sealed class PendingRewardedRequest
    {
        public PendingRewardedRequest(
            AdRewardSource source,
            string contextId,
            Func<bool> grantReward,
            AdNetworkKind[] networks)
        {
            Source = source;
            ContextId = contextId;
            GrantReward = grantReward;
            Networks = networks ?? Array.Empty<AdNetworkKind>();
        }

        public AdRewardSource Source { get; }
        public string ContextId { get; }
        public Func<bool> GrantReward { get; }
        public AdNetworkKind[] Networks { get; }
        public int AttemptIndex { get; private set; }

        public bool HasNextAttempt => AttemptIndex + 1 < Networks.Length;

        public void AdvanceAttempt() => AttemptIndex++;
    }
}
