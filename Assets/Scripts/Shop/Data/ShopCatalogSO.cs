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

    public IReadOnlyList<ShopItemSO> Items => items;
    public float FreeDiamondCooldownHours => Mathf.Max(0.1f, freeDiamondCooldownHours);
    public long FreeDiamondGrantAmount => (long)Mathf.Max(1, freeDiamondGrantAmount);

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
