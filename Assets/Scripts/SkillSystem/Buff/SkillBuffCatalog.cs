using UnityEngine;

/// <summary>
/// 技能 Buff 数值表：将 <see cref="SkillBuffKind"/> + tier 映射到 <see cref="SkillBuffProfile"/> 字段。
/// 流水线位置：Buff 应用阶段写入 Profile → <see cref="ISkillEffect"/> 施法时读取。
/// </summary>
public static class SkillBuffCatalog
{
    private static readonly float[] PercentTiers = { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f };
    private static readonly float[] RadiusPercentTiers = { 0.3f, 0.5f, 0.8f, 1f, 1.3f };
    private static readonly int[] CountTiers2345 = { 2, 3, 4, 5 };
    private static readonly int[] PierceTiers = { 1, 2, 3 };

    /// <summary>
    /// 获取 Buff 种类对应的目标技能类型；全局 Buff 返回 <see cref="SkillType.None"/>。
    /// </summary>
    /// <param name="kind">Buff 种类。</param>
    /// <returns>目标技能类型。</returns>
    /// <summary>
    /// 解析 Buff 种类对应的目标技能；全局 Kind 返回 <see cref="SkillType.None"/>。
    /// </summary>
    /// <param name="kind">Buff 种类。</param>
    /// <returns>应写入 Profile 的技能类型。</returns>
    public static SkillType GetTargetSkill(SkillBuffKind kind)
    {
        switch (kind)
        {
            case SkillBuffKind.ShootTrajectoryLines:
            case SkillBuffKind.ShootVolleyCount:
            case SkillBuffKind.ShootPierce:
            case SkillBuffKind.ShootBounce:
            case SkillBuffKind.ShootSplitOnHit:
                return SkillType.Shoot;
            case SkillBuffKind.LightningBoltCount:
            case SkillBuffKind.LightningChainTargets:
            case SkillBuffKind.LightningEndExplosion:
            case SkillBuffKind.LightningStun:
                return SkillType.Lightning;
            case SkillBuffKind.ThunderStrikeCount:
            case SkillBuffKind.ThunderRadius:
            case SkillBuffKind.ThunderStunAll:
            case SkillBuffKind.ThunderStunDuration:
            case SkillBuffKind.ThunderPersistentZone:
                return SkillType.Thunder;
            case SkillBuffKind.FireRainRadius:
            case SkillBuffKind.FireRainDuration:
            case SkillBuffKind.FireRainChainOnKill:
                return SkillType.FireRain;
            case SkillBuffKind.WaterWaveCount:
            case SkillBuffKind.WaterSlowStrength:
            case SkillBuffKind.WaterSlowDuration:
            case SkillBuffKind.WaterWaveSize:
                return SkillType.WaterWave;
            case SkillBuffKind.IceTrajectoryLines:
            case SkillBuffKind.IceVolleyCount:
            case SkillBuffKind.IceFreezeDuration:
            case SkillBuffKind.IceProjectileSize:
            case SkillBuffKind.IceHalfRangeExplosion:
            case SkillBuffKind.IceExplosionRadius:
                return SkillType.Ice;
            case SkillBuffKind.HealRegenPerSecond:
            case SkillBuffKind.HealMaxHpPercent:
            case SkillBuffKind.HealPeriodicTenPercent:
            case SkillBuffKind.HealPeakGrowthEvery3Min:
            case SkillBuffKind.HealOneTimeFull:
                return SkillType.Heal;
            case SkillBuffKind.GlobalAttackSpeed:
            case SkillBuffKind.GlobalBaseDamage:
            case SkillBuffKind.GlobalCritChance:
            case SkillBuffKind.GlobalCritDamage:
            case SkillBuffKind.GlobalCooldownReduction:
                return SkillType.None;
            default:
                return SkillType.None;
        }
    }

    /// <summary>
    /// 判断是否为全局 Buff（不绑定单一技能类型）。
    /// </summary>
    /// <param name="kind">Buff 种类。</param>
    /// <returns>是否为全局 Buff。</returns>
    public static bool IsGlobalKind(SkillBuffKind kind) => GetTargetSkill(kind) == SkillType.None && kind != SkillBuffKind.None;

