/// <summary>
/// 待机状态；射击表现由 <see cref="SkillShoot"/> 经 <see cref="Entity.AnimationDriver"/> 驱动。
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
