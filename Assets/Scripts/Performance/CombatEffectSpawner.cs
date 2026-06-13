using UnityEngine;

/// <summary>
/// 战斗命中特效生成：优先对象池，受 <see cref="PerformanceManager"/> 预算约束。
/// </summary>
public static class CombatEffectSpawner
{
    /// <summary>
    /// 尝试生成命中特效，受性能预算与对象池约束。
    /// </summary>
    /// <param name="prefab">特效 Prefab 模板。</param>
    /// <param name="position">生成位置。</param>
    /// <param name="rotation">生成旋转。</param>
    /// <param name="lifetimeSeconds">特效持续时间（秒），小于等于 0 时使用组件默认值。</param>
    /// <returns>生成成功返回 true，预算不足或 Prefab 为空时返回 false。</returns>
    public static bool TrySpawnHitEffect(GameObject prefab, Vector3 position, Quaternion rotation, float lifetimeSeconds = -1f)
    {
        if (prefab == null)
        {
            return false;
        }

        if (ServiceLocator.TryGet(out PerformanceManager performance) &&
            !performance.TryAcquire(PerformanceBudgetCategory.CombatVfx))
        {
            return false;
        }

        GameObject instance = null;
        if (ServiceLocator.TryGet(out PoolManager poolManager) &&
            poolManager.HasPool(GameConstants.PoolKeys.CombatVfx))
        {
            instance = poolManager.Spawn(GameConstants.PoolKeys.CombatVfx, position, rotation);
        }

        if (instance == null)
        {
            instance = Object.Instantiate(prefab, position, rotation);
        }

        if (instance == null)
        {
            if (ServiceLocator.TryGet(out PerformanceManager performanceManager))
            {
                performanceManager.Release(PerformanceBudgetCategory.CombatVfx);
            }

            return false;
        }

        if (!instance.TryGetComponent(out PooledTimedVfx timedVfx))
        {
            timedVfx = instance.AddComponent<PooledTimedVfx>();
        }

        timedVfx.Play(lifetimeSeconds);
        return true;
    }
}
