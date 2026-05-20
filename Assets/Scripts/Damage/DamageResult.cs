using System;

/// <summary>
/// 伤害结算结果：最终伤害、暴击、击杀与触发标签。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct DamageResult
{
    public float FinalDamage { get; }
    // 减免伤害
    public float MitigatedAmount { get; }
    public bool IsCritical { get; }
    // 是否击杀
    public bool IsKill { get; }
    // 触发标签
    public DamageTag TriggeredTags { get; }

    public DamageResult(
        float finalDamage,
        float mitigatedAmount,
        bool isCritical,
        bool isKill,
        DamageTag triggeredTags = DamageTag.None)
    {
        FinalDamage = Math.Max(0f, finalDamage);
        MitigatedAmount = Math.Max(0f, mitigatedAmount);
        IsCritical = isCritical;
        IsKill = isKill;
        TriggeredTags = triggeredTags;
    }

    public static DamageResult None => new DamageResult(0f, 0f, false, false);

    public DamageResult WithKill(bool isKill)
    {
        return new DamageResult(FinalDamage, MitigatedAmount, IsCritical, isKill, TriggeredTags);
    }
}
