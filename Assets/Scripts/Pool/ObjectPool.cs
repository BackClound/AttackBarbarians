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
    private readonly Queue<T> inactive = new Queue<T>(16);
    private readonly LinkedList<T> activeOrder = new LinkedList<T>();
    private readonly Dictionary<T, LinkedListNode<T>> activeNodes = new Dictionary<T, LinkedListNode<T>>(64);
    private readonly Dictionary<T, IPoolable> poolableCache = new Dictionary<T, IPoolable>(64);
    private readonly Func<T, T> instanceFactory;

    public int InactiveCount => inactive.Count;
    public int ActiveCount => activeOrder.Count;
    public int TotalCount => InactiveCount + ActiveCount;

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

    public bool Owns(T instance) =>
        instance != null && (activeNodes.ContainsKey(instance) || inactive.Contains(instance));

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

    private void RegisterActive(T instance)
    {
        LinkedListNode<T> node = activeOrder.AddLast(instance);
        activeNodes[instance] = node;
    }

    private void EnqueueInactive(T instance)
    {
        instance.gameObject.SetActive(false);
        Transform parent = defaultParent != null ? defaultParent : instance.transform.parent;
        instance.transform.SetParent(parent, false);
        inactive.Enqueue(instance);
    }

    private static void ApplyTransform(T instance, Vector3 position, Quaternion rotation, Transform parent)
    {
        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(parent, false);
        instanceTransform.SetPositionAndRotation(position, rotation);
    }

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

    private static void DestroyIfNeeded(T instance, bool destroyInstances)
    {
        if (destroyInstances && instance != null)
        {
            UnityEngine.Object.Destroy(instance.gameObject);
        }
    }
}
