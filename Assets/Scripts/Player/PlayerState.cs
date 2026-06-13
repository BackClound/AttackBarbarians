using UnityEngine;

/// <summary>
/// 玩家状态基类：战斗准入与目标扫描经 <see cref="PlayerController"/>。
/// </summary>
public class PlayerState : EntityState
{
    protected Player player;
    protected PlayerController Controller => player != null ? player.controller : null;

    /// <summary>创建玩家状态基类实例。</summary>
    /// <param name="player">所属玩家实体。</param>
    /// <param name="machine">玩家状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public PlayerState(Player player, StateMachine machine, string animName) : base(machine, animName)
    {
        this.player = player;
        this.anim = player.anim;
    }

    /// <summary>扫描目标并在可战斗时切换至 <see cref="PlayerShootState"/>。</summary>
    /// <returns>若成功切入射击状态则为 <c>true</c>。</returns>
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

    /// <summary>判断是否应退回待机（无法继续战斗）。</summary>
    /// <returns>无法进入战斗状态时为 <c>true</c>。</returns>
    protected bool ShouldReturnToIdle()
    {
        return Controller == null || !Controller.CanEnterCombatState();
    }
}
