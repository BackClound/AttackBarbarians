using UnityEngine;

/// <summary>
/// 统一伤害结算：按配置顺序计算伤害、写入血量并发布 <see cref="GameConstants.EventKeys.DamageApplied"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 根或子物体上。</para>
/// <para><b>获取方式：</b><see cref="ServiceLocator.Get{T}"/> / <see cref="DamagePipeline.Apply"/>。</para>
/// </remarks>
public class DamageSystem : MonoBehaviour, IGameSystem
{
    private static DamageCalculationSO fallbackRules;

    [SerializeField] private DamageCalculationSO calculationRules;

    private bool isInitialized;

    /// <summary>系统是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前使用的伤害计算公式配置。</summary>
    public DamageCalculationSO Rules => calculationRules != null ? calculationRules : GetFallbackRules();

    /// <summary>
    /// 加载伤害计算配置并完成初始化。
    /// </summary>
    public void Initialize()
    {
        if (calculationRules == null)
        {
            calculationRules = Resources.Load<DamageCalculationSO>(GameConstants.ResourcePaths.DamageCalculation);
            if (calculationRules == null)
            {
                Debug.LogWarning(
                    "[DamageSystem] 未找到 DamageCalculation 配置，使用运行时默认公式。路径: " +
                    GameConstants.ResourcePaths.DamageCalculation);
            }
        }

        isInitialized = true;
    }

    /// <summary>每帧更新（当前无逻辑）。</summary>
    /// <param name="deltaTime">帧间隔时间。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭系统并重置初始化标记。
    /// </summary>
    public void Shutdown()
    {
        isInitialized = false;
    }

    /// <summary>计算并应用伤害；目标无效时返回 <see cref="DamageResult.None"/>。</summary>
    /// <param name="info">伤害上下文。</param>
    /// <returns>伤害结算结果。</returns>
    public DamageResult ApplyDamage(DamageInfo info)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        if (info.Target == null)
        {
            Debug.LogWarning("[DamageSystem] ApplyDamage 跳过：Target 为空。");
            return DamageResult.None;
        }

        Entity_Health health = ResolveHealth(info.Target);
        if (health == null || !health.CanBeDamage())
        {
            return DamageResult.None;
        }

        DamageResult result = Calculate(info, health);
        if (result.FinalDamage <= 0f)
        {
            return result;
        }

        health.ApplyResolvedDamage(result, info);
        bool isKill = !health.CanBeDamage();
        if (isKill)
        {
            result = result.WithKill(true);
        }

        if (ShouldPublishDamageNumber(info.Target))
        {
            GameEvents.RaiseDamageApplied(
                info.Source,
                new DamageEventArgs(
                    result.FinalDamage,
                    info.Target.transform.position,
                    info.Source,
                    info.Target,
                    result.IsCritical,
                    info.SkillId,
                    info.ElementType));
        }

