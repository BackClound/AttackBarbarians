using UnityEngine;

public class PlayerDeadState : PlayerState
{
    public PlayerDeadState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
    }
}
