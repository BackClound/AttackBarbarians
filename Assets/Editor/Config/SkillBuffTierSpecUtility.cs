#if UNITY_EDITOR
using System.Text;
using UnityEngine;

/// <summary>
/// 多 tier SkillBuff 的层级与描述规则，供 Buff / Upgrade 配置引导共用。
/// </summary>
internal static class SkillBuffTierSpecUtility
{
    /// <summary>返回指定 SkillBuffKind 的最大 tier。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    public static int GetMaxTier(SkillBuffKind kind)
    {
        switch (kind)
        {
            case SkillBuffKind.ShootTrajectoryLines:
            case SkillBuffKind.LightningBoltCount:
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

    /// <summary>判断 SkillBuff 是否存在多 tier。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    public static bool HasMultipleTiers(SkillBuffKind kind) => GetMaxTier(kind) > 1;
    /// <summary>生成指定 kind/tier 的中文效果描述。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>

    public static string BuildDescription(SkillBuffKind kind, int tier)
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
            case SkillBuffKind.HealMaxHpPercent:
                return "最大生命百分比提升";
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

    /// <summary>生成升级选项显示名。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    public static string BuildUpgradeDisplayName(SkillBuffKind kind, int tier)
    {
        string label = GetUpgradeLabel(kind);
        return tier <= 1 ? label : $"{label} T{tier}";
    }
    /// <summary>生成升级选项资产文件名。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>

    public static string BuildUpgradeFileName(SkillBuffKind kind, int tier)
    {
        if (TryGetLegacyUpgradeFileName(kind, tier, out string legacy))
        {
            return legacy;
        }

        return HasMultipleTiers(kind)
            ? $"UpgradeOption_{kind}_T{tier}"
            : $"UpgradeOption_{kind}";
    }

    /// <summary>生成升级选项 configId。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    public static string BuildUpgradeConfigId(SkillBuffKind kind, int tier)
    {
        if (TryGetLegacyUpgradeConfigId(kind, tier, out string legacy))
        {
            return legacy;
        }

        string snake = ToSnakeCase(kind.ToString());
        return tier <= 1 && !HasMultipleTiers(kind)
            ? $"upgrade.{snake}"
            : $"upgrade.{snake}.t{tier}";
    }

    /// <summary>尝试获取遗留升级选项文件名。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    /// <param name="fileName">输出文件名。</param>
    public static bool TryGetLegacyUpgradeFileName(SkillBuffKind kind, int tier, out string fileName)
    {
        switch (kind)
        {
            case SkillBuffKind.ShootPierce when tier == 1:
                fileName = "UpgradeOption_ShootPierce";
                return true;
            case SkillBuffKind.ShootTrajectoryLines when tier == 1:
                fileName = "UpgradeOption_ShootTrajectoryLines";
                return true;
            case SkillBuffKind.GlobalCritChance when tier == 1:
                fileName = "UpgradeOption_GlobalCritChance";
                return true;
            case SkillBuffKind.GlobalAttackSpeed when tier == 1:
                fileName = "UpgradeOption_GlobalAttackSpeed";
                return true;
            case SkillBuffKind.LightningChainTargets when tier == 1:
                fileName = "UpgradeOption_LightningChain";
                return true;
            case SkillBuffKind.ThunderRadius when tier == 1:
                fileName = "UpgradeOption_ThunderRadius";
                return true;
            default:
                fileName = null;
                return false;
        }
    }

    /// <summary>尝试获取遗留升级选项 configId。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    /// <param name="tier">层级。</param>
    /// <param name="configId">输出 configId。</param>
    public static bool TryGetLegacyUpgradeConfigId(SkillBuffKind kind, int tier, out string configId)
    {
        switch (kind)
        {
            case SkillBuffKind.ShootPierce when tier == 1:
                configId = GameConstants.ConfigIds.UpgradeShootPierce;
                return true;
            case SkillBuffKind.ShootTrajectoryLines when tier == 1:
                configId = "upgrade.shoot_trajectory";
                return true;
            case SkillBuffKind.GlobalCritChance when tier == 1:
                configId = "upgrade.global_crit_chance";
                return true;
            case SkillBuffKind.GlobalAttackSpeed when tier == 1:
                configId = "upgrade.global_attack_speed";
                return true;
            case SkillBuffKind.LightningChainTargets when tier == 1:
                configId = GameConstants.ConfigIds.UpgradeLightningChain;
                return true;
            case SkillBuffKind.ThunderRadius when tier == 1:
                configId = GameConstants.ConfigIds.UpgradeThunderRadius;
                return true;
            default:
                configId = null;
                return false;
        }
    }

    /// <summary>返回升级选项英文标签。</summary>
    /// <param name="kind">SkillBuff 种类。</param>
    private static string GetUpgradeLabel(SkillBuffKind kind)
    {
        switch (kind)
        {
            case SkillBuffKind.ShootTrajectoryLines: return "Twin Volley";
            case SkillBuffKind.ShootVolleyCount: return "Volley Count";
            case SkillBuffKind.ShootPierce: return "Piercing Arrows";
            case SkillBuffKind.ShootBounce: return "Arrow Bounce";
            case SkillBuffKind.ShootSplitOnHit: return "Split Shot";
            case SkillBuffKind.LightningBoltCount: return "Lightning Bolts";
            case SkillBuffKind.LightningChainTargets: return "Lightning Chain";
            case SkillBuffKind.LightningEndExplosion: return "Chain Explosion";
            case SkillBuffKind.ThunderStrikeCount: return "Thunder Strikes";
            case SkillBuffKind.ThunderRadius: return "Thunder Radius";
            case SkillBuffKind.ThunderStunDuration: return "Thunder Stun+";
            case SkillBuffKind.ThunderPersistentZone: return "Thunder Zone";
            case SkillBuffKind.FireRainRadius: return "Fire Rain Radius";
            case SkillBuffKind.FireRainDuration: return "Fire Rain Duration";
            case SkillBuffKind.FireRainChainOnKill: return "Fire Rain Chain";
            case SkillBuffKind.WaterWaveCount: return "Water Waves";
            case SkillBuffKind.WaterSlowStrength: return "Water Slow+";
            case SkillBuffKind.WaterSlowDuration: return "Slow Duration";
            case SkillBuffKind.WaterWaveSize: return "Wave Size";
            case SkillBuffKind.IceTrajectoryLines: return "Ice Fan";
            case SkillBuffKind.IceVolleyCount: return "Ice Volley";
            case SkillBuffKind.IceFreezeDuration: return "Freeze Duration";
            case SkillBuffKind.IceProjectileSize: return "Ice Size";
            case SkillBuffKind.IceHalfRangeExplosion: return "Mid-Range Blast";
            case SkillBuffKind.IceExplosionRadius: return "Blast Radius";
            case SkillBuffKind.HealMaxHpPercent: return "Max HP Up";
            case SkillBuffKind.GlobalAttackSpeed: return "Attack Speed Up";
            case SkillBuffKind.GlobalBaseDamage: return "Base Damage Up";
            case SkillBuffKind.GlobalCritChance: return "Crit Chance Up";
            case SkillBuffKind.GlobalCritDamage: return "Crit Damage Up";
            case SkillBuffKind.GlobalCooldownReduction: return "Cooldown Reduction";
            default:
                return kind.ToString();
        }
    }

    /// <summary>按 tier 查表返回数值。</summary>
    /// <param name="table">数值表。</param>
    /// <param name="tier">层级。</param>
    private static int GetCountAtTier(int[] table, int tier)
    {
        tier = Mathf.Clamp(tier, 1, table.Length);
        return table[tier - 1];
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
}
#endif
