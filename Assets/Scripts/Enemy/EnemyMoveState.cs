using UnityEngine;

/// <summary>
/// 敌人移动状态：向下压境移动，检测到墙体后退回待机。
/// </summary>
public class EnemyMoveState : EnemyState
{
    /// <summary>
    /// 创建移动状态实例。
    /// </summary>
    /// <param name="enemy">所属敌人实体。</param>
    /// <param name="machine">敌人状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public EnemyMoveState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    /// <summary>
    /// 进入移动状态：向下施加移速。
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        ApplyMoveVelocity(Vector2.down);
    }

    /// <summary>
    /// 每帧检测墙体；进入攻击范围则停止并切换至待机。
    /// </summary>
    public override void OnUpdate()
    {
        if (IsWallInAttackRange())
        {
            StopMovement();
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
