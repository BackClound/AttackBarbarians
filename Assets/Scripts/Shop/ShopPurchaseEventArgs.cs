/// <summary>
/// 商店购买成功事件负载。
/// </summary>
public readonly struct ShopPurchaseEventArgs
{
    public string ItemConfigId { get; }
    public ShopRewardType RewardType { get; }
    public long RewardAmount { get; }
    public CurrencyType PriceCurrency { get; }
    public long PricePaid { get; }

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
