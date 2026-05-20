using UnityEngine;

/// <summary>
/// 所有可以被攻击的物体可以实现该接口。
/// </summary>
public interface IDamagable
{
    /// <summary>兼容入口，内部转换为 <see cref="DamageInfo"/> 并走 <see cref="DamagePipeline"/>。</summary>
    void TakeDamage(float damage);

    /// <summary>统一伤害入口，由 <see cref="DamageSystem"/> 结算并返回结果。</summary>
    DamageResult TakeDamage(DamageInfo info);
}
