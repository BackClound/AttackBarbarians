using UnityEngine;

public class EnemyIdleState : EnemyState
{
    public EnemyIdleState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
        //使用move的动画，但是velocity设置为0
        cooldownThreshold = enemy.cooldownThreshold;
        cooldownTimer = cooldownThreshold;
        rb.linearVelocity = Vector2.zero;
    }
    public override void OnUpdate()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer <= 0)
        {
            //当检测到wall之后，先切换idle State,在切换到Attack State， 优化玩家体验
            //TODO 是否定义attackcoolDownTimer 专门用于控制开始攻击前摇时长
            if (enemy.isWallDetected())
            {
                stateMachine.ChangeState(enemy.attackState);
            }
            else
            {
                stateMachine.ChangeState(enemy.moveState);
            }

        }

    }
}
