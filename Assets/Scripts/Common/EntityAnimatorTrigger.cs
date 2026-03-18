using UnityEngine;

public class EntityAnimatorTrigger : MonoBehaviour
{
    private Entity entity;
    private EntityCombat entityCombat;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
        entityCombat = GetComponentInParent<EntityCombat>();
    }

    public virtual void OnAnimationFinished()
    {
        entity?.OnAniamtorFinished();
    }

    public virtual void OnAttackTrigger()
    {
        entityCombat.PerformAttack();
    }
}
