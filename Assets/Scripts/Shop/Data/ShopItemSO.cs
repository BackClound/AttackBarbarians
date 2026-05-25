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

    public CurrencyType PriceCurrency => priceCurrency;
    public long PriceAmount => (long)Mathf.Max(0, priceAmount);
    public int PurchaseLimit => Mathf.Max(0, purchaseLimit);
    public ShopRefreshPeriod RefreshPeriod => refreshPeriod;
    public ShopRewardType RewardType => rewardType;
    public long RewardAmount => (long)Mathf.Max(0, rewardAmount);
    public string RewardConfigId => rewardConfigId;

    public bool HasPurchaseLimit => purchaseLimit > 0;

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);
        if (rewardAmount <= 0)
        {
            result.AddError(name, "rewardAmount 必须大于 0。");
        }
    }
}
