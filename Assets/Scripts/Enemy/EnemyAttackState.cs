using UnityEngine;

public class EnemyAttackState : EnemyState
{
    public EnemyAttackState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        //当攻击结束之后，冷却一定时间继续攻击，增加玩家体验
        if (isAnimFinished)
        {
            stateMachine.ChangeState(enemy.idleState);
        }
        if (!enemy.isWallDetected())
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
