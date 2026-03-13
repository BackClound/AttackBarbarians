using UnityEngine;

public class EnemyDeadState : EnemyState
{
    public EnemyDeadState(Enemy enemy, StateMachine machine, string animName) : base(enemy, machine, animName)
    {

    }
}
