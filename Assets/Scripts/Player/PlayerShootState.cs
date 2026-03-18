using UnityEngine;

public class PlayerShootState : PlayerState
{
    private SkillShoot skillShoot;
    private bool canShoot;
    private bool hasShooted;
    public PlayerShootState(Player player, StateMachine machine, string animName) : base(player, machine, animName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        skillShoot = player.skillManager.sKillShoot;
        canShoot = false;
        hasShooted = false;
    }

    public override void OnUpdate()
    {
        //TODO 这里应该根据skill的信息控制发射逻辑
        Debug.Log("player shoot state canShoot " + canShoot + " hasShooted " + hasShooted);

        if (canShoot && !hasShooted)
        {
            Debug.Log("Start activate the bullet in player shoot state");
            hasShooted = true;
            skillShoot.ActivateAttackEnemy();
        }
    }

    public override void OnAnimAttackTrigger()
    {
        canShoot = true;
    }

    public override void OnAnimFinished()
    {
        canShoot = false;
        hasShooted = false;
    }

}
