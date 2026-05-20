using UnityEngine;

public class PlayerShootState : PlayerState
{
    private AutoAttackController autoAttack;
    private bool attackFramePending;
    private float shootSpeedMulti;

    public PlayerShootState(Player player, StateMachine machine, string animName) : base(player, machine, animName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        autoAttack = Controller != null ? Controller.AutoAttack : null;
        attackFramePending = false;
        ApplyShootSpeedFromSources();
    }

    public override void OnUpdate()
    {
        if (Controller == null || !Controller.CanEnterCombatState())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (attackFramePending)
        {
            attackFramePending = false;
            ExecuteShootAttack();
        }
    }

    public override void OnAnimAttackTrigger()
    {
        attackFramePending = true;
    }

    private void ExecuteShootAttack()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            autoAttack.ExecuteAttack();
            if (!autoAttack.CanAttack)
            {
                stateMachine.ChangeState(player.idleState);
            }

            return;
        }

        player.skillManager?.sKillShoot?.ActivateOneShootAttack();
    }

    private void ApplyShootSpeedFromSources()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            shootSpeedMulti = autoAttack.AnimSpeedMultiplier;
        }
        else if (Controller != null && Controller.RuntimeStats.IsInitialized)
        {
            shootSpeedMulti = Controller.RuntimeStats.Get(StatType.AttackSpeedMulti);
        }
        else if (player.player_Health != null && player.player_Health.entity_Stats != null)
        {
            shootSpeedMulti = player.player_Health.entity_Stats.GetAttackSpeedMultiplier();
        }
        else
        {
            shootSpeedMulti = 1f;
        }

        anim.SetFloat("ShootSpeedMulti", shootSpeedMulti);
    }
}
