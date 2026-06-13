/// <summary>
/// 商店购买失败事件负载。
/// </summary>
public readonly struct ShopPurchaseFailedEventArgs
{
    /// <summary>失败商品配置 ID。</summary>
    public string ItemConfigId { get; }

    /// <summary>失败原因枚举。</summary>
    public ShopPurchaseFailedReason Reason { get; }

    /// <summary>面向 UI 的失败说明文本。</summary>
    public string Message { get; }

    /// <summary>
    /// 创建商店购买失败事件参数。
    /// </summary>
    /// <param name="itemConfigId">商品配置 ID。</param>
    /// <param name="reason">失败原因。</param>
    /// <param name="message">失败说明文本。</param>
    public ShopPurchaseFailedEventArgs(string itemConfigId, ShopPurchaseFailedReason reason, string message)
    {
        ItemConfigId = itemConfigId ?? string.Empty;
        Reason = reason;
        Message = message ?? string.Empty;
    }
}
