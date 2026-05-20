using UnityEngine;

/// <summary>
/// 玩家状态基类：战斗准入与目标扫描经 <see cref="PlayerController"/>。
/// </summary>
public class PlayerState : EntityState
{
    protected Player player;
    protected PlayerController Controller => player != null ? player.controller : null;

    public PlayerState(Player player, StateMachine machine, string animName) : base(machine, animName)
    {
        this.player = player;
        this.anim = player.anim;
    }

    protected bool TryEnterShootState()
    {
        if (player == null || player.shootState == null)
        {
            return false;
        }

        Controller?.ScanCombatTargets();

        if (Controller != null && Controller.CanEnterCombatState())
        {
            stateMachine.ChangeState(player.shootState);
            return true;
        }

        return false;
    }

    protected bool ShouldReturnToIdle()
    {
        return Controller == null || !Controller.CanEnterCombatState();
    }
}
