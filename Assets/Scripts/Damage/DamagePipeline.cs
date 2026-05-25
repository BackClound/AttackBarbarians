using UnityEngine;

/// <summary>
/// 伤害入口：统一经 <see cref="DamageSystem"/> 结算；Bootstrap 未完成时跳过并告警。
/// </summary>
/// <remarks>纯静态类，无需挂载。</remarks>
public static class DamagePipeline
{
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

        GameDebug.LogWarning(
            "[DamagePipeline] DamageSystem 未注册，跳过伤害。请确认 GameBootstrapper 已执行 Bootstrap。");
        return DamageResult.None;
    }

    public static DamageResult ApplyToTarget(float damage, GameObject target, object source = null, string skillId = null)
    {
        if (target == null)
        {
            return DamageResult.None;
        }

        DamageInfo info = DamageInfo.FromFloat(damage, target, source);
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
}
