using System;
using UnityEngine;
using UnityEngine.Advertisements;

/// <summary>
/// Unity Ads 激励视频 SDK 适配。
/// </summary>
public sealed class UnityAdsAdSdk
    : IAdSdk,
        IUnityAdsInitializationListener,
        IUnityAdsLoadListener,
        IUnityAdsShowListener
{
    private AdConfigSO config;
    private AdNetworkKind activeNetwork = AdNetworkKind.UnityAds;
    private bool isInitialized;
    private bool isShowing;
    private bool isLoading;
    private string activePlacementId;
    private Action<AdShowResult, string> pendingFinishCallback;
    private Action<bool> pendingInitCallback;

    /// <summary>所属广告网络类型。</summary>
    public AdNetworkKind NetworkKind => AdNetworkKind.UnityAds;

    /// <summary>SDK 显示名称（用于日志与调试）。</summary>
    public string DisplayName => "Unity Ads";

    /// <summary>SDK 是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>是否正在展示广告。</summary>
    public bool IsShowing => isShowing;

    /// <summary>
    /// 初始化 Unity Ads SDK。
    /// </summary>
    /// <param name="adConfig">广告运行时配置。</param>
    /// <param name="onInitialized">初始化完成回调，参数为是否成功。</param>
    public void Initialize(AdConfigSO adConfig, Action<bool> onInitialized)
    {
        config = adConfig;
        activeNetwork = AdNetworkKind.UnityAds;
        pendingInitCallback = onInitialized;

        string gameId = config != null ? config.ResolveGameId(activeNetwork) : string.Empty;
        if (string.IsNullOrWhiteSpace(gameId))
        {
            pendingInitCallback?.Invoke(false);
            pendingInitCallback = null;
            return;
        }

        bool testMode = config != null && config.ResolveTestMode(activeNetwork);
        if (Advertisement.isInitialized)
        {
            isInitialized = true;
            pendingInitCallback?.Invoke(true);
            pendingInitCallback = null;
            return;
        }

        Advertisement.Initialize(gameId, testMode, this);
    }

    /// <summary>
    /// 检查指定激励广告位是否可播放。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <returns>已初始化且未在加载或播放时返回 true。</returns>
    public bool IsRewardedReady(string placementId)
    {
        if (!isInitialized || isShowing || isLoading)
        {
            return false;
        }

        return Advertisement.isInitialized;
    }

    /// <summary>
    /// 加载并展示激励视频广告。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="onFinished">展示结束回调，参数为结果与说明文本。</param>
    public void ShowRewarded(string placementId, Action<AdShowResult, string> onFinished)
    {
        if (!isInitialized)
        {
            onFinished?.Invoke(AdShowResult.NotReady, "Unity Ads 未初始化");
            return;
        }

        if (isShowing || isLoading)
        {
            onFinished?.Invoke(AdShowResult.AlreadyShowing, "广告播放中");
            return;
        }

        if (string.IsNullOrWhiteSpace(placementId))
        {
            onFinished?.Invoke(AdShowResult.Failed, "Placement 为空");
            return;
        }

        pendingFinishCallback = onFinished;
        activePlacementId = placementId;
        isLoading = true;
        Advertisement.Load(placementId, this);
    }

    /// <summary>
    /// 关闭并释放 Unity Ads SDK 资源，中止待处理的展示回调。
    /// </summary>
    public void Shutdown()
    {
        CompletePending(AdShowResult.Failed, "广告服务已关闭");
        isInitialized = false;
        isLoading = false;
        config = null;
        pendingInitCallback = null;
    }

    /// <summary>
    /// Unity Ads SDK 初始化成功回调。
    /// </summary>
    public void OnInitializationComplete()
    {
        isInitialized = true;
        pendingInitCallback?.Invoke(true);
        pendingInitCallback = null;
    }

    /// <summary>
    /// Unity Ads SDK 初始化失败回调。
    /// </summary>
    /// <param name="error">初始化错误类型。</param>
    /// <param name="message">错误说明文本。</param>
    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"[UnityAdsAdSdk] 初始化失败: {error} {message}");
        isInitialized = false;
        pendingInitCallback?.Invoke(false);
        pendingInitCallback = null;
    }

    /// <summary>
    /// 广告素材加载成功回调，随后开始展示。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (!isLoading || placementId != activePlacementId)
        {
            return;
        }

        isLoading = false;
        isShowing = true;
        Advertisement.Show(placementId, this);
    }

    /// <summary>
    /// 广告素材加载失败回调。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="error">加载错误类型。</param>
    /// <param name="message">错误说明文本。</param>
    public void OnUnityAdsAdFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        if (placementId != activePlacementId)
        {
            return;
        }

        isLoading = false;
        Debug.LogWarning($"[UnityAdsAdSdk] 加载失败: {placementId} {error} {message}");
        CompletePending(AdShowResult.Failed, message ?? error.ToString());
    }

    /// <summary>
    /// 广告展示失败回调。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="error">展示错误类型。</param>
    /// <param name="message">错误说明文本。</param>
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        if (placementId != activePlacementId)
        {
            return;
        }

        isShowing = false;
        Debug.LogWarning($"[UnityAdsAdSdk] 展示失败: {placementId} {error} {message}");
        CompletePending(AdShowResult.Failed, message ?? error.ToString());
    }

    /// <summary>
    /// 广告开始展示回调。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    public void OnUnityAdsShowStart(string placementId) { }

    /// <summary>
    /// 广告被点击回调。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    public void OnUnityAdsShowClick(string placementId) { }

    /// <summary>
    /// 广告展示完成回调，根据完成状态判定是否完整观看。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="showCompletionState">展示完成状态。</param>
    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        if (placementId != activePlacementId)
        {
            return;
        }

        isShowing = false;
        AdShowResult result = showCompletionState == UnityAdsShowCompletionState.COMPLETED
            ? AdShowResult.Completed
            : AdShowResult.Skipped;
        string message = result == AdShowResult.Completed ? string.Empty : "未完整观看广告";
        CompletePending(result, message);
    }

    /// <summary>
    /// 完成待处理的展示回调并清理状态。
    /// </summary>
    /// <param name="result">展示结果。</param>
    /// <param name="message">结果说明文本。</param>
    private void CompletePending(AdShowResult result, string message)
    {
        isShowing = false;
        isLoading = false;
        activePlacementId = null;
        Action<AdShowResult, string> callback = pendingFinishCallback;
        pendingFinishCallback = null;
        callback?.Invoke(result, message);
    }

    /// <summary>
    /// 旧版加载失败接口（未实现，由 <see cref="OnUnityAdsAdFailedToLoad"/> 替代）。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="error">加载错误类型。</param>
    /// <param name="message">错误说明文本。</param>
    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Unity Ads SDK 工厂。
/// </summary>
public sealed class UnityAdsAdSdkFactory : IAdSdkFactory
{
    /// <summary>工厂对应的广告网络类型。</summary>
    public AdNetworkKind NetworkKind => AdNetworkKind.UnityAds;

    /// <summary>
    /// 创建 Unity Ads SDK 实例。
    /// </summary>
    /// <param name="coroutineHost">协程宿主（Unity Ads 适配器不使用，可为任意 MonoBehaviour）。</param>
    /// <returns>新创建的 <see cref="UnityAdsAdSdk"/> 实例。</returns>
    public IAdSdk Create(MonoBehaviour coroutineHost) => new UnityAdsAdSdk();
}
