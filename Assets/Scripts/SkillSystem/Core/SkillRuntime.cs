using UnityEngine;

/// <summary>
/// 单技能运行时：配置引用、冷却、等级与 Buff 聚合；由 <see cref="SkillManager"/> 持有并驱动自动施法。
/// </summary>
public sealed class SkillRuntime
{
    private readonly SkillBuffProfile buffProfile = new SkillBuffProfile();
    private readonly SkillRuntimeData baseData = new SkillRuntimeData();

    /// <summary>技能静态配置（<see cref="SkillDataSO"/>）。</summary>
    public SkillDataSO Config { get; private set; }
    /// <summary>本技能 Buff 数值聚合表。</summary>
    public SkillBuffProfile BuffProfile => buffProfile;
    /// <summary>等级表解析后的基础运行时数据。</summary>
    public SkillRuntimeData BaseData => baseData;
    /// <summary>是否已解锁。</summary>
    public bool IsUnlocked { get; private set; }
    /// <summary>基础冷却秒数（未含 Buff 与攻速除数）。</summary>
    public float CooldownSeconds { get; private set; }
    /// <summary>上次施法时间（<see cref="Time.time"/>）。</summary>
    public float LastCastTime { get; set; }
    /// <summary>累计施法次数。</summary>
    public int CastCount { get; private set; }

    private float cooldownDivisor = 1f;

    /// <summary>冷却是否就绪（含 Buff 与攻速除数）。</summary>
    public bool IsCooldownReady =>
        Time.time >= LastCastTime + GetEffectiveCooldown();

    /// <summary>
    /// 设置冷却除数（通常来自攻击速度倍率）。
    /// </summary>
    /// <param name="divisor">除数，下限 0.1。</param>
    public void SetCooldownDivisor(float divisor) =>
        cooldownDivisor = Mathf.Max(0.1f, divisor);

    /// <summary>
    /// 初始化或重置运行时状态。
    /// </summary>
    /// <param name="config">技能配置。</param>
    /// <param name="level">初始等级。</param>
    /// <param name="unlocked">是否视为已解锁。</param>
    public void Initialize(SkillDataSO config, int level = 1, bool unlocked = true)
    {
        Config = config;
        IsUnlocked = unlocked && config != null;
        buffProfile.Reset();
        if (config == null)
        {
            return;
        }

        baseData.Initialize(config, level);
        CooldownSeconds = config.BaseCooldown;
        if (baseData.Cooldown > 0f)
        {
            CooldownSeconds = baseData.Cooldown;
        }

        LastCastTime = Time.time - CooldownSeconds;
    }

    /// <summary>
    /// 设置技能等级并刷新冷却基础值。
    /// </summary>
    /// <param name="level">目标等级。</param>
    public void SetLevel(int level)
    {
        if (Config == null)
        {
            return;
        }

        baseData.SetLevel(Config, level);
        CooldownSeconds = Config.BaseCooldown;
        if (baseData.Cooldown > 0f)
        {
            CooldownSeconds = baseData.Cooldown;
        }
    }

    /// <summary>将技能标记为已解锁（需已有有效配置）。</summary>
    public void Unlock()
    {
        IsUnlocked = Config != null;
    }

    /// <summary>将技能标记为锁定（保留配置与等级数据）。</summary>
    public void Lock() => IsUnlocked = false;

    /// <summary>记录一次施法并启动冷却。</summary>
    public void StartCooldown()
    {
        LastCastTime = Time.time;
        CastCount++;
    }

    /// <summary>
    /// 计算有效冷却秒数（Buff 缩减 ÷ 攻速除数）。
    /// </summary>
    /// <returns>有效冷却秒数。</returns>
    public float GetEffectiveCooldown() =>
        buffProfile.GetEffectiveCooldown(CooldownSeconds) / cooldownDivisor;

    /// <summary>
    /// 获取伤害倍率（含 Buff）。
    /// </summary>
    /// <returns>伤害倍率，下限 0.1。</returns>
    public float GetDamageMultiplier() => Mathf.Max(0.1f, buffProfile.DamageMultiplier);

    /// <summary>清空 Buff 聚合表（升级/重算前调用）。</summary>
    public void RebuildBuffProfile()
    {
        buffProfile.Reset();
    }
}
