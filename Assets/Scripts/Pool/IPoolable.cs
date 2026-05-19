/// <summary>
/// 对象池生命周期：从池中取出与放回时由 <see cref="PoolManager"/> / <see cref="ObjectPool{T}"/> 调用。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（接口）。在子弹、敌人、伤害飘字等 Prefab 根物体上的 MonoBehaviour 实现。</para>
/// </remarks>
public interface IPoolable
{
    /// <summary>实例被激活并投入使用时调用（在 SetActive(true) 之后）。</summary>
    void OnSpawn();

    /// <summary>实例被回收进池时调用（在 SetActive(false) 之前）。</summary>
    void OnDespawn();
}
