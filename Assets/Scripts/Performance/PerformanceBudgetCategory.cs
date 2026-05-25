/// <summary>
/// 性能预算分类，供 <see cref="PerformanceManager"/> 计数与限流。
/// </summary>
public enum PerformanceBudgetCategory
{
    Enemy = 0,
    Projectile = 1,
    DamageNumber = 2,
    CombatVfx = 3,
}
