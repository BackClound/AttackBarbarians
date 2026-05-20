using UnityEngine;

public class EntityAnimatorTrigger : MonoBehaviour
{
    private Entity entity;
    private PlayerCombatBridge playerCombatBridge;
    private EnemyCombatBridge enemyCombatBridge;
    private PlayerCombat legacyPlayerCombat;
    private EnemyCombatManager legacyEnemyCombat;

    private void Awake()
    {
        entity = GetComponentInParent<Entity>();
        playerCombatBridge = GetComponentInParent<PlayerCombatBridge>();
        legacyPlayerCombat = GetComponentInParent<PlayerCombat>();
        enemyCombatBridge = GetComponentInParent<EnemyCombatBridge>();
        legacyEnemyCombat = GetComponentInParent<EnemyCombatManager>();
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

        if (legacyPlayerCombat != null)
        {
            legacyPlayerCombat.PerformAttack();
            return;
        }

        if (enemyCombatBridge != null)
        {
            enemyCombatBridge.PerformAttack();
            return;
        }

        if (legacyEnemyCombat != null)
        {
            legacyEnemyCombat.PerformAttack();
            return;
        }

        if (entity is Player player)
        {
            player.OnAnimatorAttackTrigger();
            return;
        }

        if (entity is Enemy enemy && enemy.controller != null)
        {
            enemy.controller.ExecuteWallAttack();
        }
    }
}
