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

    private static IEnumerable<BuffTierSpec> BuildAllBuffTierSpecs()
    {
        var specs = new List<BuffTierSpec>(128);
        foreach (SkillBuffKind kind in Enum.GetValues(typeof(SkillBuffKind)))
        {
            if (kind == SkillBuffKind.None)
            {
                continue;
            }

            int maxTier = GetMaxTier(kind);
            for (int tier = 1; tier <= maxTier; tier++)
            {
                specs.Add(new BuffTierSpec(
                    kind,
                    tier,
                    description: BuildDescription(kind, tier)));
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

    private static int GetMaxTier(SkillBuffKind kind)
    {
        switch (kind)
        {
            case SkillBuffKind.ShootTrajectoryLines:
            case SkillBuffKind.LightningBoltCount:
                return 4;
            case SkillBuffKind.LightningChainTargets:
                return 4;
            case SkillBuffKind.LightningEndExplosion:
            case SkillBuffKind.ThunderRadius:
            case SkillBuffKind.FireRainRadius:
            case SkillBuffKind.FireRainDuration:
            case SkillBuffKind.WaterSlowDuration:
            case SkillBuffKind.IceExplosionRadius:
            case SkillBuffKind.HealMaxHpPercent:
                return 5;
            case SkillBuffKind.ShootPierce:
            case SkillBuffKind.WaterSlowStrength:
            case SkillBuffKind.IceFreezeDuration:
            case SkillBuffKind.IceProjectileSize:
            case SkillBuffKind.IceHalfRangeExplosion:
            case SkillBuffKind.ThunderPersistentZone:
                return 3;
            case SkillBuffKind.ShootVolleyCount:
            case SkillBuffKind.ShootBounce:
            case SkillBuffKind.ShootSplitOnHit:
            case SkillBuffKind.ThunderStunDuration:
            case SkillBuffKind.FireRainChainOnKill:
            case SkillBuffKind.WaterWaveCount:
            case SkillBuffKind.WaterWaveSize:
            case SkillBuffKind.IceTrajectoryLines:
            case SkillBuffKind.IceVolleyCount:
                return 2;
            case SkillBuffKind.GlobalAttackSpeed:
            case SkillBuffKind.GlobalBaseDamage:
            case SkillBuffKind.GlobalCritChance:
            case SkillBuffKind.GlobalCritDamage:
            case SkillBuffKind.GlobalCooldownReduction:
                return 5;
            case SkillBuffKind.ThunderStrikeCount:
                return 4;
            default:
                return 1;
        }
    }

    private static string BuildDescription(SkillBuffKind kind, int tier)
    {
        switch (kind)
        {
            case SkillBuffKind.ShootTrajectoryLines:
                return $"扇形弹道 {GetCountAtTier(new[] { 2, 3, 4, 5 }, tier)} 条";
            case SkillBuffKind.ShootVolleyCount:
                return $"每次齐射 {GetCountAtTier(new[] { 2, 3 }, tier)} 发";
            case SkillBuffKind.ShootPierce:
                return $"穿透 +{GetCountAtTier(new[] { 1, 2, 3 }, tier)}";
            case SkillBuffKind.ShootBounce:
                return $"弹射 {tier} 次";
            case SkillBuffKind.ShootSplitOnHit:
                return $"命中分裂 {tier} 次";
            case SkillBuffKind.LightningBoltCount:
                return $"并行闪电 {GetCountAtTier(new[] { 2, 3, 4, 5 }, tier)} 道";
            case SkillBuffKind.LightningChainTargets:
                return $"链式连接 {GetCountAtTier(new[] { 2, 3, 4, 5 }, tier)} 个目标";
            case SkillBuffKind.LightningEndExplosion:
                return "链末端范围爆炸";
            case SkillBuffKind.LightningStun:
                return "命中附加麻痹";
            case SkillBuffKind.ThunderStrikeCount:
                return $"落雷 {GetCountAtTier(new[] { 2, 3, 4, 5 }, tier)} 次";
            case SkillBuffKind.ThunderRadius:
                return "落雷范围扩大";
            case SkillBuffKind.ThunderStunAll:
                return "范围内全体麻痹";
            case SkillBuffKind.ThunderStunDuration:
                return "麻痹时长提升";
            case SkillBuffKind.ThunderPersistentZone:
                return "生成持续伤害区域";
            case SkillBuffKind.FireRainRadius:
                return "火雨范围扩大";
            case SkillBuffKind.FireRainDuration:
                return "火雨持续时间延长";
            case SkillBuffKind.FireRainChainOnKill:
                return tier >= 2 ? "击杀连锁附近 3 敌" : "击杀连锁附近 2 敌";
            case SkillBuffKind.WaterWaveCount:
                return tier >= 2 ? "水浪 3 道" : "水浪 2 道";
            case SkillBuffKind.WaterSlowStrength:
                return "减速强度提升";
            case SkillBuffKind.WaterSlowDuration:
                return "减速时长延长";
            case SkillBuffKind.WaterWaveSize:
                return "水浪体积扩大";
            case SkillBuffKind.IceTrajectoryLines:
                return tier >= 2 ? "冰霜扇形 3 条" : "冰霜扇形 2 条";
            case SkillBuffKind.IceVolleyCount:
                return tier >= 2 ? "冰霜齐射 3 发" : "冰霜齐射 2 发";
            case SkillBuffKind.IceFreezeDuration:
                return "冰冻时长延长";
            case SkillBuffKind.IceProjectileSize:
                return "冰霜弹道体积扩大";
            case SkillBuffKind.IceHalfRangeExplosion:
                return "半程范围爆炸并冰冻";
            case SkillBuffKind.IceExplosionRadius:
                return "爆炸范围扩大";
            case SkillBuffKind.HealRegenPerSecond:
                return "每秒恢复 1% 最大生命";
            case SkillBuffKind.HealMaxHpPercent:
                return "最大生命百分比提升";
            case SkillBuffKind.HealPeriodicTenPercent:
                return "每分钟恢复 10% 最大生命";
            case SkillBuffKind.HealPeakGrowthEvery3Min:
                return "每 3 分钟峰值 +10% 最大生命";
            case SkillBuffKind.HealOneTimeFull:
                return "一次性满血";
            case SkillBuffKind.GlobalAttackSpeed:
                return $"全局攻速 +{tier * 10}%";
            case SkillBuffKind.GlobalBaseDamage:
                return $"全局伤害 +{tier * 10}%";
            case SkillBuffKind.GlobalCritChance:
                return $"全局暴击率 +{tier * 10}%";
            case SkillBuffKind.GlobalCritDamage:
                return $"全局暴击伤害 +{tier * 10}%";
            case SkillBuffKind.GlobalCooldownReduction:
                return $"全局冷却缩减 {tier * 10}%";
            default:
                return kind.ToString();
        }
    }

    private static int GetCountAtTier(int[] table, int tier)
    {
        tier = Mathf.Clamp(tier, 1, table.Length);
        return table[tier - 1];
    }

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
        string displayName = GetMaxTier(spec.Kind) > 1
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
        so.FindProperty("description").stringValue = spec.Description ?? BuildDescription(spec.Kind, spec.Tier);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(buff);
        return buff;
    }

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

    private static string BuildDefaultFileName(SkillBuffKind kind, int tier)
    {
        return GetMaxTier(kind) > 1
            ? $"BuffData_{kind}_T{tier}"
            : $"BuffData_{kind}";
    }

    private static string BuildDefaultConfigId(SkillBuffKind kind, int tier)
    {
        string snake = ToSnakeCase(kind.ToString());
        return GetMaxTier(kind) > 1
            ? $"buff.skill.{snake}.t{tier}"
            : $"buff.skill.{snake}";
    }

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
