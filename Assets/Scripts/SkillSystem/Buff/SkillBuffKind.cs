/// <summary>
/// 技能专属 Buff 种类；tier 由 <see cref="SkillBuffCatalog"/> 解析为具体数值。
/// </summary>
public enum SkillBuffKind
{
    None = 0,

    // —— 射击 ——
    // 弹道数量
    ShootTrajectoryLines,
    // 每次双/三发
    ShootVolleyCount,
    // 穿透次数
    ShootPierce,
    // 弹射次数
    ShootBounce,
    // 命中分裂次数
    ShootSplitOnHit,

    // —— 闪电 ——
    // 闪电数量
    LightningBoltCount,
    // 连接目标数量
    LightningChainTargets,
    // 末端爆炸
    LightningEndExplosion,
    // 麻痹
    LightningStun,

    // —— 落雷 ——
    // 落雷数量
    ThunderStrikeCount,
    // 范围
    ThunderRadius,
    // 全体麻痹
    ThunderStunAll,
    // 麻痹时长
    ThunderStunDuration,
    // 持续爆炸圈
    ThunderPersistentZone,

    // —— 火雨 ——
    // 范围
    FireRainRadius,
    // 持续时间
    FireRainDuration,
    // 击杀连锁附近目标数量
    FireRainChainOnKill,

    // —— 水浪 ——
    // 波浪数量
    WaterWaveCount,
    // 减速强度
    WaterSlowStrength,
    // 减速时长
    WaterSlowDuration,
    // 波浪大小
    WaterWaveSize,

    // —— 冰霜 ——
    // 弹道数量
    IceTrajectoryLines,
    // 每次齐射
    IceVolleyCount,
    // 冰冻时长
    IceFreezeDuration,
    // 弹道大小
    IceProjectileSize,
    // 半范围爆炸
    IceHalfRangeExplosion,
    // 爆炸范围
    IceExplosionRadius,

    // —— 恢复 ——
    // 每秒恢复生命值
    HealRegenPerSecond,
    // 最大生命值百分比
    HealMaxHpPercent,
    // 每分钟恢复生命值百分比
    HealPeriodicTenPercent,
    // 每 3 分钟恢复生命值峰值百分比
    HealPeakGrowthEvery3Min,
    // 一次性满血
    HealOneTimeFull,

    // —— 通用（可叠加）——
    // 攻击速度
    GlobalAttackSpeed,
    // 基础伤害
    GlobalBaseDamage,
    // 暴击几率
    GlobalCritChance,
    // 暴击伤害
    GlobalCritDamage,
    // 冷却缩减
    GlobalCooldownReduction,
}
