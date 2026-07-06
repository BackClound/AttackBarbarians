using UnityEngine;

/// <summary>
/// Buff 解锁节点展示文案与图标解析。
/// </summary>
public static class BuffUnlockPresentationResolver
{
    /// <summary>解析节点标题。</summary>
    public static string ResolveNodeTitle(BuffUnlockPathNode node)
    {
        if (node.Kind == SkillBuffKind.None)
        {
            return "未知 Buff";
        }

        if (ServiceLocator.TryGet(out ConfigManager config) &&
            TryFindBuffData(config, node.Kind, node.Tier, out BuffDataSO buff) &&
            !string.IsNullOrWhiteSpace(buff.DisplayName))
        {
            return buff.DisplayName;
        }

        return node.IsGlobal
            ? ResolveGlobalFallbackName(node.Kind, node.Tier)
            : $"{ResolveSkillShortName(node.SkillType)} · T{node.Tier}";
    }

    /// <summary>解析节点副标题。</summary>
    public static string ResolveNodeSubtitle(BuffUnlockPathNode node) =>
        node.IsGlobal ? "通用 Buff" : "专属 Buff";

    /// <summary>解析节点图标。</summary>
    public static Sprite ResolveNodeIcon(BuffUnlockPathNode node)
    {
        if (ServiceLocator.TryGet(out ConfigManager config))
        {
            if (TryFindBuffData(config, node.Kind, node.Tier, out BuffDataSO buff) && buff.Icon != null)
            {
                return buff.Icon;
            }

            string skillId = MetaSkillBuffProgressResolver.ResolveSkillConfigId(node.SkillType);
            if (!string.IsNullOrWhiteSpace(skillId) && config.TryGetSkill(skillId, out SkillDataSO skill))
            {
                return skill.Icon;
            }
        }

        return null;
    }

    /// <summary>解析背包卡显示名。</summary>
    public static string ResolveCardDisplayName(string cardConfigId, SkillType skillType)
    {
        if (cardConfigId == BuffUnlockCardConstants.Global)
        {
            return BuffUnlockCardConstants.GlobalDisplayName;
        }

        if (ServiceLocator.TryGet(out ConfigManager config))
        {
            string skillId = MetaSkillBuffProgressResolver.ResolveSkillConfigId(skillType);
            if (!string.IsNullOrWhiteSpace(skillId) &&
                config.TryGetSkill(skillId, out SkillDataSO skill) &&
                !string.IsNullOrWhiteSpace(skill.DisplayName))
            {
                return $"{skill.DisplayName}解锁卡";
            }
        }

        return "专属 Buff 解锁卡";
    }

    /// <summary>解析背包卡图标。</summary>
    public static Sprite ResolveCardIcon(string cardConfigId, SkillType skillType)
    {
        if (cardConfigId == BuffUnlockCardConstants.Global)
        {
            return null;
        }

        if (!ServiceLocator.TryGet(out ConfigManager config))
        {
            return null;
        }

        string skillId = MetaSkillBuffProgressResolver.ResolveSkillConfigId(skillType);
        return !string.IsNullOrWhiteSpace(skillId) &&
               config.TryGetSkill(skillId, out SkillDataSO skill)
            ? skill.Icon
            : null;
    }

    private static bool TryFindBuffData(ConfigManager config, SkillBuffKind kind, int tier, out BuffDataSO buff)
    {
        buff = null;
        if (config?.Database?.Buffs == null)
        {
            return false;
        }

        var buffs = config.Database.Buffs;
        for (int i = 0; i < buffs.Count; i++)
        {
            BuffDataSO candidate = buffs[i];
            if (candidate != null &&
                candidate.HasSkillBuff &&
                candidate.SkillBuffKind == kind &&
                candidate.SkillBuffTier == tier)
            {
                buff = candidate;
                return true;
            }
        }

        return false;
    }

    private static string ResolveGlobalFallbackName(SkillBuffKind kind, int tier) =>
        kind switch
        {
            SkillBuffKind.GlobalAttackSpeed => $"攻速 +{tier * 10}%",
            SkillBuffKind.GlobalBaseDamage => $"伤害 +{tier * 10}%",
            SkillBuffKind.GlobalCritChance => $"暴击 +{tier * 10}%",
            SkillBuffKind.GlobalCritDamage => $"暴伤 +{tier * 10}%",
            SkillBuffKind.GlobalCooldownReduction => $"冷却 -{tier * 10}%",
            _ => kind.ToString(),
        };

    private static string ResolveSkillShortName(SkillType skillType) =>
        skillType switch
        {
            SkillType.Shoot => "射击",
            SkillType.Lightning => "闪电",
            SkillType.Thunder => "落雷",
            SkillType.FireRain => "火雨",
            SkillType.WaterWave => "水浪",
            SkillType.Ice => "冰霜",
            SkillType.Heal => "恢复",
            _ => skillType.ToString(),
        };
}