        return result;
    }

    /// <summary>
    /// 计算顺序：基础伤害 → 技能倍率 → 攻击方增益 → 防御减免 → 元素修正 → 暴击 → 最终伤害。
    /// </summary>
    /// <param name="info">伤害上下文。</param>
    /// <param name="targetHealth">目标血量组件，可选；用于读取防御属性。</param>
    /// <returns>计算后的伤害结果（尚未写入血量）。</returns>
    public DamageResult Calculate(DamageInfo info, Entity_Health targetHealth = null)
    {
        DamageCalculationSO rules = Rules;

        if ((info.Tags & DamageTag.SkipCalculation) != 0)
        {
            float rawDamage = info.BaseDamage * info.SkillMultiplier;
            return new DamageResult(rawDamage, 0f, false, false, info.Tags);
        }

        Entity_Stats attackerStats = ResolveAttackerStats(info.Source);
        Entity_Stats defenderStats = targetHealth != null ? targetHealth.entity_Stats : ResolveDefenderStats(info.Target);

        float damage = info.BaseDamage * info.SkillMultiplier;

        if (attackerStats != null)
        {
            damage += GetElementBonus(attackerStats, info.ElementType, rules.ElementStatScale);
        }

        float mitigated = 0f;
        bool bypassArmor = (info.Tags & DamageTag.TrueDamage) != 0 || info.DamageType == DamageType.True;

        if (!bypassArmor && defenderStats != null)
        {
            float armor = defenderStats.GetArmorDefense();
            float armorPen = attackerStats != null && attackerStats.defenseStats != null && attackerStats.defenseStats.armorReduce != null
                ? attackerStats.defenseStats.armorReduce.GetValue()
                : 0f;
            float effectiveArmor = Mathf.Max(0f, armor - armorPen);
            float afterArmor = rules.ApplyArmor(damage, effectiveArmor);
            mitigated = damage - afterArmor;
            damage = afterArmor;
        }

        damage *= GetElementMultiplier(info.ElementType);

        bool isCritical = info.DamageType == DamageType.Critical;
        if (!isCritical && (!info.IsDot || rules.AllowDotCritical))
        {
            isCritical = RollCritical(attackerStats, rules);
        }

        if (isCritical && attackerStats != null && attackerStats.offenseStats != null && attackerStats.offenseStats.critPower != null)
        {
            float critMult = 1f + attackerStats.offenseStats.critPower.GetValue();
            damage *= critMult;
        }
        else if (isCritical)
        {
            damage *= 1f + rules.DefaultCritPower;
        }

        DamageTag triggered = info.Tags;
        if (info.KnockbackForce > 0f)
        {
            triggered |= DamageTag.Knockback;
        }

        return new DamageResult(damage, mitigated, isCritical, false, triggered);
    }

    /// <summary>
    /// 判断是否向 UI 发布跳字事件（当前仅敌人目标）。
    /// </summary>
    /// <param name="target">受击目标。</param>
    /// <returns>是否发布伤害数字事件。</returns>
    private static bool ShouldPublishDamageNumber(GameObject target)
    {
        return target != null && target.CompareTag(GameConstants.Tags.Enemy);
    }

    /// <summary>
    /// 根据攻击方暴击概率掷骰判定是否暴击。
    /// </summary>
    /// <param name="attackerStats">攻击方属性。</param>
    /// <param name="rules">伤害计算规则。</param>
    /// <returns>是否暴击。</returns>
    private static bool RollCritical(Entity_Stats attackerStats, DamageCalculationSO rules)
    {
        if (attackerStats == null || attackerStats.offenseStats == null || attackerStats.offenseStats.critChance == null)
        {
            return false;
        }

        float chance = Mathf.Clamp01(attackerStats.offenseStats.critChance.GetValue());
        return chance > Random.value;
    }

    /// <summary>
    /// 获取元素附加伤害加成。
    /// </summary>
    /// <param name="stats">攻击方属性。</param>
    /// <param name="element">元素类型。</param>
    /// <param name="scale">元素属性换算比例。</param>
    /// <returns>附加伤害量。</returns>
    private static float GetElementBonus(Entity_Stats stats, ElementType element, float scale)
    {
        if (stats == null || stats.offenseStats == null || scale <= 0f)
        {
            return 0f;
        }

        switch (element)
        {
            case ElementType.Fire:
                return stats.offenseStats.fireDamage != null ? stats.offenseStats.fireDamage.GetValue() * scale : 0f;
            case ElementType.Ice:
                return stats.offenseStats.iceDamage != null ? stats.offenseStats.iceDamage.GetValue() * scale : 0f;
            case ElementType.Lightning:
                return stats.offenseStats.lightingDamage != null ? stats.offenseStats.lightingDamage.GetValue() * scale : 0f;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// 获取元素伤害乘数（预留抗性系统扩展）。
    /// </summary>
    /// <param name="element">元素类型。</param>
    /// <returns>伤害乘数。</returns>
    private static float GetElementMultiplier(ElementType element)
    {
        return element == ElementType.None ? 1f : 1f;
    }

    /// <summary>
    /// 从伤害来源解析攻击方属性组件。
    /// </summary>
    /// <param name="source">伤害来源对象。</param>
    /// <returns>攻击方属性，无法解析时返回 null。</returns>
    private static Entity_Stats ResolveAttackerStats(object source)
    {
        if (source == null)
        {
            return null;
        }

        if (source is Component component)
        {
            Entity_Stats onSource = component.GetComponent<Entity_Stats>();
            if (onSource != null)
            {
                return onSource;
            }

            Entity_Health health = component.GetComponent<Entity_Health>();
            if (health != null)
            {
                return health.entity_Stats;
            }

            if (Player.HasInstance && component.gameObject == Player.Instance.gameObject)
            {
                return Player.Instance.player_Health?.entity_Stats;
            }
        }

        if (source is GameObject go)
        {
            return ResolveAttackerStats(go.transform);
        }

        return null;
    }

    /// <summary>
    /// 从目标 GameObject 解析防御方属性。
    /// </summary>
    /// <param name="target">受击目标。</param>
    /// <returns>防御方属性，无法解析时返回 null。</returns>
    private static Entity_Stats ResolveDefenderStats(GameObject target)
    {
        Entity_Health health = ResolveHealth(target);
        return health != null ? health.entity_Stats : null;
    }

    /// <summary>
    /// 从目标 GameObject 解析血量组件（支持 Enemy、Player 与通用 Entity_Health）。
    /// </summary>
    /// <param name="target">受击目标。</param>
    /// <returns>血量组件，无法解析时返回 null。</returns>
    private static Entity_Health ResolveHealth(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        if (target.TryGetComponent(out Enemy enemy))
        {
            return enemy.enemy_Health;
        }

        if (target.TryGetComponent(out Player player))
        {
            return player.player_Health;
        }

        return target.GetComponent<Entity_Health>();
    }

    /// <summary>
    /// 获取运行时默认伤害计算规则（配置缺失时使用）。
    /// </summary>
    /// <returns>默认 <see cref="DamageCalculationSO"/> 实例。</returns>
    private static DamageCalculationSO GetFallbackRules()
    {
        if (fallbackRules == null)
        {
            fallbackRules = ScriptableObject.CreateInstance<DamageCalculationSO>();
        }

        return fallbackRules;
    }
}
