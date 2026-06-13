/// <summary>
/// 碰撞体在战斗查询中的语义角色。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。</remarks>
public enum CollisionRole
{
    /// <summary>未指定角色。</summary>
    None = 0,
    /// <summary>受击区域（Hurtbox）。</summary>
    Hurtbox = 1,
    /// <summary>攻击判定区域（Hitbox）。</summary>
    Hitbox = 2,
    /// <summary>攻击距离传感器。</summary>
    AttackSensor = 3,
    /// <summary>墙体距离传感器。</summary>
    WallSensor = 4,
}
