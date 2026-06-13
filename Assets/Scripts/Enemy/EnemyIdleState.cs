using UnityEngine;

/// <summary>
/// 敌人待机状态：冷却结束后根据墙体探测切换至攻击或移动状态。
/// </summary>
public class EnemyIdleState : EnemyState
{
    /// <summary>
    /// 创建待机状态实例。
    /// </summary>
    /// <param name="enemy">所属敌人实体。</param>
    /// <param name="machine">敌人状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public EnemyIdleState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    /// <summary>
    /// 进入待机：重置攻击冷却并停止移动。
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        cooldownThreshold = enemy.cooldownThreshold;
        cooldownTimer = cooldownThreshold;
        StopMovement();
    }

    /// <summary>
    /// 每帧递减冷却；冷却结束后按墙体探测切换攻击或移动状态。
    /// </summary>
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
