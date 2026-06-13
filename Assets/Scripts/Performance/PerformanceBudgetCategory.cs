/// <summary>
/// 性能预算分类，供 <see cref="PerformanceManager"/> 计数与限流。
/// </summary>
public enum PerformanceBudgetCategory
{
    /// <summary>同屏敌人数量。</summary>
    Enemy = 0,

    /// <summary>同屏投射物数量。</summary>
    Projectile = 1,

    /// <summary>同屏伤害飘字数量。</summary>
    DamageNumber = 2,

    /// <summary>同屏战斗特效数量。</summary>
    CombatVfx = 3,
}
