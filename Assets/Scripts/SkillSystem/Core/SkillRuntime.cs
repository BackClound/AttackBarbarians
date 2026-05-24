using UnityEngine;

/// <summary>
/// 单技能运行时：配置引用、冷却、等级与 Buff 聚合。
/// </summary>
public sealed class SkillRuntime
{
    private readonly SkillBuffProfile buffProfile = new SkillBuffProfile();
    private readonly SkillRuntimeData baseData = new SkillRuntimeData();

    public SkillDataSO Config { get; private set; }
    public SkillBuffProfile BuffProfile => buffProfile;
    public SkillRuntimeData BaseData => baseData;
    public bool IsUnlocked { get; private set; }
    public float CooldownSeconds { get; private set; }
    public float LastCastTime { get; set; }
    public int CastCount { get; private set; }

    public bool IsCooldownReady =>
        Time.time >= LastCastTime + buffProfile.GetEffectiveCooldown(CooldownSeconds);

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

    public void Unlock()
    {
        IsUnlocked = Config != null;
    }

    public void StartCooldown()
    {
        LastCastTime = Time.time;
        CastCount++;
    }

    public float GetEffectiveCooldown()
    {
        return buffProfile.GetEffectiveCooldown(CooldownSeconds);
    }

    public float GetDamageMultiplier() => Mathf.Max(0.1f, buffProfile.DamageMultiplier);

    public void RebuildBuffProfile()
    {
        buffProfile.Reset();
    }
}
