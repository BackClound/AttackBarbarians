#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 一键生成默认升级选项与奖励池，并写入 ConfigDatabase。
/// </summary>
public static class UpgradeConfigBootstrapMenu
{
    private const string UpgradeFolder = "Assets/Resources/Config/Upgrade";

    [MenuItem("Attack Barbarians/Config/Create Default Upgrade Assets")]
    public static void CreateDefaultUpgradeAssets()
    {
        Directory.CreateDirectory(UpgradeFolder);

        var allOptions = new List<UpgradeOptionSO>(160);

        UpgradeOptionSO attackUp = CreateOrLoadOption(
            "UpgradeOption_AttackUp",
            GameConstants.ConfigIds.UpgradeAttackUp,
            "Attack Up",
            "+5 基础伤害",
            UpgradeEffectType.StatBuff,
            buffConfigId: GameConstants.ConfigIds.BuffAttackUp);
        allOptions.Add(attackUp);

        foreach (SkillBuffKind kind in Enum.GetValues(typeof(SkillBuffKind)))
        {
            if (kind == SkillBuffKind.None || !SkillBuffTierSpecUtility.HasMultipleTiers(kind))
            {
                continue;
            }

            int maxTier = SkillBuffTierSpecUtility.GetMaxTier(kind);
            for (int tier = 1; tier <= maxTier; tier++)
            {
                string fileName = SkillBuffTierSpecUtility.BuildUpgradeFileName(kind, tier);
                string configId = SkillBuffTierSpecUtility.BuildUpgradeConfigId(kind, tier);
                string displayName = SkillBuffTierSpecUtility.BuildUpgradeDisplayName(kind, tier);
                string description = SkillBuffTierSpecUtility.BuildDescription(kind, tier);

                UpgradeOptionSO option = CreateOrLoadOption(
                    fileName,
                    configId,
                    displayName,
                    description,
                    UpgradeEffectType.SkillBuff,
                    skillBuffKind: kind,
                    skillBuffTier: tier,
                    maxStacks: 1);
                allOptions.Add(option);
            }
        }

        UpgradeOptionSO unlockLightning = CreateOrLoadOption(
            "UpgradeOption_UnlockLightning",
            GameConstants.ConfigIds.UpgradeUnlockLightning,
            "Unlock Lightning",
            "解锁闪电技能",
            UpgradeEffectType.SkillUnlock,
            skillConfigId: GameConstants.ConfigIds.SkillLightning,
            maxStacks: 1);
        allOptions.Add(unlockLightning);

        UpgradeOptionSO unlockThunder = CreateOrLoadOption(
            "UpgradeOption_UnlockThunder",
            GameConstants.ConfigIds.UpgradeUnlockThunder,
            "Unlock Thunder",
            "解锁落雷技能",
            UpgradeEffectType.SkillUnlock,
            skillConfigId: GameConstants.ConfigIds.SkillThunder,
            maxStacks: 1);
        allOptions.Add(unlockThunder);

        UpgradeOptionSO unlockHeal = CreateOrLoadOption(
            "UpgradeOption_UnlockHeal",
            GameConstants.ConfigIds.UpgradeUnlockHeal,
            "Unlock Heal",
            "解锁恢复技能",
            UpgradeEffectType.SkillUnlock,
            skillConfigId: GameConstants.ConfigIds.SkillHeal,
            maxStacks: 1);
        allOptions.Add(unlockHeal);

        UpgradeOptionSO unlockIce = CreateOrLoadOption(
            "UpgradeOption_UnlockIce",
            GameConstants.ConfigIds.UpgradeUnlockIce,
            "Unlock Ice",
            "解锁冰霜技能",
            UpgradeEffectType.SkillUnlock,
            skillConfigId: GameConstants.ConfigIds.SkillIce,
            maxStacks: 1);
        allOptions.Add(unlockIce);

        UpgradeOptionSO unlockFireRain = CreateOrLoadOption(
            "UpgradeOption_UnlockFireRain",
            GameConstants.ConfigIds.UpgradeUnlockFireRain,
            "Unlock Fire Rain",
            "解锁火雨技能",
            UpgradeEffectType.SkillUnlock,
            skillConfigId: GameConstants.ConfigIds.SkillFireRain,
            maxStacks: 1);
        allOptions.Add(unlockFireRain);

        UpgradeOptionSO unlockWaterWave = CreateOrLoadOption(
            "UpgradeOption_UnlockWaterWave",
            GameConstants.ConfigIds.UpgradeUnlockWaterWave,
            "Unlock Water Wave",
            "解锁水浪技能",
            UpgradeEffectType.SkillUnlock,
            skillConfigId: GameConstants.ConfigIds.SkillWaterWave,
            maxStacks: 1);
        allOptions.Add(unlockWaterWave);

        RewardPoolSO pool = CreateOrLoadPool(
            "RewardPool_Default",
            GameConstants.ConfigIds.RewardPoolDefault,
            allOptions.ToArray());

        var databaseItems = new List<UnityEngine.Object>(allOptions.Count + 1);
        databaseItems.AddRange(allOptions);
        databaseItems.Add(pool);
        RegisterInDatabase(databaseItems.ToArray());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UpgradeConfigBootstrap] 已生成/更新 {allOptions.Count} 个升级选项与 RewardPool_Default。");
    }

    private static UpgradeOptionSO CreateOrLoadOption(
        string fileName,
        string configId,
        string displayName,
        string description,
        UpgradeEffectType effectType,
        string buffConfigId = null,
        string skillConfigId = null,
        SkillBuffKind skillBuffKind = SkillBuffKind.None,
        int skillBuffTier = 1,
        int maxStacks = 99,
        long resourceAmount = 0)
    {
        string path = $"{UpgradeFolder}/{fileName}.asset";
        UpgradeOptionSO asset = AssetDatabase.LoadAssetAtPath<UpgradeOptionSO>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<UpgradeOptionSO>();
            AssetDatabase.CreateAsset(asset, path);
        }

        SerializedObject so = new SerializedObject(asset);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("description").stringValue = description;
        so.FindProperty("effectType").enumValueIndex = (int)effectType;
        so.FindProperty("maxStacks").intValue = maxStacks;
        so.FindProperty("buffConfigId").stringValue = buffConfigId ?? string.Empty;
        so.FindProperty("skillConfigId").stringValue = skillConfigId ?? string.Empty;
        so.FindProperty("skillBuffKind").enumValueIndex = (int)skillBuffKind;
        so.FindProperty("skillBuffTier").intValue = skillBuffTier;
        so.FindProperty("resourceAmount").longValue = resourceAmount;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static RewardPoolSO CreateOrLoadPool(
        string fileName,
        string configId,
        params UpgradeOptionSO[] options)
    {
        string path = $"{UpgradeFolder}/{fileName}.asset";
        RewardPoolSO pool = AssetDatabase.LoadAssetAtPath<RewardPoolSO>(path);
        if (pool == null)
        {
            pool = ScriptableObject.CreateInstance<RewardPoolSO>();
            AssetDatabase.CreateAsset(pool, path);
        }

        SerializedObject so = new SerializedObject(pool);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = "Default Reward Pool";
        SerializedProperty entries = so.FindProperty("entries");
        entries.arraySize = options.Length;
        for (int i = 0; i < options.Length; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("option").objectReferenceValue = options[i];
            entry.FindPropertyRelative("weightOverride").intValue = -1;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pool);
        return pool;
    }

    private static void RegisterInDatabase(params UnityEngine.Object[] items)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(
            "Assets/Resources/Config/ConfigDatabase.asset");
        if (database == null)
        {
            Debug.LogWarning("[UpgradeConfigBootstrap] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        for (int i = 0; i < items.Length; i++)
        {
            UnityEngine.Object item = items[i];
            if (item is UpgradeOptionSO option)
            {
                AddUnique(so.FindProperty("upgradeOptions"), option);
            }
            else if (item is RewardPoolSO pool)
            {
                AddUnique(so.FindProperty("rewardPools"), pool);
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void AddUnique(SerializedProperty list, UnityEngine.Object item)
    {
        if (list == null || item == null)
        {
            return;
        }

        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == item)
            {
                return;
            }
        }

        int index = list.arraySize;
        list.InsertArrayElementAtIndex(index);
        list.GetArrayElementAtIndex(index).objectReferenceValue = item;
    }
}
#endif
