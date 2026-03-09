using UnityEngine;

/// <summary>
/// 所有可以被攻击的物体可以实现该接口
/// </summary>
public interface IDamagable
{
    public void TakeDamage(float damage);
}
