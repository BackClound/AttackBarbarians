using UnityEngine;

public class EntityAnimatorTrigger : MonoBehaviour
{
    private Entity entity;
    private PlayerCombatBridge playerCombatBridge;
    private EnemyCombatBridge enemyCombatBridge;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
        playerCombatBridge = GetComponentInParent<PlayerCombatBridge>();
        enemyCombatBridge = GetComponentInParent<EnemyCombatBridge>();
    }

    public virtual void OnAnimationFinished()
    {
        entity?.OnAniamtorFinished();
    }

    public virtual void OnAttackTrigger()
    {
        if (playerCombatBridge != null)
        {
            playerCombatBridge.PerformAttack();
            return;
        }


        if (enemyCombatBridge != null)
        {
            enemyCombatBridge.PerformAttack();
            return;
        }

        if (entity is Player player)
        {
            player.OnAnimatorAttackTrigger();
            return;
        }

        if (entity is Enemy enemy)
        {
            enemy.OnAnimatorAttackTrigger();
        }
    }
}
