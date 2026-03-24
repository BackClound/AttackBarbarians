using UnityEngine;

public class PlayerShootState : PlayerState
{
    private SkillShoot skillShoot;
    private bool isStartShooting;
    private float shootSpeedMulti;

    public PlayerShootState(Player player, StateMachine machine, string animName) : base(player, machine, animName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        skillShoot = player.skillManager.sKillShoot;
        isStartShooting = false;
        shootSpeedMulti = skillShoot.shootSpeedAnimMulti;
        anim.SetFloat("ShootSpeedMulti", shootSpeedMulti);
    }

    public override void OnUpdate()
    {
        //TODO 这里应该根据skill的信息控制发射逻辑
        Debug.Log("player shoot state canShoot " + isStartShooting);
        if (skillShoot.CanUseShootSkill())
        {
            if (isStartShooting)
            {
                isStartShooting = false;
                Debug.Log("Start activate the bullet in player shoot state");
                skillShoot.ActivateOneShootAttack();
            }
        }
        else
        {
            stateMachine.ChangeState(player.idleState);
        }
    }


    public override void OnAnimAttackTrigger()
    {
        isStartShooting = true;
    }
}
