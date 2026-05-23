/// <summary>
/// Boss 技能类型（与 <see cref="BossSkillDataSO"/> 对应）。
/// </summary>
public enum BossSkillType
{
    None = 0,
    /// <summary>冲锋</summary>
    Charge = 1,
    /// <summary>召唤</summary>
    Summon = 2,
    /// <summary>范围攻击</summary>
    AreaAttack = 3,
    /// <summary>护盾</summary>
    Shield = 4,
    /// <summary>弹幕</summary>
    Barrage = 5,
}

/// <summary>
/// Boss 阶段切换条件。
/// </summary>
public enum BossPhaseTransitionMode
{
    /// <summary>按当前血量 / 最大血量比例阈值切换。</summary>
    HealthRatio = 0,
    /// <summary>按出场后经过时间（秒）切换。</summary>
    ElapsedTime = 1,
}
