/// <summary>
/// 商店购买失败原因。
/// </summary>
public enum ShopPurchaseFailedReason
{
    /// <summary>无失败（默认值）。</summary>
    None = 0,

    /// <summary>商店服务尚未初始化。</summary>
    NotInitialized = 1,

    /// <summary>未找到对应商品配置。</summary>
    ItemNotFound = 2,

    /// <summary>已达限购次数或冷却中。</summary>
    PurchaseLimitReached = 3,

    /// <summary>货币或广告券余额不足。</summary>
    InsufficientFunds = 4,

    /// <summary>商品或奖励配置无效。</summary>
    InvalidConfiguration = 5,
}
