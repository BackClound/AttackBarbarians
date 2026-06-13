/// <summary>
/// 激励广告失败事件负载。
/// </summary>
public readonly struct AdRewardFailedEventArgs
{
    /// <summary>广告发奖业务来源。</summary>
    public AdRewardSource Source { get; }

    /// <summary>业务上下文 ID（如商品配置 ID）。</summary>
    public string ContextId { get; }

    /// <summary>广告位 Placement ID。</summary>
    public string PlacementId { get; }

    /// <summary>失败原因枚举。</summary>
    public AdRewardFailedReason Reason { get; }

    /// <summary>面向 UI 的失败说明文本。</summary>
    public string Message { get; }

    /// <summary>
    /// 创建激励广告失败事件参数。
    /// </summary>
    /// <param name="source">广告发奖业务来源。</param>
    /// <param name="contextId">业务上下文 ID。</param>
    /// <param name="placementId">广告位 Placement ID。</param>
    /// <param name="reason">失败原因。</param>
    /// <param name="message">失败说明文本。</param>
    public AdRewardFailedEventArgs(
        AdRewardSource source,
        string contextId,
        string placementId,
        AdRewardFailedReason reason,
        string message)
    {
        Source = source;
        ContextId = contextId ?? string.Empty;
        PlacementId = placementId ?? string.Empty;
        Reason = reason;
        Message = message ?? string.Empty;
    }
}
