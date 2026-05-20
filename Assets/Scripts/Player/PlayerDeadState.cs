public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
    }

    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void OnUpdate()
    {
    }
}
