using System;

/// <summary>
/// 统一广告 SDK 接口。各广告商实现本接口，由 <see cref="AdSdkRegistry"/> 按 <see cref="AdNetworkKind"/> 创建实例。
/// </summary>
public interface IAdSdk
{
    /// <summary>所属广告网络类型。</summary>
    AdNetworkKind NetworkKind { get; }

    /// <summary>SDK 显示名称（用于日志与调试）。</summary>
    string DisplayName { get; }

    /// <summary>SDK 是否已完成初始化。</summary>
    bool IsInitialized { get; }

    /// <summary>是否正在展示广告。</summary>
    bool IsShowing { get; }

    /// <summary>
    /// 初始化广告 SDK。
    /// </summary>
    /// <param name="config">广告运行时配置。</param>
    /// <param name="onInitialized">初始化完成回调，参数为是否成功。</param>
    void Initialize(AdConfigSO config, Action<bool> onInitialized);

    /// <summary>
    /// 检查指定激励广告位是否可播放。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <returns>可播放返回 true，否则 false。</returns>
    bool IsRewardedReady(string placementId);

    /// <summary>
    /// 展示激励视频广告。
    /// </summary>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="onFinished">展示结束回调，参数为结果与说明文本。</param>
    void ShowRewarded(string placementId, Action<AdShowResult, string> onFinished);

    /// <summary>
    /// 关闭并释放 SDK 资源。
    /// </summary>
    void Shutdown();
}
