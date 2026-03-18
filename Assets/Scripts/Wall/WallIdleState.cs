using UnityEngine;

public class WallIdleState : WallState
{
    public WallIdleState(WallControlManager wallControl, StateMachine machine, string animName) : base(wallControl, machine, animName)
    {
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (wallControl.beDamaged)
        {
            stateMachine.ChangeState(wallControl.damageState);
        }
    }
}
