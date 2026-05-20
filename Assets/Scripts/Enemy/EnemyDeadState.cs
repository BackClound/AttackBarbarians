using UnityEngine;

public class EnemyDeadState : EnemyState
{
    public EnemyDeadState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        StopMovement();
    }

    public override void OnUpdate()
    {
        if (isAnimFinished)
        {
            enemy.Die();
        }
    }
}
