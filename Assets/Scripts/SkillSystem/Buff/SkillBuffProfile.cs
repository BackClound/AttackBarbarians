using UnityEngine;

/// <summary>
/// 单技能运行时 Buff 数值聚合表；由 <see cref="SkillBuffCatalog"/> / <see cref="SkillBuffApplier"/> 叠加写入，
/// 施法阶段由 <see cref="ISkillEffect"/> 与 <see cref="ShootProjectileCaster"/> 读取。
/// </summary>
public sealed class SkillBuffProfile
{
    /// <summary>技能冷却下限（秒），含全局冷却缩减后不低于此值。</summary>
    public const float MinCooldownSeconds = 0.15f;
    /// <summary>水浪减速基础持续时间（秒）。</summary>
    public const float BaseWaterSlowDuration = 2f;
    /// <summary>冰霜冰冻基础持续时间（秒）。</summary>
    public const float BaseIceFreezeDuration = 0.5f;

    /// <summary>伤害倍率（含 <see cref="SkillBuffKind.GlobalBaseDamage"/> 叠加）。</summary>
    public float DamageMultiplier = 1f;
    /// <summary>冷却缩减乘数（含 <see cref="SkillBuffKind.GlobalCooldownReduction"/> 叠加）。</summary>
    public float CooldownMultiplier = 1f;
    /// <summary>暴击几率加成（百分比小数）。</summary>
    public float CritChanceBonus;
    /// <summary>暴击伤害加成（百分比小数）。</summary>
    public float CritDamageBonus;
    /// <summary>攻击速度加成（百分比小数）。</summary>
    public float AttackSpeedBonus;

    /// <summary>射击弹道行数（扇形展开）。</summary>
    public int TrajectoryLines = 1;
    /// <summary>每次齐射发射数量。</summary>
    public int ShotsPerVolley = 1;
    /// <summary>额外穿透次数。</summary>
    public int PierceBonus;
    /// <summary>弹射次数。</summary>
    public int BounceCount;
    /// <summary>命中后分裂子弹次数。</summary>
    public int SplitOnHitCount;
    /// <summary>射击弹道体积缩放。</summary>
    public float ProjectileScale = 1f;

    /// <summary>并行闪电道数。</summary>
    public int LightningBolts = 1;
    /// <summary>闪电链最大连接目标数（含起点）。</summary>
    public int ChainTargets = 1;
    /// <summary>链末端是否触发范围爆炸。</summary>
    public bool LightningEndExplosion;
    /// <summary>命中是否附加麻痹。</summary>
    public bool LightningStun;
    /// <summary>闪电麻痹持续时间（秒）。</summary>
    public float LightningStunDuration = 0.35f;
    /// <summary>闪电末端爆炸半径。</summary>
    public float LightningExplosionRadius = 1.5f;

    /// <summary>单次施法落雷次数。</summary>
    public int ThunderStrikeCount = 1;
    /// <summary>落雷 AoE 半径缩放。</summary>
    public float ThunderRadiusScale = 1f;
    /// <summary>落雷范围内是否全体麻痹。</summary>
    public bool ThunderStunAllInArea;
    /// <summary>落雷麻痹基础时长（秒）。</summary>
    public float ThunderStunDuration = 0.5f;
    /// <summary>落雷麻痹时长缩放。</summary>
    public float ThunderStunDurationScale = 1f;
    /// <summary>是否生成持续伤害区域。</summary>
    public bool ThunderPersistentZone;
    /// <summary>持续区域基础时长（秒）。</summary>
    public float ThunderZoneDuration = 1f;
    /// <summary>持续区域时长缩放。</summary>
    public float ThunderZoneDurationScale = 1f;

    /// <summary>火雨范围缩放。</summary>
    public float FireRainRadiusScale = 1f;
    /// <summary>火雨持续时间缩放（影响 Tick 次数）。</summary>
    public float FireRainDurationScale = 1f;
    /// <summary>击杀后连锁附近敌人数量。</summary>
    public int FireRainChainOnKill;

    /// <summary>水浪波次数量。</summary>
    public int WaterWaveCount = 1;
    /// <summary>减速强度（0~1，越大减速越明显）。</summary>
    public float WaterSlowPercent = 0.3f;
    /// <summary>减速时长缩放。</summary>
    public float WaterSlowDurationScale = 1f;
    /// <summary>水浪体积缩放。</summary>
    public float WaterWaveSizeScale = 1f;

