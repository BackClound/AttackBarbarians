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

    public AdNetworkKind NetworkKind => AdNetworkKind.UnityAds;

    public string DisplayName => "Unity Ads";

    public bool IsInitialized => isInitialized;

    public bool IsShowing => isShowing;

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

    public bool IsRewardedReady(string placementId)
    {
        if (!isInitialized || isShowing || isLoading)
        {
            return false;
        }

        return Advertisement.isInitialized;
    }

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

    public void Shutdown()
    {
        CompletePending(AdShowResult.Failed, "广告服务已关闭");
        isInitialized = false;
        isLoading = false;
        config = null;
        pendingInitCallback = null;
    }

    public void OnInitializationComplete()
    {
        isInitialized = true;
        pendingInitCallback?.Invoke(true);
        pendingInitCallback = null;
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogWarning($"[UnityAdsAdSdk] 初始化失败: {error} {message}");
        isInitialized = false;
        pendingInitCallback?.Invoke(false);
        pendingInitCallback = null;
    }

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

    public void OnUnityAdsShowStart(string placementId) { }

    public void OnUnityAdsShowClick(string placementId) { }

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

    private void CompletePending(AdShowResult result, string message)
    {
        isShowing = false;
        isLoading = false;
        activePlacementId = null;
        Action<AdShowResult, string> callback = pendingFinishCallback;
        pendingFinishCallback = null;
        callback?.Invoke(result, message);
    }

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
    public AdNetworkKind NetworkKind => AdNetworkKind.UnityAds;

    public IAdSdk Create(MonoBehaviour coroutineHost) => new UnityAdsAdSdk();
}
