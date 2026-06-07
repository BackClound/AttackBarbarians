using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Mock 广告 SDK，用于编辑器与无凭证环境。
/// </summary>
public sealed class MockAdSdk : IAdSdk
{
    private readonly MonoBehaviour host;
    private AdConfigSO config;
    private bool isInitialized;
    private bool isShowing;
    private Coroutine showRoutine;

    public AdNetworkKind NetworkKind => AdNetworkKind.Mock;

    public string DisplayName => "Mock";

    public bool IsInitialized => isInitialized;

    public bool IsShowing => isShowing;

    public MockAdSdk(MonoBehaviour coroutineHost)
    {
        host = coroutineHost;
    }

    public void Initialize(AdConfigSO adConfig, Action<bool> onInitialized)
    {
        config = adConfig;
        isInitialized = true;
        onInitialized?.Invoke(true);
    }

    public bool IsRewardedReady(string placementId) => isInitialized && !isShowing;

    public void ShowRewarded(string placementId, Action<AdShowResult, string> onFinished)
    {
        if (!isInitialized)
        {
            onFinished?.Invoke(AdShowResult.NotReady, "Mock 广告未初始化");
            return;
        }

        if (isShowing)
        {
            onFinished?.Invoke(AdShowResult.AlreadyShowing, "广告播放中");
            return;
        }

        if (host == null)
        {
            onFinished?.Invoke(AdShowResult.Failed, "Mock 宿主丢失");
            return;
        }

        if (showRoutine != null)
        {
            host.StopCoroutine(showRoutine);
        }

        showRoutine = host.StartCoroutine(ShowRoutine(placementId, onFinished));
    }

    public void Shutdown()
    {
        if (showRoutine != null && host != null)
        {
            host.StopCoroutine(showRoutine);
            showRoutine = null;
        }

        isShowing = false;
        isInitialized = false;
        config = null;
    }

    private IEnumerator ShowRoutine(string placementId, Action<AdShowResult, string> onFinished)
    {
        isShowing = true;
        float delay = config != null ? config.MockAdDelaySeconds : 0.35f;
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        isShowing = false;
        showRoutine = null;

        if (config != null && config.MockSimulateSkip)
        {
            onFinished?.Invoke(AdShowResult.Skipped, $"Mock 跳过 placement={placementId}");
            yield break;
        }

        onFinished?.Invoke(AdShowResult.Completed, string.Empty);
    }
}

/// <summary>
/// Mock SDK 工厂。
/// </summary>
public sealed class MockAdSdkFactory : IAdSdkFactory
{
    public AdNetworkKind NetworkKind => AdNetworkKind.Mock;

    public IAdSdk Create(MonoBehaviour coroutineHost) => new MockAdSdk(coroutineHost);
}
