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

    /// <inheritdoc />
    public AdNetworkKind NetworkKind => AdNetworkKind.Mock;

    /// <inheritdoc />
    public string DisplayName => "Mock";

    /// <inheritdoc />
    public bool IsInitialized => isInitialized;

    /// <inheritdoc />
    public bool IsShowing => isShowing;

    /// <summary>
    /// 创建 Mock 广告 SDK 实例。
    /// </summary>
    /// <param name="coroutineHost">用于播放模拟广告的协程宿主。</param>
    public MockAdSdk(MonoBehaviour coroutineHost)
    {
        host = coroutineHost;
    }

    /// <summary>
    /// 初始化 Mock 广告 SDK。
    /// </summary>
    /// <param name="adConfig">广告运行时配置。</param>
    /// <param name="onInitialized">初始化完成回调，参数为是否成功。</param>
    public void Initialize(AdConfigSO adConfig, Action<bool> onInitialized)
    {
        config = adConfig;
        isInitialized = true;
        onInitialized?.Invoke(true);
    }

    /// <summary>
    /// 检查指定激励广告位是否可播放。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <returns>已初始化且未在播放时返回 true。</returns>
    public bool IsRewardedReady(string placementId) => isInitialized && !isShowing;

    /// <summary>
    /// 展示激励视频广告（模拟播放）。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="onFinished">展示结束回调，参数为结果与说明文本。</param>
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

    /// <summary>
    /// 关闭并释放 Mock SDK 资源，停止进行中的模拟播放。
    /// </summary>
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

    /// <summary>
    /// 模拟广告播放协程。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="onFinished">播放结束回调。</param>
    /// <returns>协程迭代器。</returns>
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
    /// <inheritdoc />
    public AdNetworkKind NetworkKind => AdNetworkKind.Mock;

    /// <summary>
    /// 创建 Mock 广告 SDK 实例。
    /// </summary>
    /// <param name="coroutineHost">用于播放模拟广告的协程宿主。</param>
    /// <returns>新创建的 <see cref="MockAdSdk"/> 实例。</returns>
    public IAdSdk Create(MonoBehaviour coroutineHost) => new MockAdSdk(coroutineHost);
}
