/// <summary>
/// 待机状态；射击由 <see cref="SkillShoot"/> 在检测到敌人时驱动，不再切入 <see cref="PlayerShootState"/>。
/// </summary>
public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(Player player, StateMachine machine, string animName) : base(player, machine, animName)
    {
    }
}
