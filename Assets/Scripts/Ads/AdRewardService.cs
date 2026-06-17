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

    /// <summary>广告服务是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>当前激活的广告 SDK 是否就绪。</summary>
    public bool IsProviderReady => isProviderReady;

    /// <summary>是否正在展示激励广告。</summary>
    public bool IsShowing => isShowing;

    /// <summary>广告运行时配置资产。</summary>
    public AdConfigSO Config => config;

    /// <summary>当前激活的广告网络类型。</summary>
    public AdNetworkKind ActiveNetwork => activeNetwork;

    /// <summary>当前激活 SDK 的显示名称。</summary>
    public string ActiveSdkName => activeSdk?.DisplayName ?? "None";

    /// <summary>
    /// 初始化广告服务：加载配置、解析主用网络并激活 SDK。
    /// </summary>
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

    /// <summary>
    /// 每帧更新（当前无逻辑）。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭广告服务并释放 SDK 资源。
    /// </summary>
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

    /// <summary>
    /// 为商城广告补给位播放激励广告，成功后领取补给奖励。
    /// </summary>
    /// <param name="shopConfigId">商城补给位配置 ID。</param>
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

    /// <summary>
    /// 播放激励广告以获取广告券（不含商城补给逻辑）。
    /// </summary>
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

    /// <summary>
    /// 播放激励广告以刷新局内三选一候选。
    /// </summary>
    public void TryShowRewardedForUpgradeReroll()
    {
        if (!EnsureServiceReady(AdRewardSource.UpgradeReroll, GameConstants.ConfigIds.UpgradeReroll, out _))
        {
            return;
        }

        if (!ServiceLocator.TryGet(out RandomRewardManager rewardManager))
        {
            RaiseFailed(
                AdRewardSource.UpgradeReroll,
                GameConstants.ConfigIds.UpgradeReroll,
                GetRewardedPlacementId(),
                AdRewardFailedReason.ContextNotAllowed,
                "升级系统未就绪");
            return;
        }

        if (rewardManager.RemainingAdRerolls <= 0)
        {
            RaiseFailed(
                AdRewardSource.UpgradeReroll,
                GameConstants.ConfigIds.UpgradeReroll,
                GetRewardedPlacementId(),
                AdRewardFailedReason.ContextNotAllowed,
                "本局刷新次数已用尽");
            return;
        }

        TryShowRewardedInternal(
            AdRewardSource.UpgradeReroll,
            GameConstants.ConfigIds.UpgradeReroll,
            () => rewardManager.TryRerollViaAd());
    }

    /// <summary>
    /// 播放激励广告以全选局内三选一候选。
    /// </summary>
    public void TryShowRewardedForUpgradeSelectAll()
    {
        if (!EnsureServiceReady(AdRewardSource.UpgradeSelectAll, GameConstants.ConfigIds.UpgradeSelectAll, out _))
        {
            return;
        }

        if (!ServiceLocator.TryGet(out RandomRewardManager rewardManager))
        {
            RaiseFailed(
                AdRewardSource.UpgradeSelectAll,
                GameConstants.ConfigIds.UpgradeSelectAll,
                GetRewardedPlacementId(),
                AdRewardFailedReason.ContextNotAllowed,
                "升级系统未就绪");
            return;
        }

        if (rewardManager.RemainingAdSelectAll <= 0)
        {
            RaiseFailed(
                AdRewardSource.UpgradeSelectAll,
                GameConstants.ConfigIds.UpgradeSelectAll,
                GetRewardedPlacementId(),
                AdRewardFailedReason.ContextNotAllowed,
                "本局全选次数已用尽");
            return;
        }

        TryShowRewardedInternal(
            AdRewardSource.UpgradeSelectAll,
            GameConstants.ConfigIds.UpgradeSelectAll,
            () => rewardManager.TrySelectAllViaAd());
    }

    /// <summary>
    /// 获取当前激活网络的激励视频广告位 ID。
    /// </summary>
    /// <returns>激励视频 Placement ID。</returns>
    public string GetRewardedPlacementId() =>
        AdPlacementResolver.ResolveRewardedPlacement(config, activeNetwork);

    /// <summary>
    /// 校验广告服务是否就绪且未在播放中。
    /// </summary>
    /// <param name="source">广告发奖业务来源。</param>
    /// <param name="contextId">业务上下文 ID。</param>
    /// <param name="blockReason">阻塞原因说明。</param>
    /// <returns>就绪返回 true，否则 false 并触发失败事件。</returns>
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

    /// <summary>
    /// 激活指定广告网络并初始化 SDK。
    /// </summary>
    /// <param name="network">目标广告网络。</param>
    /// <param name="allowInitFallback">初始化失败时是否允许切换备用网络。</param>
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

    /// <summary>
    /// SDK 初始化完成后的回调处理。
    /// </summary>
    /// <param name="attemptedNetwork">尝试激活的广告网络。</param>
    /// <param name="success">是否初始化成功。</param>
    /// <param name="allowInitFallback">失败时是否允许切换备用网络。</param>
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

    /// <summary>
    /// 无法创建 SDK 时激活备用网络。
    /// </summary>
    /// <param name="failedNetwork">创建失败的广告网络。</param>
    private void ActivateInitFallback(AdNetworkKind failedNetwork)
    {
        AdNetworkKind fallback = AdRuntimeSelector.ResolveFallbackNetwork(config);
        if (fallback == failedNetwork)
        {
            fallback = AdNetworkKind.Mock;
        }

        TryActivateNetwork(fallback, allowInitFallback: false);
    }

    /// <summary>
    /// 构建激励广告展示请求并开始尝试链。
    /// </summary>
    /// <param name="source">广告发奖业务来源。</param>
    /// <param name="contextId">业务上下文 ID。</param>
    /// <param name="grantReward">观看成功后执行的业务发奖委托。</param>
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

    /// <summary>
    /// 按当前尝试索引开始一次激励广告展示。
    /// </summary>
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

    /// <summary>
    /// 为展示流程激活指定广告网络（必要时重新初始化 SDK）。
    /// </summary>
    /// <param name="network">目标广告网络。</param>
    /// <param name="placement">激励视频广告位 ID。</param>
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

    /// <summary>
    /// 在当前激活 SDK 上展示激励视频。
    /// </summary>
    /// <param name="placement">激励视频广告位 ID。</param>
    private void ShowRewardedOnActiveSdk(string placement)
    {
        if (pendingRequest == null || activeSdk == null)
        {
            return;
        }

        activeSdk.ShowRewarded(placement, (result, message) => OnRewardedShowFinished(placement, result, message));
    }

    /// <summary>
    /// 激励广告展示结束回调，处理发奖、失败与备用网络重试。
    /// </summary>
    /// <param name="placement">广告位 Placement ID。</param>
    /// <param name="result">展示结果。</param>
    /// <param name="message">结果说明文本。</param>
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

    /// <summary>
    /// 展示前失败时尝试切换下一家广告商，或结束并上报失败。
    /// </summary>
    /// <param name="placement">广告位 Placement ID。</param>
    /// <param name="message">失败说明文本。</param>
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

    /// <summary>
    /// 展示结束后恢复主用广告网络。
    /// </summary>
    private void RestorePrimaryNetworkAfterShow()
    {
        AdNetworkKind primary = AdRuntimeSelector.ResolvePrimaryNetwork(config);
        if (activeNetwork == primary && isProviderReady)
        {
            return;
        }

        TryActivateNetwork(primary, allowInitFallback: false);
    }

    /// <summary>
    /// 发布激励广告失败事件。
    /// </summary>
    /// <param name="source">广告发奖业务来源。</param>
    /// <param name="contextId">业务上下文 ID。</param>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="reason">失败原因。</param>
    /// <param name="message">失败说明文本。</param>
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

    /// <summary>
    /// 发布广告展示状态变化事件。
    /// </summary>
    private void PublishStateChanged()
    {
        bool showing = isShowing || (activeSdk != null && activeSdk.IsShowing);
        GameEvents.RaiseAdRewardStateChanged(this, new AdRewardStateChangedEventArgs(showing));
    }

    /// <summary>
    /// 待处理的激励广告展示请求，包含发奖链路与广告商尝试链。
    /// </summary>
    private sealed class PendingRewardedRequest
    {
        /// <summary>
        /// 创建待处理激励广告请求。
        /// </summary>
        /// <param name="source">广告发奖业务来源。</param>
        /// <param name="contextId">业务上下文 ID。</param>
        /// <param name="grantReward">观看完成后执行的发奖委托。</param>
        /// <param name="networks">按尝试顺序排列的广告网络数组。</param>
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

        /// <summary>广告发奖业务来源。</summary>
        public AdRewardSource Source { get; }

        /// <summary>业务上下文 ID。</summary>
        public string ContextId { get; }

        /// <summary>观看完成后执行的发奖委托。</summary>
        public Func<bool> GrantReward { get; }

        /// <summary>按尝试顺序排列的广告网络数组。</summary>
        public AdNetworkKind[] Networks { get; }

        /// <summary>当前尝试的广告网络索引。</summary>
        public int AttemptIndex { get; private set; }

        /// <summary>是否还有下一家广告商可尝试。</summary>
        public bool HasNextAttempt => AttemptIndex + 1 < Networks.Length;

        /// <summary>
        /// 推进到下一家广告商的尝试索引。
        /// </summary>
        public void AdvanceAttempt() => AttemptIndex++;
    }
}
