using UnityEngine;

/// <summary>
/// 可主动造成伤害的物体接口。
/// </summary>
/// <remarks>由武器、技能效果等实现，无需单独挂载接口本身。</remarks>
public interface IAttackable
{
    /// <summary>对目标实体造成伤害。</summary>
    /// <param name="entity">受击实体。</param>
    /// <param name="damage">伤害数值。</param>
    public void DoDamage(Entity entity, float damage);
}
