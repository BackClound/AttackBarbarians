#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成默认成就与七日签到配置，并写入 ConfigDatabase。
/// </summary>
public static class AchievementDailyConfigBootstrapMenu
{
    private const string AchievementFolder = "Assets/Resources/Config/Achievement";
    private const string DailyFolder = "Assets/Resources/Config/DailyReward";
    private const string AchievementCatalogPath = AchievementFolder + "/AchievementCatalog_Default.asset";
    private const string DailyCatalogPath = DailyFolder + "/DailyRewardCatalog_Default.asset";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";

    [MenuItem("Attack Barbarians/Meta/Create Default Achievement & Daily Reward Assets")]
    public static void CreateDefaultAssets()
    {
        Directory.CreateDirectory(AchievementFolder);
        Directory.CreateDirectory(DailyFolder);

        AchievementDataSO firstBlood = CreateOrUpdateAchievement(
            GameConstants.ConfigIds.AchievementFirstBlood,
            "初战告捷",
            "累计击杀 10 名敌人",
            AchievementTargetType.TotalEnemyKills,
            10,
            ShopRewardType.Gold,
            200);

        AchievementDataSO wave5 = CreateOrUpdateAchievement(
            GameConstants.ConfigIds.AchievementWave5,
            "波次突破",
            "最高波次达到 5",
            AchievementTargetType.HighestWaveReached,
            5,
            ShopRewardType.Gold,
            500);

        AchievementDataSO bossSlayer = CreateOrUpdateAchievement(
            GameConstants.ConfigIds.AchievementBossSlayer,
            "首领猎手",
            "击败 1 名 Boss",
            AchievementTargetType.TotalBossDefeats,
            1,
            ShopRewardType.Diamond,
            3);

        AchievementDataSO goldCollector = CreateOrUpdateAchievement(
            GameConstants.ConfigIds.AchievementGoldCollector,
            "废料囤积者",
            "累计获得 1000 废料金",
            AchievementTargetType.LifetimeGoldEarned,
            1000,
            ShopRewardType.Gold,
            800);

        AchievementCatalogSO achievementCatalog = CreateOrLoadAchievementCatalog();
        SerializedObject achievementCatalogSo = new SerializedObject(achievementCatalog);
        SerializedProperty achievementsProp = achievementCatalogSo.FindProperty("achievements");
        achievementsProp.ClearArray();
        AddRef(achievementsProp, firstBlood);
        AddRef(achievementsProp, wave5);
        AddRef(achievementsProp, bossSlayer);
        AddRef(achievementsProp, goldCollector);
        achievementCatalogSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(achievementCatalog);

        DailyRewardEntrySO[] dailyEntries = CreateDailyEntries();
        DailyRewardCatalogSO dailyCatalog = CreateOrLoadDailyCatalog();
        SerializedObject dailyCatalogSo = new SerializedObject(dailyCatalog);
        SerializedProperty entriesProp = dailyCatalogSo.FindProperty("entries");
        entriesProp.ClearArray();
        for (int i = 0; i < dailyEntries.Length; i++)
        {
            AddRef(entriesProp, dailyEntries[i]);
        }

        dailyCatalogSo.FindProperty("allowMakeup").boolValue = true;
        dailyCatalogSo.FindProperty("makeupDiamondCost").longValue = 10;
        dailyCatalogSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(dailyCatalog);

        RegisterInDatabase(firstBlood, wave5, bossSlayer, goldCollector, dailyEntries);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            "[AchievementDailyConfigBootstrap] 默认成就与签到配置已创建。" +
            "请在 GameSystems 挂载 AchievementManager、DailyRewardManager，并在 UIManager 绑定面板。");
    }

    private static AchievementDataSO CreateOrUpdateAchievement(
        string configId,
        string displayName,
        string description,
        AchievementTargetType targetType,
        int targetValue,
        ShopRewardType rewardType,
        long rewardAmount)
    {
        string safeName = configId.Replace('.', '_');
        string path = $"{AchievementFolder}/Achievement_{safeName}.asset";

        AchievementDataSO data = AssetDatabase.LoadAssetAtPath<AchievementDataSO>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<AchievementDataSO>();
            AssetDatabase.CreateAsset(data, path);
        }

        SerializedObject so = new SerializedObject(data);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("targetType").enumValueIndex = (int)targetType;
        so.FindProperty("targetValue").intValue = targetValue;
        so.FindProperty("rewardType").enumValueIndex = (int)rewardType;
        so.FindProperty("rewardAmount").longValue = rewardAmount;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(data);
        return data;
    }

    private static DailyRewardEntrySO[] CreateDailyEntries()
    {
        var entries = new DailyRewardEntrySO[7];
        long[] goldRewards = { 100, 150, 200, 250, 300, 400, 500 };
        for (int day = 1; day <= 7; day++)
        {
            ShopRewardType rewardType = day == 7 ? ShopRewardType.Diamond : ShopRewardType.Gold;
            long amount = day == 7 ? 5 : goldRewards[day - 1];
            entries[day - 1] = CreateOrUpdateDailyEntry(day, rewardType, amount);
        }

        return entries;
    }

    private static DailyRewardEntrySO CreateOrUpdateDailyEntry(int dayIndex, ShopRewardType rewardType, long amount)
    {
        string configId = $"daily_reward.day_{dayIndex}";
        string path = $"{DailyFolder}/DailyReward_day_{dayIndex}.asset";

        DailyRewardEntrySO entry = AssetDatabase.LoadAssetAtPath<DailyRewardEntrySO>(path);
        if (entry == null)
        {
            entry = ScriptableObject.CreateInstance<DailyRewardEntrySO>();
            AssetDatabase.CreateAsset(entry, path);
        }

        SerializedObject so = new SerializedObject(entry);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = $"第 {dayIndex} 日";
        so.FindProperty("dayIndex").intValue = dayIndex;
        so.FindProperty("rewardType").enumValueIndex = (int)rewardType;
        so.FindProperty("rewardAmount").longValue = amount;
        so.FindProperty("upgradeCardPoolConfigId").stringValue = UpgradeCardConstants.PoolIds.DailyReward;
        so.FindProperty("upgradeCardDrawCount").intValue = dayIndex == 7 ? 2 : 1;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(entry);
        return entry;
    }

    private static AchievementCatalogSO CreateOrLoadAchievementCatalog()
    {
        AchievementCatalogSO catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalogSO>(AchievementCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AchievementCatalogSO>();
            AssetDatabase.CreateAsset(catalog, AchievementCatalogPath);
        }

        return catalog;
    }

    private static DailyRewardCatalogSO CreateOrLoadDailyCatalog()
    {
        DailyRewardCatalogSO catalog = AssetDatabase.LoadAssetAtPath<DailyRewardCatalogSO>(DailyCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<DailyRewardCatalogSO>();
            AssetDatabase.CreateAsset(catalog, DailyCatalogPath);
        }

        return catalog;
    }

    private static void AddRef(SerializedProperty listProp, Object asset)
    {
        int index = listProp.arraySize;
        listProp.InsertArrayElementAtIndex(index);
        listProp.GetArrayElementAtIndex(index).objectReferenceValue = asset;
    }

    private static void RegisterInDatabase(
        AchievementDataSO a1,
        AchievementDataSO a2,
        AchievementDataSO a3,
        AchievementDataSO a4,
        DailyRewardEntrySO[] dailyEntries)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[AchievementDailyConfigBootstrap] 未找到 ConfigDatabase，跳过注册。");
            return;
        }

        SerializedObject dbSo = new SerializedObject(database);
        MergeUnique(dbSo.FindProperty("achievements"), a1, a2, a3, a4);
        if (dailyEntries != null)
        {
            var dailyObjects = new Object[dailyEntries.Length];
            for (int i = 0; i < dailyEntries.Length; i++)
            {
                dailyObjects[i] = dailyEntries[i];
            }

            MergeUnique(dbSo.FindProperty("dailyRewardEntries"), dailyObjects);
        }

        dbSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void MergeUnique(SerializedProperty listProp, params Object[] assets)
    {
        if (listProp == null)
        {
            return;
        }

        var existing = new HashSet<Object>();
        for (int i = 0; i < listProp.arraySize; i++)
        {
            Object entry = listProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (entry != null)
            {
                existing.Add(entry);
            }
        }

        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i] != null && !existing.Contains(assets[i]))
            {
                int index = listProp.arraySize;
                listProp.InsertArrayElementAtIndex(index);
                listProp.GetArrayElementAtIndex(index).objectReferenceValue = assets[i];
            }
        }
    }
}
#endif
