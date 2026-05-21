using UnityEngine;

/// <summary>
/// 单次投射物飞行覆盖参数（穿透、弹射、分裂、体积），由技能 Buff 注入。
/// </summary>
public struct ProjectileRuntimeOverrides
{
    // 穿透次数
    public int PierceBonus;
    // 弹射次数
    public int BounceCount;
    // 分裂次数
    public int SplitOnHitCount;
    // 命中半径缩放
    public float HitRadiusScale;
    // 最大生命周期缩放
    public float MaxLifetimeScale;
    // 最大命中次数
    public int MaxHitCountOverride;
    // 是否触发半范围爆炸
    public bool ExplodeAtHalfLifetime;
    // 爆炸半径
    public float ExplosionRadius;
    // 爆炸冻结时间
    public float ExplosionFreezeDuration;
    // 命中冻结时间
    public float OnHitFreezeDuration;
    // 元素类型
    public ElementType ElementType;
    // 是否覆盖
    public bool HasOverrides;

    // 从射击配置创建覆盖参数
    public static ProjectileRuntimeOverrides FromShootProfile(SkillBuffProfile profile)
    {
        if (profile == null)
        {
            return default;
        }

        return new ProjectileRuntimeOverrides
        {
            PierceBonus = profile.PierceBonus,
            BounceCount = profile.BounceCount,
            SplitOnHitCount = profile.SplitOnHitCount,
            HitRadiusScale = profile.ProjectileScale,
            ElementType = ElementType.Physical,
            HasOverrides = true,
        };
    }

    public static ProjectileRuntimeOverrides FromIceProfile(SkillBuffProfile profile)
    {
        if (profile == null)
        {
            return default;
        }

        return new ProjectileRuntimeOverrides
        {
            PierceBonus = 99,
            HitRadiusScale = profile.IceProjectileScale,
            MaxHitCountOverride = 32,
            ExplodeAtHalfLifetime = profile.IceExplodeAtHalfRange,
            ExplosionRadius = 2.5f * profile.IceExplosionRadiusScale,
            ExplosionFreezeDuration = profile.IceExplosionFreezeDuration,
            OnHitFreezeDuration = profile.GetIceFreezeDuration(),
            ElementType = ElementType.Ice,
            HasOverrides = true,
        };
    }
}
