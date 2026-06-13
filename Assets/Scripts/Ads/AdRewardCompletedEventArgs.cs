/// <summary>
/// 激励广告完成并发奖后的事件负载。
/// </summary>
public readonly struct AdRewardCompletedEventArgs
{
    /// <summary>广告发奖业务来源。</summary>
    public AdRewardSource Source { get; }

    /// <summary>业务上下文 ID（如商品配置 ID）。</summary>
    public string ContextId { get; }

    /// <summary>广告位 Placement ID。</summary>
    public string PlacementId { get; }

    /// <summary>
    /// 创建激励广告完成事件参数。
    /// </summary>
    /// <param name="source">广告发奖业务来源。</param>
    /// <param name="contextId">业务上下文 ID。</param>
    /// <param name="placementId">广告位 Placement ID。</param>
    public AdRewardCompletedEventArgs(AdRewardSource source, string contextId, string placementId)
    {
        Source = source;
        ContextId = contextId ?? string.Empty;
        PlacementId = placementId ?? string.Empty;
    }
}
