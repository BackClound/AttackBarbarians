using UnityEngine;

/// <summary>
/// 单技能运行时 Buff 聚合；由 <see cref="SkillBuffApplier"/> 叠加写入。
/// </summary>
public sealed class SkillBuffProfile
{
    // 最小冷却时间
    public const float MinCooldownSeconds = 0.15f;
    // 基础水浪持续时间
    public const float BaseWaterSlowDuration = 2f;
    // 基础冰冻持续时间
    public const float BaseIceFreezeDuration = 0.5f;

    // 伤害倍率
    public float DamageMultiplier = 1f;
    // 冷却缩减倍率
    public float CooldownMultiplier = 1f;
    // 暴击几率倍率
    public float CritChanceBonus;
    // 暴击伤害倍率
    public float CritDamageBonus;
    // 攻击速度倍率
    public float AttackSpeedBonus;

    public int TrajectoryLines = 1;
    // 每次齐射数量
    public int ShotsPerVolley = 1;
    // 穿透次数
    public int PierceBonus;
    // 弹射次数
    public int BounceCount;
    // 命中分裂次数
    public int SplitOnHitCount;
    // 弹道大小缩放
    public float ProjectileScale = 1f;

    public int LightningBolts = 1;
    // 连接目标数量
    public int ChainTargets = 1;
    // 末端爆炸
    public bool LightningEndExplosion;
    // 麻痹
    public bool LightningStun;
    public float LightningStunDuration = 0.35f;
    // 末端爆炸范围
    public float LightningExplosionRadius = 1.5f;

    public int ThunderStrikeCount = 1;
    // 范围缩放
    public float ThunderRadiusScale = 1f;
    // 全体麻痹
    public bool ThunderStunAllInArea;
    public float ThunderStunDuration = 0.5f;
    public float ThunderStunDurationScale = 1f;
    // 持续爆炸圈
    public bool ThunderPersistentZone;
    // 持续爆炸圈时长
    public float ThunderZoneDuration = 1f;
    // 持续爆炸圈时长缩放
    public float ThunderZoneDurationScale = 1f;

    // 范围缩放
    public float FireRainRadiusScale = 1f;
    // 持续时间缩放
    public float FireRainDurationScale = 1f;
    // 击杀连锁附近目标数量
    public int FireRainChainOnKill;

    public int WaterWaveCount = 1;
    // 减速强度
    public float WaterSlowPercent = 0.3f;
    // 减速时长缩放
    public float WaterSlowDurationScale = 1f;
    public float WaterWaveSizeScale = 1f;
    // 波浪大小缩放 

    public int IceTrajectoryLines = 1;
    public int IceShotsPerCast = 1;
    // 冰冻时长缩放
    public float IceFreezeDurationScale = 1f;
    // 弹道大小缩放
    public float IceProjectileScale = 1f;
    // 半范围爆炸
    public bool IceExplodeAtHalfRange;
    // 爆炸冻结时间
    public float IceExplosionFreezeDuration = 2f;
    // 爆炸范围缩放
    public float IceExplosionRadiusScale = 1f;

    // 每秒恢复生命值百分比
    public bool HealRegenPercentPerSecond;
    // 最大生命值百分比
    public float HealMaxHpPercentBonus;
    // 每分钟恢复生命值百分比
    public bool HealEveryMinuteTenPercent;
    // 每 3 分钟恢复生命值峰值百分比
    public bool HealEvery3MinPeakTenPercent;
    // 一次性满血
    public bool HealOneTimeFull;

    public float GetEffectiveCooldown(float baseCooldown) =>
        Mathf.Max(MinCooldownSeconds, baseCooldown * CooldownMultiplier);

    public float GetIceFreezeDuration() => BaseIceFreezeDuration * IceFreezeDurationScale;

    public float GetWaterSlowDuration() => BaseWaterSlowDuration * WaterSlowDurationScale;

    public float GetThunderStunDuration() => ThunderStunDuration * ThunderStunDurationScale;

    public float GetThunderZoneDuration() => ThunderZoneDuration * ThunderZoneDurationScale;

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
