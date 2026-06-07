/// <summary>
/// 激励广告失败事件负载。
/// </summary>
public readonly struct AdRewardFailedEventArgs
{
    public AdRewardSource Source { get; }
    public string ContextId { get; }
    public string PlacementId { get; }
    public AdRewardFailedReason Reason { get; }
    public string Message { get; }

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
