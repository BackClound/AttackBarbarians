/// <summary>
/// 待机状态；射击由 <see cref="SkillShoot"/> 在检测到敌人时驱动，不再切入 <see cref="PlayerShootState"/>。
/// </summary>
public class PlayerIdleState : PlayerState
{
    /// <summary>创建待机状态实例。</summary>
    /// <param name="player">所属玩家实体。</param>
    /// <param name="machine">玩家状态机。</param>
    /// <param name="animName">Animator 状态名。</param>
    public PlayerIdleState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
    }
}