    /// <summary>
    /// 将 Buff 写入 profile；tier 从 1 起。
    /// </summary>
    /// <param name="profile">目标 Buff 聚合表。</param>
    /// <param name="kind">Buff 种类。</param>
    /// <param name="tier">Buff 层级，从 1 起。</param>
    public static void Apply(SkillBuffProfile profile, SkillBuffKind kind, int tier)
    {
        if (profile == null || kind == SkillBuffKind.None)
        {
            return;
        }

        tier = Mathf.Max(1, tier);

        switch (kind)
        {
            case SkillBuffKind.ShootTrajectoryLines:
                profile.TrajectoryLines = GetCountTier(CountTiers2345, tier, 4);
                break;
            case SkillBuffKind.ShootVolleyCount:
                profile.ShotsPerVolley = GetCountTier(new[] { 2, 3 }, tier, 3);
                break;
            case SkillBuffKind.ShootPierce:
                profile.PierceBonus = GetCountTier(PierceTiers, tier, 3);
                break;
            case SkillBuffKind.ShootBounce:
                profile.BounceCount = Mathf.Min(tier, 2);
                break;
            case SkillBuffKind.ShootSplitOnHit:
                profile.SplitOnHitCount = Mathf.Min(tier, 2);
                break;

            case SkillBuffKind.LightningBoltCount:
                profile.LightningBolts = GetCountTier(CountTiers2345, tier, 4);
                break;
            case SkillBuffKind.LightningChainTargets:
                profile.ChainTargets = GetCountTier(new[] { 2, 3, 4, 5 }, tier, 5);
                break;
            case SkillBuffKind.LightningEndExplosion:
                profile.LightningEndExplosion = true;
                profile.LightningExplosionRadius *= 1f + GetPercentTier(RadiusPercentTiers, tier);
                break;
            case SkillBuffKind.LightningStun:
                profile.LightningStun = true;
                break;

            case SkillBuffKind.ThunderStrikeCount:
                profile.ThunderStrikeCount = GetCountTier(CountTiers2345, tier, 5);
                break;
            case SkillBuffKind.ThunderRadius:
                profile.ThunderRadiusScale += GetPercentTier(RadiusPercentTiers, tier);
                break;
            case SkillBuffKind.ThunderStunAll:
                profile.ThunderStunAllInArea = true;
                break;
            case SkillBuffKind.ThunderStunDuration:
                profile.ThunderStunDurationScale += GetPercentTier(new[] { 0.2f, 0.5f }, tier);
                break;
            case SkillBuffKind.ThunderPersistentZone:
                profile.ThunderPersistentZone = true;
                profile.ThunderZoneDurationScale += GetPercentTier(new[] { 0.3f, 0.5f, 1f }, tier);
                break;

            case SkillBuffKind.FireRainRadius:
                profile.FireRainRadiusScale += GetPercentTier(RadiusPercentTiers, tier);
                break;
            case SkillBuffKind.FireRainDuration:
                profile.FireRainDurationScale += GetPercentTier(RadiusPercentTiers, tier);
                break;
            case SkillBuffKind.FireRainChainOnKill:
                profile.FireRainChainOnKill = tier >= 2 ? 3 : 2;
                break;

            case SkillBuffKind.WaterWaveCount:
                profile.WaterWaveCount = tier >= 2 ? 3 : 2;
                break;
            case SkillBuffKind.WaterSlowStrength:
                profile.WaterSlowPercent += GetPercentTier(new[] { 0.3f, 0.5f, 0.8f }, tier);
                profile.WaterSlowPercent = Mathf.Clamp(profile.WaterSlowPercent, 0.1f, 0.9f);
                break;
            case SkillBuffKind.WaterSlowDuration:
                profile.WaterSlowDurationScale += GetPercentTier(RadiusPercentTiers, tier);
                break;
            case SkillBuffKind.WaterWaveSize:
                profile.WaterWaveSizeScale += GetPercentTier(new[] { 0.3f, 0.5f }, tier);
                break;

            case SkillBuffKind.IceTrajectoryLines:
                profile.IceTrajectoryLines = tier >= 2 ? 3 : 2;
                break;
            case SkillBuffKind.IceVolleyCount:
                profile.IceShotsPerCast = tier >= 2 ? 3 : 2;
                break;
            case SkillBuffKind.IceFreezeDuration:
                profile.IceFreezeDurationScale += GetPercentTier(new[] { 0.3f, 0.5f, 1f }, tier);
                break;
            case SkillBuffKind.IceProjectileSize:
                profile.IceProjectileScale += GetPercentTier(new[] { 0.3f, 0.5f, 0.8f }, tier);
                break;
            case SkillBuffKind.IceHalfRangeExplosion:
                profile.IceExplodeAtHalfRange = true;
                profile.IceExplosionFreezeDuration = tier switch
                {
                    1 => 2f,
                    2 => 3f,
                    _ => 4f,
                };
                break;
            case SkillBuffKind.IceExplosionRadius:
                profile.IceExplosionRadiusScale += GetPercentTier(RadiusPercentTiers, tier);
                break;

            case SkillBuffKind.HealRegenPerSecond:
                profile.HealRegenPercentPerSecond = true;
                break;
            case SkillBuffKind.HealMaxHpPercent:
                profile.HealMaxHpPercentBonus += GetPercentTier(RadiusPercentTiers, tier);
                break;
            case SkillBuffKind.HealPeriodicTenPercent:
                profile.HealEveryMinuteTenPercent = true;
                break;
            case SkillBuffKind.HealPeakGrowthEvery3Min:
                profile.HealEvery3MinPeakTenPercent = true;
                break;
            case SkillBuffKind.HealOneTimeFull:
                profile.HealOneTimeFull = true;
                break;

            case SkillBuffKind.GlobalAttackSpeed:
                profile.AttackSpeedBonus += GetStackPercent(tier);
                break;
            case SkillBuffKind.GlobalBaseDamage:
                profile.DamageMultiplier += GetStackPercent(tier);
                break;
            case SkillBuffKind.GlobalCritChance:
                profile.CritChanceBonus += GetStackPercent(tier);
                break;
            case SkillBuffKind.GlobalCritDamage:
                profile.CritDamageBonus += GetStackPercent(tier);
                break;
            case SkillBuffKind.GlobalCooldownReduction:
                profile.CooldownMultiplier *= 1f - GetStackPercent(tier);
                profile.CooldownMultiplier = Mathf.Max(0.2f, profile.CooldownMultiplier);
                break;
        }
    }

