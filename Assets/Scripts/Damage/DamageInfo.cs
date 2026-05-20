using UnityEngine;

/// <summary>
/// 单次伤害请求的上下文：来源、目标、基础数值、技能与元素等。
/// </summary>
/// <remarks>纯数据结构，无需挂载。优先使用 <see cref="Create"/> 工厂方法。</remarks>
public readonly struct DamageInfo
{
    public object Source { get; }
    public GameObject Target { get; }
    public float BaseDamage { get; }
    public float SkillMultiplier { get; }
    public string SkillId { get; }
    public ElementType ElementType { get; }
    public DamageType DamageType { get; }
    public DamageTag Tags { get; }
    public bool IsDot { get; }
    public float KnockbackForce { get; }

    public DamageInfo(
        object source,
        GameObject target,
        float baseDamage,
        float skillMultiplier = 1f,
        string skillId = null,
        ElementType elementType = ElementType.None,
        DamageType damageType = DamageType.Normal,
        DamageTag tags = DamageTag.None,
        bool isDot = false,
        float knockbackForce = 0f)
    {
        Source = source;
        Target = target;
        BaseDamage = Mathf.Max(0f, baseDamage);
        SkillMultiplier = Mathf.Max(0f, skillMultiplier);
        SkillId = skillId ?? string.Empty;
        ElementType = elementType;
        DamageType = damageType;
        Tags = tags;
        IsDot = isDot;
        KnockbackForce = knockbackForce;
    }

    public static DamageInfo Create(
        object source,
        GameObject target,
        float baseDamage,
        float skillMultiplier = 1f,
        string skillId = null,
        ElementType elementType = ElementType.None,
        DamageTag tags = DamageTag.None,
        bool isDot = false,
        float knockbackForce = 0f)
    {
        return new DamageInfo(
            source,
            target,
            baseDamage,
            skillMultiplier,
            skillId,
            elementType,
            DamageType.Normal,
            tags,
            isDot,
            knockbackForce);
    }

    /// <summary>兼容旧 <c>TakeDamage(float)</c> 入口。</summary>
    public static DamageInfo FromLegacy(float damage, GameObject target, object source = null)
    {
        return new DamageInfo(
            source,
            target,
            damage,
            1f,
            null,
            ElementType.None,
            DamageType.Normal,
            DamageTag.SkipCalculation);
    }

    public DamageInfo WithTarget(GameObject target)
    {
        return new DamageInfo(
            Source,
            target,
            BaseDamage,
            SkillMultiplier,
            SkillId,
            ElementType,
            DamageType,
            Tags,
            IsDot,
            KnockbackForce);
    }
}
