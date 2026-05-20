public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (isAnimFinished)
        {
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        if (!enemy.IsWallDetected())
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }

    public override void OnAnimAttackTrigger()
    {
        Controller?.ExecuteWallAttack();
    }
}
