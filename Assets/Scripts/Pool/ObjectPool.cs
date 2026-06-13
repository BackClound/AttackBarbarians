using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单 Prefab 泛型对象池：预热、借出、归还，并在生命周期内调用 <see cref="IPoolable"/>。
/// </summary>
/// <typeparam name="T">池内组件类型（通常为 Prefab 根上的 MonoBehaviour）。</typeparam>
public class ObjectPool<T> where T : Component
{
    private readonly T prefab;
    private readonly Transform defaultParent;
    private readonly int maxCount;
    private readonly bool canExpand;
    private readonly PoolOverflowPolicy overflowPolicy;
    // 不活跃对象队列
    private readonly Queue<T> inactive = new Queue<T>(16);
    // 活跃对象链表
    private readonly LinkedList<T> activeOrder = new LinkedList<T>();
    // 活跃对象字典
    private readonly Dictionary<T, LinkedListNode<T>> activeNodes = new Dictionary<T, LinkedListNode<T>>(64);
    // 池able缓存字典
    private readonly Dictionary<T, IPoolable> poolableCache = new Dictionary<T, IPoolable>(64);
    // 实例工厂函数
    private readonly Func<T, T> instanceFactory;

    // 不活跃对象数量
    /// <summary>当前处于空闲队列中的实例数量。</summary>
    public int InactiveCount => inactive.Count;
    // 活跃对象数量
    /// <summary>当前已借出并处于活跃状态的实例数量。</summary>
    public int ActiveCount => activeOrder.Count;
    // 总对象数量
    /// <summary>池中实例总数（空闲 + 活跃）。</summary>
    public int TotalCount => InactiveCount + ActiveCount;

    /// <summary>
    /// 创建对象池实例。
    /// </summary>
    /// <param name="prefab">实例模板组件。</param>
    /// <param name="defaultParent">默认父节点。</param>
    /// <param name="maxCount">最大容量。</param>
    /// <param name="canExpand">是否允许扩容。</param>
    /// <param name="overflowPolicy">溢出策略。</param>
    /// <param name="instanceFactory">自定义实例工厂，为 null 时使用 Instantiate。</param>
    public ObjectPool(
        T prefab,
        Transform defaultParent,
        int maxCount,
        bool canExpand,
        PoolOverflowPolicy overflowPolicy,
        Func<T, T> instanceFactory = null)
    {
        this.prefab = prefab;
        this.defaultParent = defaultParent;
        this.maxCount = Mathf.Max(1, maxCount);
        this.canExpand = canExpand;
        this.overflowPolicy = overflowPolicy;
        this.instanceFactory = instanceFactory;
    }

    /// <summary>
    /// 预热指定数量的实例到空闲队列。
    /// </summary>
    /// <param name="count">目标预热数量，不会超过最大容量。</param>
    public void Prewarm(int count)
    {
        int target = Mathf.Min(count, maxCount);
        while (TotalCount < target)
        {
            T instance = CreateInstance();
            if (instance == null)
            {
                break;
            }

            EnqueueInactive(instance);
        }
    }

    /// <summary>
    /// 借出并激活一个实例，触发 <see cref="IPoolable.OnSpawn"/>。
    /// </summary>
    /// <param name="position">世界坐标位置。</param>
    /// <param name="rotation">世界坐标旋转。</param>
    /// <param name="parent">父节点，可为 null。</param>
    /// <returns>借出的实例；池满且无法获取时返回 null。</returns>
    public T Spawn(Vector3 position, Quaternion rotation, Transform parent)
    {
        T instance = TakeInstance();
        if (instance == null)
        {
            return null;
        }

        ApplyTransform(instance, position, rotation, parent);
        instance.gameObject.SetActive(true);
        RegisterActive(instance);
        InvokePoolable(instance, spawn: true);
        return instance;
    }

    /// <summary>
    /// 借出实例但不计入活跃列表（用于 Skill 预创建并长期持有的子弹）。
    /// </summary>
    /// <param name="position">世界坐标位置。</param>
    /// <param name="rotation">世界坐标旋转。</param>
    /// <param name="parent">父节点，可为 null。</param>
    /// <returns>借出的未激活实例；池满且无法获取时返回 null。</returns>
    public T Allocate(Vector3 position, Quaternion rotation, Transform parent)
    {
        T instance = TakeInstance();
        if (instance == null)
        {
            return null;
        }

        ApplyTransform(instance, position, rotation, parent);
        instance.gameObject.SetActive(false);
        return instance;
    }

    /// <summary>
    /// 回收实例到池中，触发 <see cref="IPoolable.OnDespawn"/>。
    /// </summary>
    /// <param name="instance">要回收的实例。</param>
    public void Despawn(T instance)
    {
        if (instance == null)
        {
            return;
        }

        if (activeNodes.TryGetValue(instance, out LinkedListNode<T> node))
        {
            activeOrder.Remove(node);
            activeNodes.Remove(instance);
            InvokePoolable(instance, spawn: false);
        }

        EnqueueInactive(instance);
    }

    /// <summary>
    /// 归还未通过 <see cref="Spawn"/> 激活的租借实例（不触发 OnDespawn）。
    /// </summary>
    /// <param name="instance">要归还的实例。</param>
    public void ReturnAllocated(T instance)
    {
        if (instance == null)
        {
            return;
        }

        if (activeNodes.ContainsKey(instance))
        {
            Despawn(instance);
            return;
        }

        EnqueueInactive(instance);
    }

