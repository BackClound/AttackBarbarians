/// <summary>
/// 敌人攻击状态：停止移动并播放攻击动画，攻击帧对城墙结算伤害。
/// </summary>
public class EnemyAttackState : EnemyState
{
    /// <summary>
    /// 创建攻击状态实例。
    /// </summary>
    /// <param name="enemy">所属敌人实体。</param>
    /// <param name="machine">敌人状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public EnemyAttackState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    /// <summary>
    /// 进入攻击状态：停止移动。
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        StopMovement();
    }

    /// <summary>
    /// 动画结束或墙体脱离攻击范围时退回待机。
    /// </summary>
    public override void OnUpdate()
    {
        if (isAnimFinished)
        {
            stateMachine.ChangeState(enemy.idleState);
            return;
        }

        if (!IsWallInAttackRange())
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }

    /// <summary>
    /// 动画攻击帧：经 <see cref="EnemyController.ExecuteWallAttack"/> 对城墙造成伤害。
    /// </summary>
    public override void OnAnimAttackTrigger()
    {
        Controller?.ExecuteWallAttack();
    }
}
