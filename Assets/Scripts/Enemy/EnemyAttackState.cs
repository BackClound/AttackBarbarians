using UnityEngine;

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (!enemy.isWallDetected())
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
