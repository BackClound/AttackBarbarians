using UnityEngine;

public class WallDamageState : WallState
{
    public WallDamageState(WallControlManager wallControl, StateMachine machine, string animName) : base(wallControl, machine, animName)
    {
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (isAnimFinished)
        {
            wallControl.ResetDamageState(false);
            stateMachine.ChangeState(wallControl.idleState);
        }
    }
}
