/// <summary>
/// 技能专属 Buff 种类枚举；tier 由 <see cref="SkillBuffCatalog"/> 解析为 <see cref="SkillBuffProfile"/> 具体数值。
/// 全局 Kind 写入玩家属性或全部技能冷却，技能 Kind 写入对应 <see cref="SkillType"/> 的 Profile。
/// </summary>
public enum SkillBuffKind
{
    /// <summary>无 Buff。</summary>
    None = 0,

    // —— 射击 ——
    /// <summary>扇形弹道数量（2/3/4 条）。</summary>
    ShootTrajectoryLines,
    /// <summary>每次齐射数量（2/3 发）。</summary>
    ShootVolleyCount,
    /// <summary>额外穿透次数（+1/+2/+3）。</summary>
    ShootPierce,
    /// <summary>弹射次数（1/2 次）。</summary>
    ShootBounce,
    /// <summary>命中分裂子弹次数（1/2 次）。</summary>
    ShootSplitOnHit,

    // —— 闪电 ——
    /// <summary>并行闪电道数（2~4 道）。</summary>
    LightningBoltCount,
    /// <summary>链式连接目标数（2~5 个）。</summary>
    LightningChainTargets,
    /// <summary>链末端范围爆炸。</summary>
    LightningEndExplosion,
    /// <summary>命中附加麻痹。</summary>
    LightningStun,

    // —— 落雷 ——
    /// <summary>单次落雷次数（2~5 次）。</summary>
    ThunderStrikeCount,
    /// <summary>落雷 AoE 范围缩放。</summary>
    ThunderRadius,
    /// <summary>范围内全体麻痹。</summary>
    ThunderStunAll,
    /// <summary>麻痹时长缩放。</summary>
    ThunderStunDuration,
    /// <summary>生成持续伤害区域。</summary>
    ThunderPersistentZone,

    // —— 火雨 ——
    /// <summary>火雨范围缩放。</summary>
    FireRainRadius,
    /// <summary>火雨持续时间缩放。</summary>
    FireRainDuration,
    /// <summary>击杀连锁附近敌人（2/3 个）。</summary>
    FireRainChainOnKill,

    // —— 水浪 ——
    /// <summary>水浪波次数量（2/3 道）。</summary>
    WaterWaveCount,
    /// <summary>减速强度提升。</summary>
    WaterSlowStrength,
    /// <summary>减速时长缩放。</summary>
    WaterSlowDuration,
    /// <summary>水浪体积缩放。</summary>
    WaterWaveSize,

    // —— 冰霜 ——
    /// <summary>冰霜扇形弹道行数（2/3 条）。</summary>
    IceTrajectoryLines,
    /// <summary>冰霜每次齐射数量（2/3 发）。</summary>
    IceVolleyCount,
    /// <summary>冰冻时长缩放。</summary>
    IceFreezeDuration,
    /// <summary>冰霜弹道体积缩放。</summary>
    IceProjectileSize,
    /// <summary>半程范围爆炸并冰冻。</summary>
    IceHalfRangeExplosion,
    /// <summary>半程爆炸范围缩放。</summary>
    IceExplosionRadius,

    // —— 恢复 ——
    /// <summary>每秒按最大生命 1% 回血。</summary>
    HealRegenPerSecond,
    /// <summary>最大生命百分比提升。</summary>
    HealMaxHpPercent,
    /// <summary>每分钟恢复 10% 最大生命。</summary>
    HealPeriodicTenPercent,
    /// <summary>每 3 分钟提升 10% 最大生命峰值。</summary>
    HealPeakGrowthEvery3Min,
    /// <summary>一次性满血。</summary>
    HealOneTimeFull,

    // —— 通用（可无限叠加）——
    /// <summary>全局攻击速度 +10%/tier。</summary>
    GlobalAttackSpeed,
    /// <summary>全局基础伤害 +10%/tier。</summary>
    GlobalBaseDamage,
    /// <summary>全局暴击几率 +10%/tier。</summary>
    GlobalCritChance,
    /// <summary>全局暴击伤害 +10%/tier。</summary>
    GlobalCritDamage,
    /// <summary>全局冷却缩减 ×(1-10%/tier)。</summary>
    GlobalCooldownReduction,
}
