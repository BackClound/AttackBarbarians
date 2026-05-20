/// <summary>
/// 碰撞体在战斗查询中的语义角色。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。</remarks>
public enum CollisionRole
{
    None = 0,
    // 受击区域
    Hurtbox = 1,
    // 攻击区域
    Hitbox = 2,
    // 攻击传感器
    AttackSensor = 3,
    // 墙体传感器
    WallSensor = 4,
}
