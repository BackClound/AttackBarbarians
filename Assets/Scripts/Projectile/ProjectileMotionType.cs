/// <summary>投射物移动方式。</summary>
public enum ProjectileMotionType
{
    // 直线
    Straight = 0,
    // 追踪
    Homing = 1,
    // 弧线
    ArcToPoint = 2,
    // 链式
    Chain = 3,
    // 轨道
    Orbit = 4,
}
