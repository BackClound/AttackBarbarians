public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (Controller != null && Controller.CanEnterCombatState())
        {
            stateMachine.ChangeState(player.shootState);
        }
    }
}
