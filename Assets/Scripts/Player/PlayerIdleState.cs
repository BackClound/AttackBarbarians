using UnityEngine;

public class PlayerIdleState : PlayerState
{
    private SkillShoot skillShoot;
    private AutoAttackController autoAttack;

    public PlayerIdleState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        skillShoot = player.skillManager.sKillShoot;
        autoAttack = player.controller != null ? player.controller.AutoAttack : null;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (CanEnterShootState())
        {
            stateMachine.ChangeState(player.shootState);
        }
    }

    private bool CanEnterShootState()
    {
        if (autoAttack != null && autoAttack.IsReady)
        {
            return autoAttack.CanAttack;
        }

        return skillShoot != null && skillShoot.CanUseShootSkill();
    }
}
