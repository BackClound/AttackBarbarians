using UnityEngine;

public class EnemyMoveState : EnemyState
{
    public EnemyMoveState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        rb.linearVelocity = Vector2.down * enemy.moveSpeed;
    }

    public override void OnUpdate()
    {
        if (enemy.isWallDetected())
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
