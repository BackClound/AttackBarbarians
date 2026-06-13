using UnityEngine;

/// <summary>
/// 单次投射物飞行覆盖参数（穿透、弹射、分裂、体积），由技能 Buff 注入。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。由技能系统在发射时传入。</remarks>
public struct ProjectileRuntimeOverrides
{
    /// <summary>额外穿透次数。</summary>
    public int PierceBonus;
    /// <summary>弹射次数。</summary>
    public int BounceCount;
    /// <summary>命中时分裂子弹数量。</summary>
    public int SplitOnHitCount;
    /// <summary>命中半径缩放倍率。</summary>
    public float HitRadiusScale;
    /// <summary>最大生命周期缩放倍率。</summary>
    public float MaxLifetimeScale;
    /// <summary>最大命中次数覆盖值。</summary>
    public int MaxHitCountOverride;
    /// <summary>是否在生命周期过半时触发范围爆炸。</summary>
    public bool ExplodeAtHalfLifetime;
    /// <summary>范围爆炸半径。</summary>
    public float ExplosionRadius;
    /// <summary>爆炸附带的冰冻持续时间（秒）。</summary>
    public float ExplosionFreezeDuration;
    /// <summary>命中附带的冰冻持续时间（秒）。</summary>
    public float OnHitFreezeDuration;
    /// <summary>覆盖元素类型。</summary>
    public ElementType ElementType;
    /// <summary>是否包含有效覆盖参数。</summary>
    public bool HasOverrides;

    /// <summary>
    /// 从射击技能 Buff 配置创建覆盖参数。
    /// </summary>
    /// <param name="profile">射击 Buff 配置。</param>
    /// <returns>飞行覆盖参数。</returns>
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

    /// <summary>
    /// 从冰系技能 Buff 配置创建覆盖参数（穿透、半程爆炸、冰冻等）。
    /// </summary>
    /// <param name="profile">冰系 Buff 配置。</param>
    /// <returns>飞行覆盖参数。</returns>
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
