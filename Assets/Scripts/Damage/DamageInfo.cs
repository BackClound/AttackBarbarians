using UnityEngine;

/// <summary>
/// 单次伤害请求的上下文：来源、目标、基础数值、技能与元素等。
/// </summary>
/// <remarks>纯数据结构，无需挂载。优先使用 <see cref="Create"/> 工厂方法。</remarks>
public readonly struct DamageInfo
{
    /// <summary>伤害来源对象（敌人、技能、投射物等）。</summary>
    public object Source { get; }
    /// <summary>受击目标 GameObject。</summary>
    public GameObject Target { get; }
    /// <summary>基础伤害数值。</summary>
    public float BaseDamage { get; }
    /// <summary>技能伤害倍率。</summary>
    public float SkillMultiplier { get; }
    /// <summary>关联技能标识。</summary>
    public string SkillId { get; }
    /// <summary>元素类型。</summary>
    public ElementType ElementType { get; }
    /// <summary>伤害类别（普通、暴击、持续等）。</summary>
    public DamageType DamageType { get; }
    /// <summary>伤害行为标签（击退、穿透、真实伤害等）。</summary>
    public DamageTag Tags { get; }
    /// <summary>是否为持续伤害（DoT）。</summary>
    public bool IsDot { get; }
    /// <summary>击退力度。</summary>
    public float KnockbackForce { get; }

    /// <summary>
    /// 构造完整的伤害上下文。
    /// </summary>
    /// <param name="source">伤害来源。</param>
    /// <param name="target">受击目标。</param>
    /// <param name="baseDamage">基础伤害。</param>
    /// <param name="skillMultiplier">技能倍率，默认 1。</param>
    /// <param name="skillId">技能标识。</param>
    /// <param name="elementType">元素类型。</param>
    /// <param name="damageType">伤害类别。</param>
    /// <param name="tags">行为标签。</param>
    /// <param name="isDot">是否为 DoT。</param>
    /// <param name="knockbackForce">击退力度。</param>
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

    /// <summary>
    /// 创建普通伤害请求（默认伤害类别为 Normal）。
    /// </summary>
    /// <param name="source">伤害来源。</param>
    /// <param name="target">受击目标。</param>
    /// <param name="baseDamage">基础伤害。</param>
    /// <param name="skillMultiplier">技能倍率，默认 1。</param>
    /// <param name="skillId">技能标识。</param>
    /// <param name="elementType">元素类型。</param>
    /// <param name="tags">行为标签。</param>
    /// <param name="isDot">是否为 DoT。</param>
    /// <param name="knockbackForce">击退力度。</param>
    /// <returns>构造的伤害上下文。</returns>
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

    /// <summary>由 <c>TakeDamage(float)</c> 等简单数值入口构造，默认跳过完整公式链。</summary>
    /// <param name="damage">伤害数值。</param>
    /// <param name="target">受击目标。</param>
    /// <param name="source">伤害来源，可选。</param>
    /// <returns>跳过完整计算的伤害上下文。</returns>
    public static DamageInfo FromFloat(float damage, GameObject target, object source = null)
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

    /// <summary>
    /// 返回替换目标后的新伤害上下文（其余字段不变）。
    /// </summary>
    /// <param name="target">新的受击目标。</param>
    /// <returns>更新目标后的副本。</returns>
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
