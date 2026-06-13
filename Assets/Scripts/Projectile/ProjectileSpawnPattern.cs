/// <summary>一次技能释放的弹道排布。</summary>
/// <remarks><b>是否需要挂载：</b>否。</remarks>
public enum ProjectileSpawnPattern
{
    /// <summary>单发直线。</summary>
    Single = 0,
    /// <summary>扇形多弹道。</summary>
    Fan = 1,
    /// <summary>环形均匀分布。</summary>
    Ring = 2,
    /// <summary>多波次连发（预留）。</summary>
    MultiWave = 3,
}
