using UnityEngine;

/// <summary>
/// 战斗命中特效生成：优先对象池，受 <see cref="PerformanceManager"/> 预算约束。
/// </summary>
public static class CombatEffectSpawner
{
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
