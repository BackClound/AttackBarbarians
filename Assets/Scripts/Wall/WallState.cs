using UnityEngine;

public class WallState : EntityState
{
    protected WallControlManager wallControl;
    public WallState(WallControlManager wallControl, StateMachine machine, string animName) : base(machine, animName)
    {
        this.wallControl = wallControl;
        anim = wallControl.anim;
    }
}
