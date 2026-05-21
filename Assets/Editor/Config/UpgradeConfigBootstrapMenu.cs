#if UNITY_EDITOR
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

        UpgradeOptionSO attackUp = CreateOrLoadOption(
            "UpgradeOption_AttackUp",
            GameConstants.ConfigIds.UpgradeAttackUp,
            "Attack Up",
            "+5 base damage",
            UpgradeEffectType.StatBuff,
            buffConfigId: GameConstants.ConfigIds.BuffAttackUp);

        UpgradeOptionSO shootPierce = CreateOrLoadOption(
            "UpgradeOption_ShootPierce",
            GameConstants.ConfigIds.UpgradeShootPierce,
            "Piercing Arrows",
            "Shooting gains pierce",
            UpgradeEffectType.SkillBuff,
            skillBuffKind: SkillBuffKind.ShootPierce);

        UpgradeOptionSO unlockLightning = CreateOrLoadOption(
            "UpgradeOption_UnlockLightning",
            GameConstants.ConfigIds.UpgradeUnlockLightning,
            "Unlock Lightning",
            "Unlock lightning skill",
            UpgradeEffectType.SkillUnlock,
            skillConfigId: GameConstants.ConfigIds.SkillLightning);

        UpgradeOptionSO goldBonus = CreateOrLoadOption(
            "UpgradeOption_GoldBonus",
            GameConstants.ConfigIds.UpgradeGoldBonus,
            "Gold Bonus",
            "+50 gold",
            UpgradeEffectType.ResourceGold,
            resourceAmount: 50);

        RewardPoolSO pool = CreateOrLoadPool(
            "RewardPool_Default",
            GameConstants.ConfigIds.RewardPoolDefault,
            attackUp,
            shootPierce,
            unlockLightning,
            goldBonus);

        RegisterInDatabase(attackUp, shootPierce, unlockLightning, goldBonus, pool);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[UpgradeConfigBootstrap] 默认升级资产已创建并写入 ConfigDatabase。");
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
        so.FindProperty("buffConfigId").stringValue = buffConfigId ?? string.Empty;
        so.FindProperty("skillConfigId").stringValue = skillConfigId ?? string.Empty;
        so.FindProperty("skillBuffKind").enumValueIndex = (int)skillBuffKind;
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

    private static void RegisterInDatabase(
        UpgradeOptionSO attackUp,
        UpgradeOptionSO shootPierce,
        UpgradeOptionSO unlockLightning,
        UpgradeOptionSO goldBonus,
        RewardPoolSO pool)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(
            "Assets/Resources/Config/ConfigDatabase.asset");
        if (database == null)
        {
            Debug.LogWarning("[UpgradeConfigBootstrap] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        AddUnique(so.FindProperty("upgradeOptions"), attackUp);
        AddUnique(so.FindProperty("upgradeOptions"), shootPierce);
        AddUnique(so.FindProperty("upgradeOptions"), unlockLightning);
        AddUnique(so.FindProperty("upgradeOptions"), goldBonus);
        AddUnique(so.FindProperty("rewardPools"), pool);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    private static void AddUnique(SerializedProperty list, Object item)
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