    /// <summary>
    /// 清空池中所有实例。
    /// </summary>
    /// <param name="destroyInstances">为 true 时销毁 GameObject，否则仅停用。</param>
    public void Clear(bool destroyInstances)
    {
        while (inactive.Count > 0)
        {
            T instance = inactive.Dequeue();
            DestroyIfNeeded(instance, destroyInstances);
        }

        LinkedListNode<T> node = activeOrder.First;
        while (node != null)
        {
            LinkedListNode<T> next = node.Next;
            DestroyIfNeeded(node.Value, destroyInstances);
            node = next;
        }

        activeOrder.Clear();
        activeNodes.Clear();
        poolableCache.Clear();
    }

    /// <summary>
    /// 判断指定实例是否由本池管理。
    /// </summary>
    /// <param name="instance">待检查的实例。</param>
    /// <returns>属于本池返回 true，否则返回 false。</returns>
    public bool Owns(T instance) =>
        instance != null && (activeNodes.ContainsKey(instance) || inactive.Contains(instance));

    /// <summary>
    /// 从空闲队列或新建实例中获取一个可用对象。
    /// </summary>
    /// <returns>可用实例；池满且策略拒绝时返回 null。</returns>
    private T TakeInstance()
    {
        while (inactive.Count > 0)
        {
            T instance = inactive.Dequeue();
            if (instance != null)
            {
                return instance;
            }
        }

        if (TotalCount < maxCount)
        {
            return CreateInstance();
        }

        if (canExpand && overflowPolicy == PoolOverflowPolicy.ExpandOrReject)
        {
            return CreateInstance();
        }

        if (overflowPolicy == PoolOverflowPolicy.RecycleOldest && activeOrder.Count > 0)
        {
            T oldest = activeOrder.First.Value;
            Despawn(oldest);
            return TakeInstance();
        }

        return null;
    }

    /// <summary>
    /// 创建新实例并缓存其 <see cref="IPoolable"/> 组件。
    /// </summary>
    /// <returns>新建的实例；创建失败时返回 null。</returns>
    private T CreateInstance()
    {
        T instance = instanceFactory != null ? instanceFactory(prefab) : UnityEngine.Object.Instantiate(prefab, defaultParent);
        if (instance != null)
        {
            instance.name = prefab.name;
            CachePoolable(instance);
            instance.gameObject.SetActive(false);
        }

        return instance;
    }

    /// <summary>
    /// 查找并缓存实例上的 <see cref="IPoolable"/> 实现。
    /// </summary>
    /// <param name="instance">目标实例。</param>
    private void CachePoolable(T instance)
    {
        if (instance == null || poolableCache.ContainsKey(instance))
        {
            return;
        }

        IPoolable poolable = instance as IPoolable ?? instance.GetComponent<IPoolable>();
        if (poolable != null)
        {
            poolableCache[instance] = poolable;
        }
    }

    /// <summary>
    /// 将实例登记为活跃状态并加入借出顺序链表。
    /// </summary>
    /// <param name="instance">要登记的实例。</param>
    private void RegisterActive(T instance)
    {
        LinkedListNode<T> node = activeOrder.AddLast(instance);
        activeNodes[instance] = node;
    }

    /// <summary>
    /// 将实例停用并放回空闲队列。
    /// </summary>
    /// <param name="instance">要入队的实例。</param>
    private void EnqueueInactive(T instance)
    {
        instance.gameObject.SetActive(false);
        Transform parent = defaultParent != null ? defaultParent : instance.transform.parent;
        instance.transform.SetParent(parent, false);
        inactive.Enqueue(instance);
    }

    /// <summary>
    /// 设置实例的父节点、位置与旋转。
    /// </summary>
    /// <param name="instance">目标实例。</param>
    /// <param name="position">世界坐标位置。</param>
    /// <param name="rotation">世界坐标旋转。</param>
    /// <param name="parent">父节点。</param>
    private static void ApplyTransform(T instance, Vector3 position, Quaternion rotation, Transform parent)
    {
        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(parent, false);
        instanceTransform.SetPositionAndRotation(position, rotation);
    }

    /// <summary>
    /// 调用实例的 <see cref="IPoolable"/> 生命周期回调。
    /// </summary>
    /// <param name="instance">目标实例。</param>
    /// <param name="spawn">为 true 时调用 OnSpawn，否则调用 OnDespawn。</param>
    private void InvokePoolable(T instance, bool spawn)
    {
        if (!poolableCache.TryGetValue(instance, out IPoolable poolable))
        {
            CachePoolable(instance);
            poolableCache.TryGetValue(instance, out poolable);
        }

        if (poolable == null)
        {
            return;
        }

        if (spawn)
        {
            poolable.OnSpawn();
        }
        else
        {
            poolable.OnDespawn();
        }
    }

    /// <summary>
    /// 按需销毁实例的 GameObject。
    /// </summary>
    /// <param name="instance">目标实例。</param>
    /// <param name="destroyInstances">为 true 时执行销毁。</param>
    private static void DestroyIfNeeded(T instance, bool destroyInstances)
    {
        if (destroyInstances && instance != null)
        {
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}
