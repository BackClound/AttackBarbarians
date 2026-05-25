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

    public bool IsInitialized => isInitialized;
    public DamageCalculationSO Rules => calculationRules != null ? calculationRules : GetFallbackRules();

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

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        isInitialized = false;
    }

    /// <summary>计算并应用伤害；目标无效时返回 <see cref="DamageResult.None"/>。</summary>
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

    private static bool ShouldPublishDamageNumber(GameObject target)
    {
        return target != null && target.CompareTag(GameConstants.Tags.Enemy);
    }

    private static bool RollCritical(Entity_Stats attackerStats, DamageCalculationSO rules)
    {
        if (attackerStats == null || attackerStats.offenseStats == null || attackerStats.offenseStats.critChance == null)
        {
            return false;
        }

        float chance = Mathf.Clamp01(attackerStats.offenseStats.critChance.GetValue());
        return chance > Random.value;
    }

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

    private static float GetElementMultiplier(ElementType element)
    {
        return element == ElementType.None ? 1f : 1f;
    }

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

    private static Entity_Stats ResolveDefenderStats(GameObject target)
    {
        Entity_Health health = ResolveHealth(target);
        return health != null ? health.entity_Stats : null;
    }

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

    private static DamageCalculationSO GetFallbackRules()
    {
        if (fallbackRules == null)
        {
            fallbackRules = ScriptableObject.CreateInstance<DamageCalculationSO>();
        }

        return fallbackRules;
    }
}