    /// <summary>
    /// 获取通用叠加百分比（10%/tier，上限为最高档）。
    /// </summary>
    /// <param name="tier">Buff 层级，从 1 起。</param>
    /// <returns>叠加百分比（0.1~0.5）。</returns>
    /// <summary>
    /// 获取通用百分比 tier 值（10%/20%/…/50%）。
    /// </summary>
    /// <param name="tier">Buff 层级，从 1 起。</param>
    /// <returns>百分比小数。</returns>
    public static float GetStackPercent(int tier)
    {
        tier = Mathf.Max(1, tier);
        if (tier <= PercentTiers.Length)
        {
            return PercentTiers[tier - 1];
        }

        return PercentTiers[PercentTiers.Length - 1];
    }

    /// <summary>按 tier 查表获取百分比加成。</summary>
    /// <param name="table">百分比档位表。</param>
    /// <param name="tier">Buff 层级。</param>
    /// <returns>对应百分比值。</returns>
    /// <summary>从百分比表中按 tier 取值。</summary>
    /// <param name="table">百分比表。</param>
    /// <param name="tier">层级，从 1 起。</param>
    /// <returns>对应百分比小数。</returns>
    private static float GetPercentTier(float[] table, int tier)
    {
        tier = Mathf.Clamp(tier, 1, table.Length);
        return table[tier - 1];
    }

    /// <summary>按 tier 查表获取数量档位并限制上限。</summary>
    /// <param name="table">数量档位表。</param>
    /// <param name="tier">Buff 层级。</param>
    /// <param name="max">数量上限。</param>
    /// <returns>对应数量值。</returns>
    /// <summary>从数量表中按 tier 取值并限制上限。</summary>
    /// <param name="table">数量表。</param>
    /// <param name="tier">层级，从 1 起。</param>
    /// <param name="max">允许的最大值。</param>
    /// <returns>对应数量。</returns>
    private static int GetCountTier(int[] table, int tier, int max)
    {
        tier = Mathf.Clamp(tier, 1, table.Length);
        return Mathf.Min(table[tier - 1], max);
    }
}
