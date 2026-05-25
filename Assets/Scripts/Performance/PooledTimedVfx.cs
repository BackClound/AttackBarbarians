using UnityEngine;

/// <summary>
/// 短时战斗特效：播放后按时回收到 <see cref="PoolManager"/>。
/// </summary>
[DisallowMultipleComponent]
public class PooledTimedVfx : MonoBehaviour, IPoolable
{
    [SerializeField] private float lifetimeSeconds = 0.45f;

    private float remaining;
    private bool isActive;

    public void Play(float durationOverride = -1f)
    {
        remaining = durationOverride > 0f ? durationOverride : lifetimeSeconds;
        isActive = true;
        gameObject.SetActive(true);
    }

    public void OnSpawn()
    {
        remaining = lifetimeSeconds;
        isActive = true;
    }

    public void OnDespawn()
    {
        isActive = false;
        remaining = 0f;
    }

    private void Update()
    {
        if (!isActive)
        {
            return;
        }

        remaining -= Time.deltaTime;
        if (remaining <= 0f)
        {
            Recycle();
        }
    }

    private void Recycle()
    {
        isActive = false;
        if (ServiceLocator.TryGet(out PerformanceManager performance))
        {
            performance.Release(PerformanceBudgetCategory.CombatVfx);
        }

        if (ServiceLocator.TryGet(out PoolManager poolManager) && poolManager.IsManagedInstance(gameObject))
        {
            poolManager.Despawn(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
