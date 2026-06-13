using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 商店商品目录与免费钻石规则。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Shop/ShopCatalog_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "Attack Barbarians/Shop/Shop Catalog")]
public class ShopCatalogSO : ScriptableObject
{
    [SerializeField] private List<ShopItemSO> items = new List<ShopItemSO>(8);

    [Header("Free Diamond")]
    [SerializeField] private float freeDiamondCooldownHours = 12f;
    [SerializeField] private long freeDiamondGrantAmount = 5;

    [Header("Ad Free Supply")]
    [Tooltip("每个商城广告补给位每日可观看广告领取的次数。")]
    [SerializeField] private int adFreeSupplyDailyLimit = 5;

    /// <summary>目录中所有商品条目。</summary>
    public IReadOnlyList<ShopItemSO> Items => items;

    /// <summary>免费钻石领取冷却时长（小时）。</summary>
    public float FreeDiamondCooldownHours => Mathf.Max(0.1f, freeDiamondCooldownHours);

    /// <summary>每次免费领取的钻石数量。</summary>
    public long FreeDiamondGrantAmount => (long)Mathf.Max(1, freeDiamondGrantAmount);

    /// <summary>每个广告补给位每日可领取次数上限。</summary>
    public int AdFreeSupplyDailyLimit => Mathf.Max(1, adFreeSupplyDailyLimit);

    /// <summary>
    /// 按配置 ID 查找商品。
    /// </summary>
    /// <param name="configId">商品配置 ID。</param>
    /// <param name="item">找到的商品配置；未找到时为 null。</param>
    /// <returns>找到返回 true，否则 false。</returns>
    public bool TryGetItem(string configId, out ShopItemSO item)
    {
        item = null;
        if (items == null || string.IsNullOrWhiteSpace(configId))
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            ShopItemSO entry = items[i];
            if (entry != null && entry.ConfigId == configId)
            {
                item = entry;
                return true;
            }
        }

        return false;
    }
}
