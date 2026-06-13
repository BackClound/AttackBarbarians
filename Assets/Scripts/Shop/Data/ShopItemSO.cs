using UnityEngine;

/// <summary>
/// 商店商品配置：价格、限购、奖励内容。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Shop/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "ShopItem", menuName = "Attack Barbarians/Shop/Shop Item")]
public class ShopItemSO : ConfigDataBase
{
    [Header("Price")]
    [SerializeField] private CurrencyType priceCurrency = CurrencyType.Gold;
    [SerializeField] private long priceAmount = 100;

    [Header("Limit")]
    [SerializeField] private int purchaseLimit;
    [SerializeField] private ShopRefreshPeriod refreshPeriod = ShopRefreshPeriod.None;

    [Header("Reward")]
    [SerializeField] private ShopRewardType rewardType = ShopRewardType.Gold;
    [SerializeField] private long rewardAmount = 500;
    [SerializeField] private string rewardConfigId;

    /// <summary>购买所需货币类型。</summary>
    public CurrencyType PriceCurrency => priceCurrency;

    /// <summary>购买价格（不小于 0）。</summary>
    public long PriceAmount => (long)Mathf.Max(0, priceAmount);

    /// <summary>限购次数（0 表示不限购）。</summary>
    public int PurchaseLimit => Mathf.Max(0, purchaseLimit);

    /// <summary>限购计数刷新周期。</summary>
    public ShopRefreshPeriod RefreshPeriod => refreshPeriod;

    /// <summary>购买后发放的奖励类型。</summary>
    public ShopRewardType RewardType => rewardType;

    /// <summary>奖励数量（不小于 0）。</summary>
    public long RewardAmount => (long)Mathf.Max(0, rewardAmount);

    /// <summary>奖励附加配置 ID（如升级卡卡池）。</summary>
    public string RewardConfigId => rewardConfigId;

    /// <summary>是否启用限购。</summary>
    public bool HasPurchaseLimit => purchaseLimit > 0;

    /// <summary>
    /// 收集配置校验错误。
    /// </summary>
    /// <param name="result">校验结果收集器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);
        if (rewardAmount <= 0)
        {
            result.AddError(name, "rewardAmount 必须大于 0。");
        }
    }
}
