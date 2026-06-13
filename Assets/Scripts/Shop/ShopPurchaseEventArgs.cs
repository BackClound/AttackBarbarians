/// <summary>
/// 商店购买成功事件负载。
/// </summary>
public readonly struct ShopPurchaseEventArgs
{
    /// <summary>商品配置 ID。</summary>
    public string ItemConfigId { get; }

    /// <summary>发放的奖励类型。</summary>
    public ShopRewardType RewardType { get; }

    /// <summary>发放的奖励数量。</summary>
    public long RewardAmount { get; }

    /// <summary>支付使用的货币类型。</summary>
    public CurrencyType PriceCurrency { get; }

    /// <summary>实际支付的货币数量（免费为 0）。</summary>
    public long PricePaid { get; }

    /// <summary>
    /// 创建商店购买成功事件参数。
    /// </summary>
    /// <param name="itemConfigId">商品配置 ID。</param>
    /// <param name="rewardType">奖励类型。</param>
    /// <param name="rewardAmount">奖励数量。</param>
    /// <param name="priceCurrency">支付货币类型。</param>
    /// <param name="pricePaid">支付数量。</param>
    public ShopPurchaseEventArgs(
        string itemConfigId,
        ShopRewardType rewardType,
        long rewardAmount,
        CurrencyType priceCurrency,
        long pricePaid)
    {
        ItemConfigId = itemConfigId ?? string.Empty;
        RewardType = rewardType;
        RewardAmount = rewardAmount;
        PriceCurrency = priceCurrency;
        PricePaid = pricePaid;
    }
}
