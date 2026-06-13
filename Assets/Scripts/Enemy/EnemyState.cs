using UnityEngine;

/// <summary>
/// 敌人状态基类：墙体探测与移动统一经 <see cref="EnemyController"/>（就绪时）。
/// </summary>
public class EnemyState : EntityState
{
    protected Enemy enemy;
    protected EnemyController Controller => enemy != null ? enemy.controller : null;
    protected float cooldownThreshold;
    protected float cooldownTimer;

    /// <summary>
    /// 创建敌人状态基类实例。
    /// </summary>
    /// <param name="enemy">所属敌人实体。</param>
    /// <param name="machine">敌人状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public EnemyState(Enemy enemy, StateMachine machine, string animName) : base(machine, animName)
    {
        this.enemy = enemy;
        this.anim = enemy.anim;
        this.rb = enemy.rb;
    }

    /// <summary>
    /// 检测墙体是否在攻击范围内。
    /// </summary>
    /// <returns>可攻击墙体时为 <c>true</c>。</returns>
    protected bool IsWallInAttackRange()
    {
        if (Controller != null && Controller.IsReady)
        {
            return Controller.IsWallInAttackRange();
        }

        return enemy != null && enemy.IsWallDetected();
    }

    /// <summary>
    /// 按方向设置移动速度，受 <see cref="EnemyStatusController"/> 控制状态影响。
    /// </summary>
    /// <param name="direction">移动方向。</param>
    protected void ApplyMoveVelocity(Vector2 direction)
    {
        if (enemy == null)
        {
            return;
        }

        float speed = enemy.moveSpeed;
        if (Controller != null && Controller.IsReady && Controller.Enemy != null)
        {
            speed = Controller.Enemy.moveSpeed;
        }

        EnemyStatusController status = enemy.GetComponent<EnemyStatusController>();
        if (status != null)
        {
            speed *= status.MoveSpeedMultiplier;
            if (status.IsMovementBlocked)
            {
                enemy.SetVelocity(Vector2.zero);
                return;
            }
        }

        enemy.SetVelocity(direction.normalized * speed);
    }

    /// <summary>
    /// 停止移动（速度归零）。
    /// </summary>
    protected void StopMovement()
    {
        enemy?.SetVelocity(Vector2.zero);
    }
}
