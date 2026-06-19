#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成技能解锁表、技能 Buff 与局内解锁升级选项，并写入 ConfigDatabase。
/// </summary>
public static class SkillConfigBootstrapMenu
{
    private const string SkillFolder = "Assets/Resources/Config/Skill";
    private const string BuffFolder = "Assets/Resources/Config/Buff/Skill";
    private const string UpgradeFolder = "Assets/Resources/Config/Upgrade";
    /// <summary>菜单：生成扩展技能解锁表、Buff 与升级选项。</summary>

    [MenuItem("Attack Barbarians/Config/Create More Skills Assets")]
    public static void CreateMoreSkillsAssets()
    {
        Directory.CreateDirectory(SkillFolder);
        Directory.CreateDirectory(BuffFolder);
        Directory.CreateDirectory(UpgradeFolder);

        SkillUnlockTableSO unlockTable = CreateOrLoadUnlockTable();
        BuffDataSO lightningChainBuff = CreateSkillBuff(
            "BuffData_LightningChain",
            "buff.skill.lightning_chain",
            SkillBuffKind.LightningChainTargets,
            3);
        BuffDataSO thunderRadiusBuff = CreateSkillBuff(
            "BuffData_ThunderRadius",
            "buff.skill.thunder_radius",
            SkillBuffKind.ThunderRadius,
            2);

        UpgradeOptionSO unlockThunder = CreateUnlockOption(
            "UpgradeOption_UnlockThunder",
            GameConstants.ConfigIds.UpgradeUnlockThunder,
            "Unlock Thunder",
            GameConstants.ConfigIds.SkillThunder);
        UpgradeOptionSO unlockFireRain = CreateUnlockOption(
            "UpgradeOption_UnlockFireRain",
            GameConstants.ConfigIds.UpgradeUnlockFireRain,
            "Unlock Fire Rain",
            GameConstants.ConfigIds.SkillFireRain);
        UpgradeOptionSO unlockWater = CreateUnlockOption(
            "UpgradeOption_UnlockWaterWave",
            GameConstants.ConfigIds.UpgradeUnlockWaterWave,
            "Unlock Water Wave",
            GameConstants.ConfigIds.SkillWaterWave);
        UpgradeOptionSO unlockIce = CreateUnlockOption(
            "UpgradeOption_UnlockIce",
            GameConstants.ConfigIds.UpgradeUnlockIce,
            "Unlock Ice",
            GameConstants.ConfigIds.SkillIce);
        UpgradeOptionSO unlockHeal = CreateUnlockOption(
            "UpgradeOption_UnlockHeal",
            GameConstants.ConfigIds.UpgradeUnlockHeal,
            "Unlock Heal",
            GameConstants.ConfigIds.SkillHeal);
        UpgradeOptionSO lightningChain = CreateSkillBuffOption(
            "UpgradeOption_LightningChain",
            GameConstants.ConfigIds.UpgradeLightningChain,
            "Lightning Chain",
            SkillBuffKind.LightningChainTargets,
            3);
        UpgradeOptionSO thunderRadius = CreateSkillBuffOption(
            "UpgradeOption_ThunderRadius",
            GameConstants.ConfigIds.UpgradeThunderRadius,
            "Thunder Radius",
            SkillBuffKind.ThunderRadius,
            2);

        RegisterInDatabase(
            unlockTable,
            lightningChainBuff,
            thunderRadiusBuff,
            unlockThunder,
            unlockFireRain,
            unlockWater,
            unlockIce,
            unlockHeal,
            lightningChain,
            thunderRadius);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SkillConfigBootstrap] More Skills 配置已生成。");
    }

    /// <summary>创建或更新 SkillUnlockTable 默认条目。</summary>
    private static SkillUnlockTableSO CreateOrLoadUnlockTable()
    {
        string path = $"{SkillFolder}/SkillUnlockTable_Default.asset";
        SkillUnlockTableSO table = AssetDatabase.LoadAssetAtPath<SkillUnlockTableSO>(path);
        if (table == null)
        {
            table = ScriptableObject.CreateInstance<SkillUnlockTableSO>();
            AssetDatabase.CreateAsset(table, path);
        }

        SerializedObject so = new SerializedObject(table);
        SerializedProperty entries = so.FindProperty("entries");
        entries.arraySize = 7;
        SetUnlockEntry(entries.GetArrayElementAtIndex(0), GameConstants.ConfigIds.SkillShoot, 0, true);
        SetUnlockEntry(entries.GetArrayElementAtIndex(1), GameConstants.ConfigIds.SkillLightning, 600, false);
        SetUnlockEntry(entries.GetArrayElementAtIndex(2), GameConstants.ConfigIds.SkillHeal, 900, false);
        SetUnlockEntry(entries.GetArrayElementAtIndex(3), GameConstants.ConfigIds.SkillThunder, 1800, false);
        SetUnlockEntry(entries.GetArrayElementAtIndex(4), GameConstants.ConfigIds.SkillFireRain, 3600, false);
        SetUnlockEntry(entries.GetArrayElementAtIndex(5), GameConstants.ConfigIds.SkillIce, 5400, false);
        SetUnlockEntry(entries.GetArrayElementAtIndex(6), GameConstants.ConfigIds.SkillWaterWave, 7200, false);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
        return table;
    }

    /// <summary>写入单条技能解锁配置。</summary>
    /// <param name="element">entries 数组元素。</param>
    /// <param name="skillId">技能 ID。</param>
    /// <param name="seconds">所需游玩秒数。</param>
    /// <param name="defaultUnlocked">是否默认解锁。</param>
    private static void SetUnlockEntry(SerializedProperty element, string skillId, long seconds, bool defaultUnlocked)
    {
        element.FindPropertyRelative("skillConfigId").stringValue = skillId;
        element.FindPropertyRelative("requiredPlayTimeSeconds").longValue = seconds;
        element.FindPropertyRelative("unlockedByDefault").boolValue = defaultUnlocked;
    }

    /// <summary>创建或更新技能 Buff 资产。</summary>
    /// <param name="fileName">文件名。</param>
    /// <param name="configId">配置 ID。</param>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    private static BuffDataSO CreateSkillBuff(string fileName, string configId, SkillBuffKind kind, int tier)
    {
        string path = $"{BuffFolder}/{fileName}.asset";
        BuffDataSO buff = AssetDatabase.LoadAssetAtPath<BuffDataSO>(path);
        if (buff == null)
        {
            buff = ScriptableObject.CreateInstance<BuffDataSO>();
            AssetDatabase.CreateAsset(buff, path);
        }

        SerializedObject so = new SerializedObject(buff);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = kind.ToString();
        so.FindProperty("hasSkillBuff").boolValue = true;
        so.FindProperty("skillBuffKind").enumValueIndex = (int)kind;
        so.FindProperty("skillBuffTier").intValue = tier;
        so.FindProperty("isPermanent").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(buff);
        return buff;
    }

    /// <summary>创建技能解锁类 UpgradeOption。</summary>
    /// <param name="fileName">文件名。</param>
    /// <param name="configId">配置 ID。</param>
    /// <param name="title">标题。</param>
    /// <param name="skillId">技能 ID。</param>
    private static UpgradeOptionSO CreateUnlockOption(string fileName, string configId, string title, string skillId)
    {
        return CreateOption(fileName, configId, title, UpgradeEffectType.SkillUnlock, skillId, SkillBuffKind.None, 0);
    }

    /// <summary>创建 SkillBuff 类 UpgradeOption。</summary>
    /// <param name="fileName">文件名。</param>
    /// <param name="configId">配置 ID。</param>
    /// <param name="title">标题。</param>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    private static UpgradeOptionSO CreateSkillBuffOption(
        string fileName,
        string configId,
        string title,
        SkillBuffKind kind,
        int tier)
    {
        return CreateOption(fileName, configId, title, UpgradeEffectType.SkillBuff, string.Empty, kind, tier);
    }

    /// <summary>创建或更新 UpgradeOption 资产。</summary>
    /// <param name="fileName">文件名。</param>
    /// <param name="configId">配置 ID。</param>
    /// <param name="title">标题。</param>
    /// <param name="effectType">效果类型。</param>
    /// <param name="skillId">技能 ID。</param>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    private static UpgradeOptionSO CreateOption(
        string fileName,
        string configId,
        string title,
        UpgradeEffectType effectType,
        string skillId,
        SkillBuffKind kind,
        int tier)
    {
        string path = $"{UpgradeFolder}/{fileName}.asset";
        UpgradeOptionSO option = AssetDatabase.LoadAssetAtPath<UpgradeOptionSO>(path);
        if (option == null)
        {
            option = ScriptableObject.CreateInstance<UpgradeOptionSO>();
            AssetDatabase.CreateAsset(option, path);
        }

        SerializedObject so = new SerializedObject(option);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = title;
        so.FindProperty("description").stringValue = title;
        so.FindProperty("effectType").enumValueIndex = (int)effectType;
        so.FindProperty("skillConfigId").stringValue = skillId ?? string.Empty;
        so.FindProperty("skillBuffKind").enumValueIndex = (int)kind;
        so.FindProperty("skillBuffTier").intValue = tier;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(option);
        return option;
    }

    /// <summary>将解锁表与 Buff/升级选项注册到 ConfigDatabase。</summary>
    /// <param name="unlockTable">解锁表。</param>
    /// <param name="items">Buff 与升级选项。</param>
    private static void RegisterInDatabase(
        SkillUnlockTableSO unlockTable,
        params Object[] items)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(
            "Assets/Resources/Config/ConfigDatabase.asset");
        if (database == null)
        {
            Debug.LogWarning("[SkillConfigBootstrap] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        so.FindProperty("skillUnlockTable").objectReferenceValue = unlockTable;
        for (int i = 0; i < items.Length; i++)
        {
            Object item = items[i];
            if (item is BuffDataSO buff)
            {
                AddUnique(so.FindProperty("buffs"), buff);
            }
            else if (item is UpgradeOptionSO upgrade)
            {
                AddUnique(so.FindProperty("upgradeOptions"), upgrade);
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    /// <summary>向列表去重追加引用。</summary>
    /// <param name="list">列表属性。</param>
    /// <param name="item">待追加对象。</param>
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
