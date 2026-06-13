using UnityEngine;

/// <summary>
/// 可受伤害对象的统一接口，由 <see cref="Entity"/> 及墙体等实现。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否（接口）。实现类按各自 Prefab 挂载。</remarks>
public interface IDamagable
{
    /// <summary>兼容 float 伤害入口，内部转换为 <see cref="DamageInfo"/> 并走 <see cref="DamagePipeline"/>。</summary>
    /// <param name="damage">伤害数值。</param>
    void TakeDamage(float damage);

    /// <summary>统一伤害入口，由 <see cref="DamageSystem"/> 结算并返回结果。</summary>
    /// <param name="info">伤害上下文。</param>
    /// <returns>结算结果。</returns>
    DamageResult TakeDamage(DamageInfo info);
}
