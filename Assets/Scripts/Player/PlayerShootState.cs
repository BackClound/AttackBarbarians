using UnityEngine;

/// <summary>
/// 射击动画状态（仅表现）。发弹由 <see cref="SkillShoot"/> 按敌人检测触发，不经过 <see cref="OnAnimAttackTrigger"/>。
/// </summary>
public class PlayerShootState : PlayerState
{
    public PlayerShootState(Player player, StateMachine machine, string animName) : base(player, machine, animName) { }

    public override void OnEnter()
    {
        base.OnEnter();
        ApplyShootSpeedFromSources();
    }

    public override void OnUpdate()
    {
        if (ShouldReturnToIdle())
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void OnAnimAttackTrigger()
    {
        // 射击改由 SkillShoot 检测驱动，忽略动画攻击帧。
    }

    private void ApplyShootSpeedFromSources()
    {
        float shootSpeedMulti = 1f;
        if (player.skillManager?.ShootController != null)
        {
            shootSpeedMulti = player.skillManager.ShootController.GetAnimSpeedMultiplier();
        }
        else if (Controller != null && Controller.RuntimeStats.IsInitialized)
        {
            shootSpeedMulti = Controller.RuntimeStats.Get(StatType.AttackSpeedMulti);
        }
        else if (player.player_Health != null && player.player_Health.entity_Stats != null)
        {
            shootSpeedMulti = player.player_Health.entity_Stats.GetAttackSpeedMultiplier();
        }

        if (anim != null)
        {
            anim.SetFloat("ShootSpeedMulti", shootSpeedMulti);
        }
    }
}
