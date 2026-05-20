using UnityEngine;

public class PlayerState : EntityState
{
    protected Player player;
    protected PlayerController Controller => player != null ? player.controller : null;

    public PlayerState(Player player, StateMachine machine, string animName) : base(machine, animName)
    {
        this.player = player;
        this.anim = player.anim;
    }
}
