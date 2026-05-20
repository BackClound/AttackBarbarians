using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        cooldownThreshold = enemy.cooldownThreshold;
        cooldownTimer = cooldownThreshold;
        StopMovement();
    }

    public override void OnUpdate()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f)
        {
            return;
        }

        if (IsWallInAttackRange())
        {
            stateMachine.ChangeState(enemy.attackState);
        }
        else
        {
            stateMachine.ChangeState(enemy.moveState);
        }
    }
}
