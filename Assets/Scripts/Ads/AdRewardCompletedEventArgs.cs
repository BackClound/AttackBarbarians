/// <summary>
/// 激励广告完成并发奖后的事件负载。
/// </summary>
public readonly struct AdRewardCompletedEventArgs
{
    public AdRewardSource Source { get; }
    public string ContextId { get; }
    public string PlacementId { get; }

    public AdRewardCompletedEventArgs(AdRewardSource source, string contextId, string placementId)
    {
        Source = source;
        ContextId = contextId ?? string.Empty;
        PlacementId = placementId ?? string.Empty;
    }
}
