using UnityEngine;

public class EnemyMoveState : EnemyState
{
    public EnemyMoveState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        ApplyMoveVelocity(Vector2.down);
    }

    public override void OnUpdate()
    {
        if (IsWallInAttackRange())
        {
            StopMovement();
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
