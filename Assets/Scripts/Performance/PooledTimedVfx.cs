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

    /// <summary>
    /// 播放特效并启动生命周期倒计时。
    /// </summary>
    /// <param name="durationOverride">自定义持续时间（秒），小于等于 0 时使用序列化默认值。</param>
    public void Play(float durationOverride = -1f)
    {
        remaining = durationOverride > 0f ? durationOverride : lifetimeSeconds;
        isActive = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 实例从对象池借出时调用，重置生命周期计时。
    /// </summary>
    public void OnSpawn()
    {
        remaining = lifetimeSeconds;
        isActive = true;
    }

    /// <summary>
    /// 实例回收到对象池时调用，停止计时。
    /// </summary>
    public void OnDespawn()
    {
        isActive = false;
        remaining = 0f;
    }

    /// <summary>
    /// 每帧递减剩余时间，到期后回收特效。
    /// </summary>
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

    /// <summary>
    /// 释放性能预算并将特效归还对象池或销毁。
    /// </summary>
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
