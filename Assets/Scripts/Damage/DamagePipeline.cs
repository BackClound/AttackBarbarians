using UnityEngine;

/// <summary>
/// 伤害入口：优先走 <see cref="DamageSystem"/>，Bootstrap 未完成时回退到旧 float 伤害。
/// </summary>
/// <remarks>纯静态类，无需挂载。</remarks>
public static class DamagePipeline
{
    // 应用 
    public static DamageResult Apply(DamageInfo info)
    {
        if (info.Target == null)
        {
            return DamageResult.None;
        }

        if (ServiceLocator.TryGet(out DamageSystem damageSystem))
        {
            return damageSystem.ApplyDamage(info);
        }

        return ApplyLegacy(info);
    }

    // 应用到目标
    public static DamageResult ApplyToTarget(float damage, GameObject target, object source = null, string skillId = null)
    {
        if (target == null)
        {
            return DamageResult.None;
        }

        DamageInfo info = DamageInfo.FromLegacy(damage, target, source);
        if (!string.IsNullOrEmpty(skillId))
        {
            info = new DamageInfo(
                source,
                target,
                damage,
                1f,
                skillId,
                ElementType.None,
                DamageType.Normal,
                DamageTag.SkipCalculation);
        }

        return Apply(info);
    }

    // 应用到可伤害的物体
    public static DamageResult ApplyToDamagable(IDamagable damagable, DamageInfo info)
    {
        if (damagable == null)
        {
            return DamageResult.None;
        }

        if (damagable is Component component && info.Target == null)
        {
            info = info.WithTarget(component.gameObject);
        }

        return Apply(info);
    }

    // 应用旧逻辑
    private static DamageResult ApplyLegacy(DamageInfo info)
    {
        Entity_Health health = ResolveHealth(info.Target);
        if (health == null || !health.CanBeDamage())
        {
            return DamageResult.None;
        }

        float amount = info.BaseDamage * info.SkillMultiplier;
        health.ApplyResolvedDamage(new DamageResult(amount, 0f, false, false, info.Tags), info);

        bool isKill = !health.CanBeDamage();
        DamageResult result = new DamageResult(amount, 0f, false, isKill, info.Tags);

        if (info.Target.CompareTag(GameConstants.Tags.Enemy))
        {
            GameEvents.RaiseDamageApplied(
                info.Source,
                new DamageEventArgs(
                    amount,
                    info.Target.transform.position,
                    info.Source,
                    info.Target,
                    false,
                    info.SkillId,
                    info.ElementType));
        }

        return result;
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
}
