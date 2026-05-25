#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成默认商店商品与目录，并写入 ConfigDatabase。
/// </summary>
public static class ShopConfigBootstrapMenu
{
    private const string ShopFolder = "Assets/Resources/Config/Shop";
    private const string CatalogPath = ShopFolder + "/ShopCatalog_Default.asset";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";

    [MenuItem("Attack Barbarians/Shop/Create Default Shop Assets")]
    public static void CreateDefaultShopAssets()
    {
        Directory.CreateDirectory(ShopFolder);

        ShopItemSO goldSmall = CreateOrUpdateItem(
            GameConstants.ConfigIds.ShopGoldPackSmall,
            "废料金补给·小",
            CurrencyType.Diamond,
            10,
            ShopRewardType.Gold,
            500,
            purchaseLimit: 3,
            ShopRefreshPeriod.Daily);

        ShopItemSO goldLarge = CreateOrUpdateItem(
            GameConstants.ConfigIds.ShopGoldPackLarge,
            "废料金补给·大",
            CurrencyType.Diamond,
            45,
            ShopRewardType.Gold,
            3000,
            purchaseLimit: 1,
            ShopRefreshPeriod.Daily);

        ShopItemSO diamondPack = CreateOrUpdateItem(
            GameConstants.ConfigIds.ShopDiamondPack,
            "量子钻兑换",
            CurrencyType.Gold,
            800,
            ShopRewardType.Diamond,
            5,
            purchaseLimit: 5,
            ShopRefreshPeriod.None);

        ShopCatalogSO catalog = CreateOrLoadCatalog();
        SerializedObject catalogSo = new SerializedObject(catalog);
        SerializedProperty itemsProp = catalogSo.FindProperty("items");
        itemsProp.ClearArray();
        AddItemRef(itemsProp, goldSmall);
        AddItemRef(itemsProp, goldLarge);
        AddItemRef(itemsProp, diamondPack);
        catalogSo.FindProperty("freeDiamondCooldownHours").floatValue = 12f;
        catalogSo.FindProperty("freeDiamondGrantAmount").longValue = 5;
        catalogSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);

        RegisterInDatabase(goldSmall, goldLarge, diamondPack);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ShopConfigBootstrap] 默认商店配置已创建。请将 ShopPanel 绑定到 UIManager，并配置 itemBindings。");
    }

    private static ShopItemSO CreateOrUpdateItem(
        string configId,
        string displayName,
        CurrencyType priceCurrency,
        long priceAmount,
        ShopRewardType rewardType,
        long rewardAmount,
        int purchaseLimit,
        ShopRefreshPeriod refreshPeriod)
    {
        string safeName = configId.Replace('.', '_');
        string path = $"{ShopFolder}/ShopItem_{safeName}.asset";

        ShopItemSO item = AssetDatabase.LoadAssetAtPath<ShopItemSO>(path);
        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ShopItemSO>();
            AssetDatabase.CreateAsset(item, path);
        }

        SerializedObject so = new SerializedObject(item);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("priceCurrency").enumValueIndex = (int)priceCurrency;
        so.FindProperty("priceAmount").longValue = priceAmount;
        so.FindProperty("purchaseLimit").intValue = purchaseLimit;
        so.FindProperty("refreshPeriod").enumValueIndex = (int)refreshPeriod;
        so.FindProperty("rewardType").enumValueIndex = (int)rewardType;
        so.FindProperty("rewardAmount").longValue = rewardAmount;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
        return item;
    }

    private static ShopCatalogSO CreateOrLoadCatalog()
    {
        ShopCatalogSO catalog = AssetDatabase.LoadAssetAtPath<ShopCatalogSO>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ShopCatalogSO>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        return catalog;
    }

    private static void AddItemRef(SerializedProperty listProp, ShopItemSO item)
    {
        int index = listProp.arraySize;
        listProp.InsertArrayElementAtIndex(index);
        listProp.GetArrayElementAtIndex(index).objectReferenceValue = item;
    }

    private static void RegisterInDatabase(params ShopItemSO[] items)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[ShopConfigBootstrap] 未找到 ConfigDatabase，跳过注册。");
            return;
        }

        SerializedObject dbSo = new SerializedObject(database);
        SerializedProperty shopProp = dbSo.FindProperty("shopItems");
        if (shopProp == null)
        {
            Debug.LogWarning("[ShopConfigBootstrap] ConfigDatabase 缺少 shopItems 字段。");
            return;
        }

        var existing = new HashSet<ShopItemSO>();
        for (int i = 0; i < shopProp.arraySize; i++)
        {
            ShopItemSO entry = shopProp.GetArrayElementAtIndex(i).objectReferenceValue as ShopItemSO;
            if (entry != null)
            {
                existing.Add(entry);
            }
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && !existing.Contains(items[i]))
            {
                int index = shopProp.arraySize;
                shopProp.InsertArrayElementAtIndex(index);
                shopProp.GetArrayElementAtIndex(index).objectReferenceValue = items[i];
            }
        }

        dbSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }
}
#endif
