/// <summary>投射物移动方式。</summary>
/// <remarks><b>是否需要挂载：</b>否。</remarks>
public enum ProjectileMotionType
{
    /// <summary>直线飞行。</summary>
    Straight = 0,
    /// <summary>追踪目标转向。</summary>
    Homing = 1,
    /// <summary>弧线飞向目标点。</summary>
    ArcToPoint = 2,
    /// <summary>链式弹跳（预留）。</summary>
    Chain = 3,
    /// <summary>绕中心点轨道运动。</summary>
    Orbit = 4,
}
