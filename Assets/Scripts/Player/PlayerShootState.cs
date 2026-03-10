using UnityEngine;

public class PlayerShootState : PlayerState
{
    private SkillShoot skillShoot;
    public PlayerShootState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
        skillShoot = player.skillManager.sKillShoot;
    }
}
