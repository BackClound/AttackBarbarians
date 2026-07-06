using System.Collections.Generic;

/// <summary>
/// 各技能专属 Buff 解锁顺序（按 SkillBuffKind）。
/// </summary>
public static class BuffUnlockSkillDefinitions
{
    private static readonly SkillBuffKind[] GlobalKinds =
    {
        SkillBuffKind.GlobalAttackSpeed,
        SkillBuffKind.GlobalBaseDamage,
        SkillBuffKind.GlobalCritChance,
        SkillBuffKind.GlobalCritDamage,
        SkillBuffKind.GlobalCooldownReduction,
    };

    private static readonly Dictionary<SkillType, SkillBuffKind[]> ExclusiveKindsBySkill =
        new Dictionary<SkillType, SkillBuffKind[]>
        {
            {
                SkillType.Shoot,
                new[]
                {
                    SkillBuffKind.ShootTrajectoryLines,
                    SkillBuffKind.ShootVolleyCount,
                    SkillBuffKind.ShootPierce,
                    SkillBuffKind.ShootBounce,
                    SkillBuffKind.ShootSplitOnHit,
                }
            },
            {
                SkillType.Lightning,
                new[]
                {
                    SkillBuffKind.LightningBoltCount,
                    SkillBuffKind.LightningChainTargets,
                    SkillBuffKind.LightningEndExplosion,
                    SkillBuffKind.LightningStun,
                }
            },
            {
                SkillType.Thunder,
                new[]
                {
                    SkillBuffKind.ThunderStrikeCount,
                    SkillBuffKind.ThunderRadius,
                    SkillBuffKind.ThunderStunAll,
                    SkillBuffKind.ThunderStunDuration,
                    SkillBuffKind.ThunderPersistentZone,
                }
            },
            {
                SkillType.FireRain,
                new[]
                {
                    SkillBuffKind.FireRainRadius,
                    SkillBuffKind.FireRainDuration,
                    SkillBuffKind.FireRainChainOnKill,
                }
            },
            {
                SkillType.WaterWave,
                new[]
                {
                    SkillBuffKind.WaterWaveCount,
                    SkillBuffKind.WaterSlowStrength,
                    SkillBuffKind.WaterSlowDuration,
                    SkillBuffKind.WaterWaveSize,
                }
            },
            {
                SkillType.Ice,
                new[]
                {
                    SkillBuffKind.IceTrajectoryLines,
                    SkillBuffKind.IceVolleyCount,
                    SkillBuffKind.IceFreezeDuration,
                    SkillBuffKind.IceProjectileSize,
                    SkillBuffKind.IceHalfRangeExplosion,
                    SkillBuffKind.IceExplosionRadius,
                }
            },
            {
                SkillType.Heal,
                new[]
                {
                    SkillBuffKind.HealRegenPerSecond,
                    SkillBuffKind.HealMaxHpPercent,
                    SkillBuffKind.HealPeriodicTenPercent,
                    SkillBuffKind.HealPeakGrowthEvery3Min,
                    SkillBuffKind.HealOneTimeFull,
                }
            },
        };

    /// <summary>科技页展示顺序（已解锁技能优先，射击始终展示）。</summary>
    public static readonly SkillType[] DisplayOrder =
    {
        SkillType.Shoot,
        SkillType.Lightning,
        SkillType.Thunder,
        SkillType.FireRain,
        SkillType.WaterWave,
        SkillType.Ice,
        SkillType.Heal,
    };

    /// <summary>循环使用的全局 Buff 种类列表。</summary>
    public static IReadOnlyList<SkillBuffKind> GlobalKindsList => GlobalKinds;

    /// <summary>获取技能专属 Buff 解锁顺序。</summary>
    public static IReadOnlyList<SkillBuffKind> GetExclusiveKinds(SkillType skillType) =>
        ExclusiveKindsBySkill.TryGetValue(skillType, out SkillBuffKind[] kinds)
            ? kinds
            : System.Array.Empty<SkillBuffKind>();

    /// <summary>路径中第 n 个通用区块的大小：5、7、9…</summary>
    public static int GetGlobalBlockSize(int globalBlockIndex) => 5 + globalBlockIndex * 2;
}
