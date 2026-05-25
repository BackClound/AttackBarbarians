/// <summary>
/// 商店购买失败事件负载。
/// </summary>
public readonly struct ShopPurchaseFailedEventArgs
{
    public string ItemConfigId { get; }
    public ShopPurchaseFailedReason Reason { get; }
    public string Message { get; }

    public ShopPurchaseFailedEventArgs(string itemConfigId, ShopPurchaseFailedReason reason, string message)
    {
        ItemConfigId = itemConfigId ?? string.Empty;
        Reason = reason;
        Message = message ?? string.Empty;
    }
}
