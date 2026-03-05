using UnityEngine;

public class PlayerState : EntityState
{
    protected Player player;
    public PlayerState(Player player, StateMachine machine, string animName) : base(machine, animName)
    {
        this.player = player;
        this.anim = player.anim;
    }
}
