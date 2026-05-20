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

    public EnemyState(Enemy enemy, StateMachine machine, string animName) : base(machine, animName)
    {
        this.enemy = enemy;
        this.anim = enemy.anim;
        this.rb = enemy.rb;
    }

    protected bool IsWallInAttackRange()
    {
        if (Controller != null && Controller.IsReady)
        {
            return Controller.IsWallInAttackRange();
        }

        return enemy != null && enemy.IsWallDetected();
    }

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

        enemy.SetVelocity(direction.normalized * speed);
    }

    protected void StopMovement()
    {
        enemy?.SetVelocity(Vector2.zero);
    }
}
