/// <summary>
/// 商店购买失败原因。
/// </summary>
public enum ShopPurchaseFailedReason
{
    None = 0,
    NotInitialized = 1,
    ItemNotFound = 2,
    PurchaseLimitReached = 3,
    InsufficientFunds = 4,
    InvalidConfiguration = 5,
}
