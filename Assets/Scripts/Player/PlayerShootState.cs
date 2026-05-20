using UnityEngine;

public class PlayerShootState : PlayerState
{
    private SkillShoot skillShoot;
    private AutoAttackController autoAttack;
    private bool isStartShooting;
    private float shootSpeedMulti;

    public PlayerShootState(Player player, StateMachine machine, string animName) : base(player, machine, animName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        skillShoot = player.skillManager.sKillShoot;
        autoAttack = player.controller != null ? player.controller.AutoAttack : null;
        isStartShooting = false;
        ApplyShootSpeedFromSources();
        if (skillShoot != null)
        {
            skillShoot.updateAttackSpeedMultiAction += ApplyShootSpeedMulti;
        }
    }

    public override void OnExit()
    {
        if (skillShoot != null)
        {
            skillShoot.updateAttackSpeedMultiAction -= ApplyShootSpeedMulti;
        }

        base.OnExit();
    }

    public override void OnUpdate()
    {
        if (!CanContinueShooting())
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        if (isStartShooting)
        {
            isStartShooting = false;
            ExecuteShootAttack();
        }
    }

    public void ApplyShootSpeedMulti(float multi)
    {
        shootSpeedMulti = multi;
        anim.SetFloat("ShootSpeedMulti", shootSpeedMulti);
    }

    public override void OnAnimAttackTrigger()
    {
        isStartShooting = true;
    }

    private bool CanContinueShooting()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            return autoAttack.CanAttack;
        }

        return skillShoot != null && skillShoot.CanUseShootSkill();
    }

    private void ExecuteShootAttack()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            autoAttack.ExecuteAttack();
            if (!autoAttack.CanAttack)
            {
                player.playerCombatManager?.UpdateAttackStatus(false);
                stateMachine.ChangeState(player.idleState);
            }

            return;
        }

        skillShoot?.ActivateOneShootAttack();
    }

    private void ApplyShootSpeedFromSources()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            shootSpeedMulti = autoAttack.AnimSpeedMultiplier;
        }
        else if (skillShoot != null)
        {
            shootSpeedMulti = skillShoot.shootSpeedAnimMulti;
        }
        else
        {
            shootSpeedMulti = 1f;
        }

        anim.SetFloat("ShootSpeedMulti", shootSpeedMulti);
    }
}