    /// <summary>冰霜扇形弹道行数。</summary>
    public int IceTrajectoryLines = 1;
    /// <summary>冰霜每次齐射数量。</summary>
    public int IceShotsPerCast = 1;
    /// <summary>冰冻时长缩放。</summary>
    public float IceFreezeDurationScale = 1f;
    /// <summary>冰霜弹道体积缩放。</summary>
    public float IceProjectileScale = 1f;
    /// <summary>是否在半程触发范围爆炸。</summary>
    public bool IceExplodeAtHalfRange;
    /// <summary>半程爆炸冰冻时长（秒）。</summary>
    public float IceExplosionFreezeDuration = 2f;
    /// <summary>半程爆炸范围缩放。</summary>
    public float IceExplosionRadiusScale = 1f;

    /// <summary>是否启用每秒按最大生命 1% 回血。</summary>
    public bool HealRegenPercentPerSecond;
    /// <summary>最大生命百分比加成（累计）。</summary>
    public float HealMaxHpPercentBonus;
    /// <summary>是否每分钟恢复 10% 最大生命。</summary>
    public bool HealEveryMinuteTenPercent;
    /// <summary>是否每 3 分钟提升 10% 最大生命峰值。</summary>
    public bool HealEvery3MinPeakTenPercent;
    /// <summary>是否触发一次性满血（触发后自动清除）。</summary>
    public bool HealOneTimeFull;

    /// <summary>
    /// 计算有效冷却秒数（基础冷却 × 缩减乘数，不低于 <see cref="MinCooldownSeconds"/>）。
    /// </summary>
    /// <param name="baseCooldown">技能基础冷却秒数。</param>
    /// <returns>应用 Buff 后的冷却秒数。</returns>
    public float GetEffectiveCooldown(float baseCooldown) =>
        Mathf.Max(MinCooldownSeconds, baseCooldown * CooldownMultiplier);

    /// <summary>获取冰霜冰冻有效时长。</summary>
    /// <returns>冰冻秒数。</returns>
    public float GetIceFreezeDuration() => BaseIceFreezeDuration * IceFreezeDurationScale;

    /// <summary>获取水浪减速有效时长。</summary>
    /// <returns>减速秒数。</returns>
    public float GetWaterSlowDuration() => BaseWaterSlowDuration * WaterSlowDurationScale;

    /// <summary>获取落雷麻痹有效时长。</summary>
    /// <returns>麻痹秒数。</returns>
    public float GetThunderStunDuration() => ThunderStunDuration * ThunderStunDurationScale;

    /// <summary>获取落雷持续区域有效时长。</summary>
    /// <returns>区域持续秒数。</returns>
    public float GetThunderZoneDuration() => ThunderZoneDuration * ThunderZoneDurationScale;

    /// <summary>重置全部 Buff 字段为默认值（解锁/重算前调用）。</summary>
    public void Reset()
    {
        DamageMultiplier = 1f;
        CooldownMultiplier = 1f;
        CritChanceBonus = 0f;
        CritDamageBonus = 0f;
        AttackSpeedBonus = 0f;
        TrajectoryLines = 1;
        ShotsPerVolley = 1;
        PierceBonus = 0;
        BounceCount = 0;
        SplitOnHitCount = 0;
        ProjectileScale = 1f;
        LightningBolts = 1;
        ChainTargets = 1;
        LightningEndExplosion = false;
        LightningStun = false;
        LightningStunDuration = 0.35f;
        LightningExplosionRadius = 1.5f;
        ThunderStrikeCount = 1;
        ThunderRadiusScale = 1f;
        ThunderStunAllInArea = false;
        ThunderStunDuration = 0.5f;
        ThunderStunDurationScale = 1f;
        ThunderPersistentZone = false;
        ThunderZoneDuration = 1f;
        ThunderZoneDurationScale = 1f;
        FireRainRadiusScale = 1f;
        FireRainDurationScale = 1f;
        FireRainChainOnKill = 0;
        WaterWaveCount = 1;
        WaterSlowPercent = 0.3f;
        WaterSlowDurationScale = 1f;
        WaterWaveSizeScale = 1f;
        IceTrajectoryLines = 1;
        IceShotsPerCast = 1;
        IceFreezeDurationScale = 1f;
        IceProjectileScale = 1f;
        IceExplodeAtHalfRange = false;
        IceExplosionFreezeDuration = 2f;
        IceExplosionRadiusScale = 1f;
        HealRegenPercentPerSecond = false;
        HealMaxHpPercentBonus = 0f;
        HealEveryMinuteTenPercent = false;
        HealEvery3MinPeakTenPercent = false;
        HealOneTimeFull = false;
    }
}
