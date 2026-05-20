/// <summary>
/// Player 自动攻击的目标选择策略。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="PlayerDataSO"/> 与 <see cref="PlayerTargetScanner"/> 使用。</para>
/// </remarks>
public enum PlayerTargetPolicy
{
    /// <summary>射程内距离扫描原点最近。</summary>
    Nearest = 0,

    /// <summary>射程内当前血量最低。</summary>
    LowestHealth = 1,

    /// <summary>射程内距离墙体/终点最近（竖屏防守：优先靠近底部的敌人）。</summary>
    NearestToWall = 2,

    /// <summary>优先 Boss，同优先级下按 <see cref="NearestToWall"/> 排序。</summary>
    BossFirst = 3,
}
