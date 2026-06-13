using UnityEngine;

/// <summary>
/// 射击动画状态（仅表现）。发弹由 <see cref="SkillShoot"/> 按敌人检测触发，不经过 <see cref="OnAnimAttackTrigger"/>。
/// </summary>
public class PlayerShootState : PlayerState
{
    /// <summary>创建射击动画状态实例。</summary>
    /// <param name="player">所属玩家实体。</param>
    /// <param name="machine">玩家状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public PlayerShootState(Player player, StateMachine machine, string animName) : base(player, machine, animName) { }

    /// <summary>进入射击状态：同步动画攻速倍率。</summary>
    public override void OnEnter()
    {
        base.OnEnter();
        ApplyShootSpeedFromSources();
    }

    /// <summary>每帧检测；无法继续战斗时转换至 <see cref="PlayerIdleState"/>。</summary>
    public override void OnUpdate()
    {
        if (ShouldReturnToIdle())
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    /// <summary>忽略 Animator 攻击帧（发弹由 <see cref="SkillShoot"/> 驱动）。</summary>
    public override void OnAnimAttackTrigger()
    {
        // 射击改由 SkillShoot 检测驱动，忽略动画攻击帧。
    }

    /// <summary>从技能控制器或属性快照读取攻速并写入 Animator。</summary>
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
