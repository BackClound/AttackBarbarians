#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 按 <c>docs/version_02/skill_buff_system.md</c> 一键生成全部 <see cref="BuffDataSO"/> 并写入 ConfigDatabase。
/// </summary>
public static class BuffConfigBootstrapMenu
{
    private const string BuffRootFolder = "Assets/Resources/Config/Buff";
    private const string SkillBuffFolder = "Assets/Resources/Config/Buff/Skill";
    private const string DatabasePath = "Assets/Resources/Config/ConfigDatabase.asset";

    private readonly struct BuffTierSpec
    {
        public BuffTierSpec(
            SkillBuffKind kind,
            int tier,
            string fileName = null,
            string configId = null,
            string description = null,
            string folderOverride = null)
        {
            Kind = kind;
            Tier = tier;
            FileName = fileName;
            ConfigId = configId;
            Description = description;
            FolderOverride = folderOverride;
        }

        public SkillBuffKind Kind { get; }
        public int Tier { get; }
        public string FileName { get; }
        public string ConfigId { get; }
        public string Description { get; }
        public string FolderOverride { get; }
    }

    /// <summary>菜单：按规范生成全部 BuffData 并写入 ConfigDatabase。</summary>
    [MenuItem("Attack Barbarians/Config/Create All Buff Assets")]
    public static void CreateAllBuffAssets()
    {
        Directory.CreateDirectory(BuffRootFolder);
        Directory.CreateDirectory(SkillBuffFolder);

        var created = new List<BuffDataSO>(128);
        BuffDataSO attackUp = CreateOrLoadStatBuff();
        created.Add(attackUp);

        foreach (BuffTierSpec spec in BuildAllBuffTierSpecs())
        {
            BuffDataSO buff = CreateOrLoadSkillBuff(spec);
            created.Add(buff);
        }

        RegisterInDatabase(created);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BuffConfigBootstrap] 已生成/更新 {created.Count} 个 Buff 资产并写入 ConfigDatabase。");
    }

    /// <summary>枚举全部 SkillBuffKind 构建 tier 规格列表。</summary>
    private static IEnumerable<BuffTierSpec> BuildAllBuffTierSpecs()
    {
        var specs = new List<BuffTierSpec>(128);
        foreach (SkillBuffKind kind in Enum.GetValues(typeof(SkillBuffKind)))
        {
            if (kind == SkillBuffKind.None)
            {
                continue;
            }

            int maxTier = SkillBuffTierSpecUtility.GetMaxTier(kind);
            for (int tier = 1; tier <= maxTier; tier++)
            {
                specs.Add(new BuffTierSpec(
                    kind,
                    tier,
                    description: SkillBuffTierSpecUtility.BuildDescription(kind, tier)));
            }
        }

        ApplyLegacyAssetAliases(specs);
        return specs;
    }

    /// <summary>保留既有资产路径与 configId，避免升级选项与试玩配置断引用。</summary>
    private static void ApplyLegacyAssetAliases(List<BuffTierSpec> specs)
    {
        ReplaceSpec(specs, SkillBuffKind.LightningChainTargets, 3, new BuffTierSpec(
            SkillBuffKind.LightningChainTargets,
            3,
            fileName: "BuffData_LightningChain",
            configId: "buff.skill.lightning_chain",
            description: "链式连接 2~5 个目标",
            folderOverride: SkillBuffFolder));

        ReplaceSpec(specs, SkillBuffKind.ThunderRadius, 2, new BuffTierSpec(
            SkillBuffKind.ThunderRadius,
            2,
            fileName: "BuffData_ThunderRadius",
            configId: "buff.skill.thunder_radius",
            description: "落雷范围 +30%~+130%",
            folderOverride: SkillBuffFolder));
    }

    /// <summary>在规格列表中替换指定 kind/tier 条目。</summary>
    /// <param name="specs">规格列表。</param>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    /// <param name="replacement">替换规格。</param>
    private static void ReplaceSpec(
        List<BuffTierSpec> specs,
        SkillBuffKind kind,
        int tier,
        BuffTierSpec replacement)
    {
        for (int i = 0; i < specs.Count; i++)
        {
            if (specs[i].Kind == kind && specs[i].Tier == tier)
            {
                specs[i] = replacement;
                return;
            }
        }
    }

    /// <summary>创建或更新 AttackUp 属性 Buff 资产。</summary>
    private static BuffDataSO CreateOrLoadStatBuff()
    {
        string path = $"{BuffRootFolder}/BuffData_AttackUp.asset";
        BuffDataSO buff = AssetDatabase.LoadAssetAtPath<BuffDataSO>(path);
        if (buff == null)
        {
            buff = ScriptableObject.CreateInstance<BuffDataSO>();
            AssetDatabase.CreateAsset(buff, path);
        }

        SerializedObject so = new SerializedObject(buff);
        so.FindProperty("configId").stringValue = GameConstants.ConfigIds.BuffAttackUp;
        so.FindProperty("displayName").stringValue = "Attack Up";
        so.FindProperty("duration").floatValue = 5f;
        so.FindProperty("isPermanent").boolValue = false;
        so.FindProperty("maxStacks").intValue = 3;
        so.FindProperty("refreshDurationOnStack").boolValue = true;
        so.FindProperty("hasSkillBuff").boolValue = false;
        so.FindProperty("description").stringValue = "+5 基础伤害";

        SerializedProperty modifiers = so.FindProperty("modifiers");
        modifiers.arraySize = 1;
        SerializedProperty modifier = modifiers.GetArrayElementAtIndex(0);
        modifier.FindPropertyRelative("statType").enumValueIndex = (int)StatType.Damage;
        modifier.FindPropertyRelative("modifierType").enumValueIndex = (int)ConfigModifierType.Flat;
        modifier.FindPropertyRelative("value").floatValue = 5f;
        modifier.FindPropertyRelative("order").intValue = 0;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(buff);
        return buff;
    }

    /// <summary>根据 BuffTierSpec 创建或更新技能 Buff。</summary>
    /// <param name="spec">Buff 层级规格。</param>
    private static BuffDataSO CreateOrLoadSkillBuff(BuffTierSpec spec)
    {
        string subFolder = GetSkillSubFolder(spec.Kind);
        string folder = !string.IsNullOrEmpty(spec.FolderOverride)
            ? spec.FolderOverride
            : string.IsNullOrEmpty(subFolder)
                ? SkillBuffFolder
                : $"{SkillBuffFolder}/{subFolder}";
        Directory.CreateDirectory(folder);

        string fileName = string.IsNullOrEmpty(spec.FileName)
            ? BuildDefaultFileName(spec.Kind, spec.Tier)
            : spec.FileName;
        string configId = string.IsNullOrEmpty(spec.ConfigId)
            ? BuildDefaultConfigId(spec.Kind, spec.Tier)
            : spec.ConfigId;
        string path = $"{folder}/{fileName}.asset";

        BuffDataSO buff = AssetDatabase.LoadAssetAtPath<BuffDataSO>(path);
        if (buff == null)
        {
            buff = ScriptableObject.CreateInstance<BuffDataSO>();
            AssetDatabase.CreateAsset(buff, path);
        }

        SkillType target = SkillBuffCatalog.GetTargetSkill(spec.Kind);
        string displayName = SkillBuffTierSpecUtility.GetMaxTier(spec.Kind) > 1
            ? $"{spec.Kind} T{spec.Tier}"
            : spec.Kind.ToString();

        SerializedObject so = new SerializedObject(buff);
        so.FindProperty("configId").stringValue = configId;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("duration").floatValue = 5f;
        so.FindProperty("isPermanent").boolValue = true;
        so.FindProperty("maxStacks").intValue = 1;
        so.FindProperty("refreshDurationOnStack").boolValue = false;
        so.FindProperty("modifiers").arraySize = 0;
        so.FindProperty("hasSkillBuff").boolValue = true;
        so.FindProperty("skillBuffKind").enumValueIndex = (int)spec.Kind;
        so.FindProperty("skillBuffTarget").enumValueIndex = (int)target;
        so.FindProperty("skillBuffTier").intValue = spec.Tier;
        so.FindProperty("description").stringValue = spec.Description ?? SkillBuffTierSpecUtility.BuildDescription(spec.Kind, spec.Tier);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(buff);
        return buff;
    }

    /// <summary>按目标技能返回 Buff 子目录名。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    private static string GetSkillSubFolder(SkillBuffKind kind)
    {
        switch (SkillBuffCatalog.GetTargetSkill(kind))
        {
            case SkillType.Shoot: return "Shoot";
            case SkillType.Lightning: return "Lightning";
            case SkillType.Thunder: return "Thunder";
            case SkillType.FireRain: return "FireRain";
            case SkillType.WaterWave: return "WaterWave";
            case SkillType.Ice: return "Ice";
            case SkillType.Heal: return "Heal";
            case SkillType.None:
                return SkillBuffCatalog.IsGlobalKind(kind) ? "Global" : string.Empty;
            default:
                return string.Empty;
        }
    }

    /// <summary>生成 Buff 资产默认文件名。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    private static string BuildDefaultFileName(SkillBuffKind kind, int tier)
    {
        return SkillBuffTierSpecUtility.GetMaxTier(kind) > 1
            ? $"BuffData_{kind}_T{tier}"
            : $"BuffData_{kind}";
    }

    /// <summary>生成 Buff 默认 configId。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    private static string BuildDefaultConfigId(SkillBuffKind kind, int tier)
    {
        string snake = ToSnakeCase(kind.ToString());
        return SkillBuffTierSpecUtility.GetMaxTier(kind) > 1
            ? $"buff.skill.{snake}.t{tier}"
            : $"buff.skill.{snake}";
    }

    /// <summary>将 PascalCase 转为 snake_case。</summary>
    /// <param name="value">源字符串。</param>
    private static string ToSnakeCase(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsUpper(c) && i > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(c));
        }

        return builder.ToString();
    }

    /// <summary>将全部 Buff 去重注册到 ConfigDatabase。</summary>
    /// <param name="buffs">Buff 列表。</param>
    private static void RegisterInDatabase(IReadOnlyList<BuffDataSO> buffs)
    {
        ConfigDatabaseSO database = AssetDatabase.LoadAssetAtPath<ConfigDatabaseSO>(DatabasePath);
        if (database == null)
        {
            Debug.LogWarning("[BuffConfigBootstrap] 未找到 ConfigDatabase.asset。");
            return;
        }

        SerializedObject so = new SerializedObject(database);
        SerializedProperty list = so.FindProperty("buffs");
        for (int i = 0; i < buffs.Count; i++)
        {
            AddUnique(list, buffs[i]);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(database);
    }

    /// <summary>向列表去重追加引用。</summary>
    /// <param name="list">列表属性。</param>
    /// <param name="item">待追加对象。</param>
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
