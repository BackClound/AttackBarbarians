using UnityEngine;

/// <summary>
/// 所有具有攻击能力的物体，可以实现该接口
/// </summary>
public interface IAttackable
{
    public void DoDamage(Entity entity, float damage);
}
