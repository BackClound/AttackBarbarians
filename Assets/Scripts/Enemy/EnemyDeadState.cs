using UnityEngine;

/// <summary>
/// 敌人死亡状态：播放死亡动画，结束后回收或销毁实例。
/// </summary>
public class EnemyDeadState : EnemyState
{
    /// <summary>
    /// 创建死亡状态实例。
    /// </summary>
    /// <param name="enemy">所属敌人实体。</param>
    /// <param name="machine">敌人状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public EnemyDeadState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    /// <summary>
    /// 进入死亡状态：停止移动并播放死亡动画。
    /// </summary>
    public override void OnEnter()
    {
        base.OnEnter();
        StopMovement();
    }

    /// <summary>
    /// 死亡动画结束后调用 <see cref="Enemy.Die"/> 回收实例。
    /// </summary>
    public override void OnUpdate()
    {
        if (isAnimFinished)
        {
            enemy.Die();
        }
    }
}
