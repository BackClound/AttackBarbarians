#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成全部升级卡、奖励池，并写入 ConfigDatabase；同时更新商店宝箱与七日签到奖励引用。
/// </summary>
public static class UpgradeCardConfigBootstrapMenu
{
    private const string CardFolder = "Assets/Resources/Config/UpgradeCard";
    private const string PoolFolder = CardFolder + "/Pools";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";
    private const string ShopFolder = "Assets/Resources/Config/Shop";
    private const string DailyFolder = "Assets/Resources/Config/DailyReward";

    [MenuItem("Attack Barbarians/Upgrade Card/Create All Upgrade Cards & Reward Pools")]
    public static void CreateAllUpgradeCardAssets()
    {
        Directory.CreateDirectory(CardFolder);
        Directory.CreateDirectory(PoolFolder);

        var cards = CreateAllCards();
        var pools = CreateAllPools(cards);

        RegisterInDatabase(cards, pools);
        UpdateShopCrates();
        UpdateDailyRewards();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[UpgradeCardConfigBootstrap] 已创建 13 张升级卡与 8 个奖励池。" +
            "请在 GameSystems 确认 UpgradeCardManager、MetaRewardService 已挂载。");
    }

    private static Dictionary<string, UpgradeCardSO> CreateAllCards()
    {
        var cards = new Dictionary<string, UpgradeCardSO>();

        cards[UpgradeCardConstants.CardIds.SkillShoot] = CreateSkillCard(
            UpgradeCardConstants.CardIds.SkillShoot, "射击技能卡", GameConstants.ConfigIds.SkillShoot);
        cards[UpgradeCardConstants.CardIds.SkillFireRain] = CreateSkillCard(
            UpgradeCardConstants.CardIds.SkillFireRain, "火雨技能卡", GameConstants.ConfigIds.SkillFireRain);
        cards[UpgradeCardConstants.CardIds.SkillIce] = CreateSkillCard(
            UpgradeCardConstants.CardIds.SkillIce, "冰冻技能卡", GameConstants.ConfigIds.SkillIce);
        cards[UpgradeCardConstants.CardIds.SkillLightning] = CreateSkillCard(
            UpgradeCardConstants.CardIds.SkillLightning, "闪电技能卡", GameConstants.ConfigIds.SkillLightning);
        cards[UpgradeCardConstants.CardIds.SkillThunder] = CreateSkillCard(
            UpgradeCardConstants.CardIds.SkillThunder, "雷鸣技能卡", GameConstants.ConfigIds.SkillThunder);
        cards[UpgradeCardConstants.CardIds.SkillWaterWave] = CreateSkillCard(
            UpgradeCardConstants.CardIds.SkillWaterWave, "水波技能卡", GameConstants.ConfigIds.SkillWaterWave);
        cards[UpgradeCardConstants.CardIds.SkillHeal] = CreateSkillCard(
            UpgradeCardConstants.CardIds.SkillHeal, "治疗技能卡", GameConstants.ConfigIds.SkillHeal);
        cards[UpgradeCardConstants.CardIds.SkillGeneric] = CreateGenericSkillCard();

        cards[UpgradeCardConstants.CardIds.AttrMaxHp] = CreateAttributeCard(
            UpgradeCardConstants.CardIds.AttrMaxHp, "生命属性卡", StatType.MaxHp);
        cards[UpgradeCardConstants.CardIds.AttrDamage] = CreateAttributeCard(
            UpgradeCardConstants.CardIds.AttrDamage, "攻击属性卡", StatType.Damage);
        cards[UpgradeCardConstants.CardIds.AttrMoveSpeed] = CreateAttributeCard(
            UpgradeCardConstants.CardIds.AttrMoveSpeed, "移速属性卡", StatType.MoveSpeed);
        cards[UpgradeCardConstants.CardIds.AttrAttackSpeed] = CreateAttributeCard(
            UpgradeCardConstants.CardIds.AttrAttackSpeed, "攻速属性卡", StatType.AttackSpeed);
        cards[UpgradeCardConstants.CardIds.AttrGeneric] = CreateGenericAttributeCard();

        return cards;
    }

    private static Dictionary<string, UpgradeCardRewardPoolSO> CreateAllPools(Dictionary<string, UpgradeCardSO> cards)
    {
        var pools = new Dictionary<string, UpgradeCardRewardPoolSO>();

        pools[UpgradeCardConstants.PoolIds.ShopCrateCommon] = CreatePool(
            UpgradeCardConstants.PoolIds.ShopCrateCommon,
            "普通补给箱奖池",
            1,
            Weighted(
                (cards[UpgradeCardConstants.CardIds.AttrMaxHp], 150),
                (cards[UpgradeCardConstants.CardIds.AttrDamage], 150),
                (cards[UpgradeCardConstants.CardIds.AttrMoveSpeed], 120),
                (cards[UpgradeCardConstants.CardIds.AttrAttackSpeed], 120),
                (cards[UpgradeCardConstants.CardIds.SkillShoot], 90),
                (cards[UpgradeCardConstants.CardIds.SkillLightning], 70),
                (cards[UpgradeCardConstants.CardIds.AttrGeneric], 50),
                (cards[UpgradeCardConstants.CardIds.SkillGeneric], 50)));

        pools[UpgradeCardConstants.PoolIds.ShopCratePremium] = CreatePool(
            UpgradeCardConstants.PoolIds.ShopCratePremium,
            "高级补给箱奖池",
            1,
            Weighted(
                (cards[UpgradeCardConstants.CardIds.SkillFireRain], 110),
                (cards[UpgradeCardConstants.CardIds.SkillIce], 105),
                (cards[UpgradeCardConstants.CardIds.SkillThunder], 100),
                (cards[UpgradeCardConstants.CardIds.SkillWaterWave], 95),
                (cards[UpgradeCardConstants.CardIds.SkillHeal], 90),
                (cards[UpgradeCardConstants.CardIds.SkillLightning], 75),
                (cards[UpgradeCardConstants.CardIds.AttrDamage], 55),
                (cards[UpgradeCardConstants.CardIds.AttrMaxHp], 50),
                (cards[UpgradeCardConstants.CardIds.SkillGeneric], 45),
                (cards[UpgradeCardConstants.CardIds.AttrGeneric], 40)));

        pools[UpgradeCardConstants.PoolIds.OfflineReward] = CreatePool(
            UpgradeCardConstants.PoolIds.OfflineReward,
            "离线收益奖池",
            1,
            Weighted(
                (cards[UpgradeCardConstants.CardIds.AttrMaxHp], 100),
                (cards[UpgradeCardConstants.CardIds.AttrDamage], 100),
                (cards[UpgradeCardConstants.CardIds.AttrMoveSpeed], 80),
                (cards[UpgradeCardConstants.CardIds.AttrAttackSpeed], 80),
                (cards[UpgradeCardConstants.CardIds.AttrGeneric], 50)));

        pools[UpgradeCardConstants.PoolIds.OnlineReward] = CreatePool(
            UpgradeCardConstants.PoolIds.OnlineReward,
            "在线奖励奖池",
            1,
            Weighted(
                (cards[UpgradeCardConstants.CardIds.SkillShoot], 80),
                (cards[UpgradeCardConstants.CardIds.SkillLightning], 70),
                (cards[UpgradeCardConstants.CardIds.SkillHeal], 60),
                (cards[UpgradeCardConstants.CardIds.SkillGeneric], 50),
                (cards[UpgradeCardConstants.CardIds.AttrDamage], 90)));

        pools[UpgradeCardConstants.PoolIds.Lottery] = CreatePool(
            UpgradeCardConstants.PoolIds.Lottery,
            "幸运抽奖奖池",
            1,
            Weighted(
                (cards[UpgradeCardConstants.CardIds.SkillFireRain], 50),
                (cards[UpgradeCardConstants.CardIds.SkillIce], 50),
                (cards[UpgradeCardConstants.CardIds.SkillThunder], 50),
                (cards[UpgradeCardConstants.CardIds.SkillWaterWave], 50),
                (cards[UpgradeCardConstants.CardIds.SkillGeneric], 80),
                (cards[UpgradeCardConstants.CardIds.AttrGeneric], 80),
                (cards[UpgradeCardConstants.CardIds.AttrMaxHp], 70),
                (cards[UpgradeCardConstants.CardIds.AttrDamage], 70)));

        pools[UpgradeCardConstants.PoolIds.DailyReward] = CreatePool(
            UpgradeCardConstants.PoolIds.DailyReward,
            "七日签到奖池",
            1,
            Weighted(
                (cards[UpgradeCardConstants.CardIds.AttrMaxHp], 100),
                (cards[UpgradeCardConstants.CardIds.AttrDamage], 100),
                (cards[UpgradeCardConstants.CardIds.SkillShoot], 80),
                (cards[UpgradeCardConstants.CardIds.SkillLightning], 70),
                (cards[UpgradeCardConstants.CardIds.SkillGeneric], 40)));

        pools[UpgradeCardConstants.PoolIds.RunSettlement] = CreatePool(
            UpgradeCardConstants.PoolIds.RunSettlement,
            "战斗结算奖池",
            1,
            Weighted(
                (cards[UpgradeCardConstants.CardIds.AttrMaxHp], 90),
                (cards[UpgradeCardConstants.CardIds.AttrDamage], 90),
                (cards[UpgradeCardConstants.CardIds.AttrMoveSpeed], 70),
                (cards[UpgradeCardConstants.CardIds.AttrAttackSpeed], 70),
                (cards[UpgradeCardConstants.CardIds.SkillShoot], 60),
                (cards[UpgradeCardConstants.CardIds.SkillLightning], 50)));

        pools[UpgradeCardConstants.PoolIds.StageReward] = CreatePool(
            UpgradeCardConstants.PoolIds.StageReward,
            "通关奖励奖池",
            1,
            Weighted(
                (cards[UpgradeCardConstants.CardIds.SkillFireRain], 60),
                (cards[UpgradeCardConstants.CardIds.SkillIce], 60),
                (cards[UpgradeCardConstants.CardIds.SkillThunder], 60),
                (cards[UpgradeCardConstants.CardIds.SkillHeal], 60),
                (cards[UpgradeCardConstants.CardIds.SkillGeneric], 70),
                (cards[UpgradeCardConstants.CardIds.AttrGeneric], 70)));

        return pools;
    }

    private static UpgradeCardSO CreateSkillCard(string configId, string displayName, string skillConfigId)
    {
        return CreateCard(configId, displayName, UpgradeCardCategory.Skill, UpgradeRarity.Common, skillConfigId, StatType.None);
    }

    private static UpgradeCardSO CreateAttributeCard(string configId, string displayName, StatType statType)
    {
        return CreateCard(configId, displayName, UpgradeCardCategory.Attribute, UpgradeRarity.Common, null, statType);
    }

    private static UpgradeCardSO CreateGenericSkillCard()
    {
        return CreateCard(
            UpgradeCardConstants.CardIds.SkillGeneric,
            "通用技能升级卡",
            UpgradeCardCategory.GenericSkill,
            UpgradeRarity.Rare,
            null,
            StatType.None,
            "随机获得一张技能升级卡");
    }

    private static UpgradeCardSO CreateGenericAttributeCard()
    {
        return CreateCard(
            UpgradeCardConstants.CardIds.AttrGeneric,
            "通用属性升级卡",
            UpgradeCardCategory.GenericAttribute,
            UpgradeRarity.Rare,
            null,
            StatType.None,
            "随机获得一张属性升级卡");
    }

    private static UpgradeCardSO CreateCard(
        string configId,
        string displayName,
        UpgradeCardCategory category,
        UpgradeRarity rarity,
        string skillConfigId,
        StatType statType,
        string description = null)
    {
        string safeName = configId.Replace('.', '_');
        string path = $"{CardFolder}/UpgradeCard_{safeName}.asset";

        UpgradeCardSO card = AssetDatabase.LoadAssetAtPath<UpgradeCardSO>(path);
        if (card == null)
        {
            card = ScriptableObject.CreateInstance<UpgradeCardSO>();
            AssetDatabase.CreateAsset(card, path);
        }

        SerializedObject so = new SerializedObject(card);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("category").enumValueIndex = (int)category;
        so.FindProperty("rarity").enumValueIndex = (int)rarity;
        so.FindProperty("levelsPerCard").intValue = 1;
        so.FindProperty("skillConfigId").stringValue = skillConfigId ?? string.Empty;
        so.FindProperty("targetStat").enumValueIndex = (int)statType;
        so.FindProperty("description").stringValue = description ?? $"{displayName}：提升对应成长 1 级";
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(card);
        return card;
    }

    private static UpgradeCardRewardPoolSO CreatePool(
        string configId,
        string displayName,
        int drawCount,
        List<(UpgradeCardSO card, int weight)> entries)
    {
        string safeName = configId.Replace('.', '_');
        string path = $"{PoolFolder}/UpgradeCardPool_{safeName}.asset";

        UpgradeCardRewardPoolSO pool = AssetDatabase.LoadAssetAtPath<UpgradeCardRewardPoolSO>(path);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<UpgradeCardRewardPoolSO>();
            AssetDatabase.CreateAsset(pool, path);
        }

        SerializedObject so = new SerializedObject(pool);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("drawCount").intValue = drawCount;

        SerializedProperty entriesProp = so.FindProperty("entries");
        entriesProp.ClearArray();
        for (int i = 0; i < entries.Count; i++)
        {
            entriesProp.InsertArrayElementAtIndex(i);
            SerializedProperty entryProp = entriesProp.GetArrayElementAtIndex(i);
            entryProp.FindPropertyRelative("card").objectReferenceValue = entries[i].card;
            entryProp.FindPropertyRelative("weight").intValue = entries[i].weight;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pool);
        return pool;
    }

    private static List<(UpgradeCardSO card, int weight)> Weighted(params (UpgradeCardSO card, int weight)[] entries)
    {
        return new List<(UpgradeCardSO, int)>(entries);
    }

    private static void RegisterInDatabase(
        Dictionary<string, UpgradeCardSO> cards,
        Dictionary<string, UpgradeCardRewardPoolSO> pools)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[UpgradeCardConfigBootstrap] 未找到 ConfigDatabase，跳过注册。");
            return;
        }

        SerializedObject dbSo = new SerializedObject(database);
        ReplaceList(dbSo.FindProperty("upgradeCards"), cards.Values);
        ReplaceList(dbSo.FindProperty("upgradeCardRewardPools"), pools.Values);
        dbSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void ReplaceList<T>(SerializedProperty listProp, IEnumerable<T> values) where T : Object
    {
        if (listProp == null || values == null)
        {
            return;
        }

        listProp.ClearArray();
        int index = 0;
        foreach (T value in values)
        {
            if (value == null)
            {
                continue;
            }

            listProp.InsertArrayElementAtIndex(index);
            listProp.GetArrayElementAtIndex(index).objectReferenceValue = value;
            index++;
        }
    }

    private static void UpdateShopCrates()
    {
        UpdateShopItem(
            GameConstants.ConfigIds.ShopCrateCommonSingle,
            UpgradeCardConstants.PoolIds.ShopCrateCommon,
            1);
        UpdateShopItem(
            GameConstants.ConfigIds.ShopCrateCommonTen,
            UpgradeCardConstants.PoolIds.ShopCrateCommon,
            10);
        UpdateShopItem(
            GameConstants.ConfigIds.ShopCratePremiumSingle,
            UpgradeCardConstants.PoolIds.ShopCratePremium,
            1);
        UpdateShopItem(
            GameConstants.ConfigIds.ShopCratePremiumTen,
            UpgradeCardConstants.PoolIds.ShopCratePremium,
            10);
        UpdateShopItem(
            GameConstants.ConfigIds.ShopAdCrateCommon,
            UpgradeCardConstants.PoolIds.ShopCrateCommon,
            1);
        UpdateShopItem(
            GameConstants.ConfigIds.ShopAdCratePremium,
            UpgradeCardConstants.PoolIds.ShopCratePremium,
            1);
    }

    private static void UpdateShopItem(string configId, string poolConfigId, int drawCount)
    {
        string safeName = configId.Replace('.', '_');
        string path = $"{ShopFolder}/ShopItem_{safeName}.asset";
        ShopItemSO item = AssetDatabase.LoadAssetAtPath<ShopItemSO>(path);
        if (item == null)
        {
            return;
        }

        SerializedObject so = new SerializedObject(item);
        so.FindProperty("rewardConfigId").stringValue = poolConfigId;
        so.FindProperty("rewardAmount").longValue = drawCount;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(item);
    }

    private static void UpdateDailyRewards()
    {
        for (int day = 1; day <= 7; day++)
        {
            string path = $"{DailyFolder}/DailyReward_day_{day}.asset";
            DailyRewardEntrySO entry = AssetDatabase.LoadAssetAtPath<DailyRewardEntrySO>(path);
            if (entry == null)
            {
                continue;
            }

            SerializedObject so = new SerializedObject(entry);
            so.FindProperty("upgradeCardPoolConfigId").stringValue = UpgradeCardConstants.PoolIds.DailyReward;
            so.FindProperty("upgradeCardDrawCount").intValue = day == 7 ? 2 : 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }
    }
}
#endif
