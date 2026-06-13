using UnityEngine;

/// <summary>
/// 伤害入口：统一经 <see cref="DamageSystem"/> 结算；Bootstrap 未完成时跳过并告警。
/// </summary>
/// <remarks>纯静态类，无需挂载。</remarks>
public static class DamagePipeline
{
    /// <summary>
    /// 将伤害上下文提交至 <see cref="DamageSystem"/> 结算。
    /// </summary>
    /// <param name="info">伤害上下文。</param>
    /// <returns>结算结果；目标为空或未注册系统时返回 None。</returns>
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

    /// <summary>
    /// 以浮点伤害对指定目标结算（可选技能标识）。
    /// </summary>
    /// <param name="damage">伤害数值。</param>
    /// <param name="target">受击目标。</param>
    /// <param name="source">伤害来源，可选。</param>
    /// <param name="skillId">技能标识，可选。</param>
    /// <returns>结算结果。</returns>
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

    /// <summary>
    /// 对实现 <see cref="IDamagable"/> 的目标应用伤害。
    /// </summary>
    /// <param name="damagable">可受伤对象。</param>
    /// <param name="info">伤害上下文。</param>
    /// <returns>结算结果。</returns>
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
