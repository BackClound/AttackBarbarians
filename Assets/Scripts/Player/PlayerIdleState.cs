using UnityEngine;

public class PlayerIdleState : PlayerState
{
    private SkillShoot skillShoot;

    public PlayerIdleState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        skillShoot = player.skillManager.sKillShoot;
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        //TODO 检测是否可以进行攻击，是否可以切换到PlayerShootState
        //1. 当enemy不为null并且ShootSkill没有在冷却期时，可以changeState

        if (skillShoot.CanUseShootSkill())
        {
            stateMachine.ChangeState(player.shootState);
        }

    }
}
