using System;

/// <summary>
/// 伤害结算结果：最终伤害、暴击、击杀与触发标签。
/// </summary>
/// <remarks>纯数据结构，无需挂载。</remarks>
public readonly struct DamageResult
{
    /// <summary>最终生效伤害。</summary>
    public float FinalDamage { get; }
    /// <summary>被护甲等方式减免的伤害量。</summary>
    public float MitigatedAmount { get; }
    /// <summary>是否触发暴击。</summary>
    public bool IsCritical { get; }
    /// <summary>是否击杀目标。</summary>
    public bool IsKill { get; }
    /// <summary>本次结算触发的伤害标签。</summary>
    public DamageTag TriggeredTags { get; }

    /// <summary>
    /// 构造伤害结算结果。
    /// </summary>
    /// <param name="finalDamage">最终伤害。</param>
    /// <param name="mitigatedAmount">减免伤害量。</param>
    /// <param name="isCritical">是否暴击。</param>
    /// <param name="isKill">是否击杀。</param>
    /// <param name="triggeredTags">触发的标签。</param>
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

    /// <summary>零伤害的空结果。</summary>
    public static DamageResult None => new DamageResult(0f, 0f, false, false);

    /// <summary>
    /// 返回更新击杀标记后的新结果。
    /// </summary>
    /// <param name="isKill">是否击杀。</param>
    /// <returns>更新后的副本。</returns>
    public DamageResult WithKill(bool isKill)
    {
        return new DamageResult(FinalDamage, MitigatedAmount, IsCritical, isKill, TriggeredTags);
    }
}
