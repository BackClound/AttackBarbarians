/// <summary>
/// 商店商品限购刷新周期。
/// </summary>
public enum ShopRefreshPeriod
{
    /// <summary>不限购或永久累计。</summary>
    None = 0,

    /// <summary>每日 UTC 零点重置。</summary>
    Daily = 1,
}
