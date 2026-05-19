using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池管理器：按 Key 注册、预热、Spawn、Despawn、清空，供敌人、子弹、飘字等高频对象复用。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。</para>
/// <para><b>推荐挂载对象：</b>在 <c>GameSystems</c> 下创建子物体 <c>PoolRoot</c>，将本组件挂在 <c>PoolRoot</c> 上。</para>
/// <para><b>不要挂载到：</b>Player、单个 Enemy Prefab、子弹 Prefab 上。</para>
/// <para><b>Inspector 配置：</b>拖入 <see cref="PoolConfigSO"/> 和/或在 <c>Inspector Pools</c> 中填写条目；Key 与 <see cref="GameConstants.PoolKeys"/> 一致。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;PoolManager&gt;()</c>。</para>
/// </remarks>
public class PoolManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private PoolConfigSO poolConfig;
    [SerializeField] private List<PoolEntry> inspectorPools = new List<PoolEntry>();

    private readonly Dictionary<string, GameObjectPoolHandle> pools = new Dictionary<string, GameObjectPoolHandle>(32);

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        pools.Clear();
        RegisterEntries(poolConfig != null ? poolConfig.Entries : null);
        RegisterEntries(inspectorPools);
        IsInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        ClearAll(destroyInstances: true);
        IsInitialized = false;
    }

    public bool HasPool(string key) => !string.IsNullOrEmpty(key) && pools.ContainsKey(key);

    public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!TryGetPool(key, out GameObjectPoolHandle handle))
        {
            return null;
        }

        Transform instance = handle.Pool.Spawn(position, rotation, parent != null ? parent : handle.Entry.Parent);
        return instance != null ? instance.gameObject : null;
    }

    public T Spawn<T>(string key, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        GameObject instance = Spawn(key, position, rotation, parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    /// <summary>
    /// 借出未激活实例（不触发 OnSpawn），供 Skill 预创建子弹列表等场景。
    /// </summary>
    public GameObject Allocate(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!TryGetPool(key, out GameObjectPoolHandle handle))
        {
            return null;
        }

        Transform instance = handle.Pool.Allocate(position, rotation, parent != null ? parent : handle.Entry.Parent);
        return instance != null ? instance.gameObject : null;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryFindPoolForInstance(instance, out GameObjectPoolHandle handle))
        {
            Destroy(instance);
            return;
        }

        handle.Pool.Despawn(instance.transform);
    }

    public void ReturnAllocated(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!TryFindPoolForInstance(instance, out GameObjectPoolHandle handle))
        {
            Destroy(instance);
            return;
        }

        handle.Pool.ReturnAllocated(instance.transform);
    }

    public bool IsManagedInstance(GameObject instance)
    {
        return instance != null && TryFindPoolForInstance(instance, out _);
    }

    public void ClearAll(bool destroyInstances)
    {
        foreach (KeyValuePair<string, GameObjectPoolHandle> pair in pools)
        {
            pair.Value.Pool.Clear(destroyInstances);
        }

        if (destroyInstances)
        {
            pools.Clear();
        }
    }

    public void ClearPool(string key, bool destroyInstances)
    {
        if (pools.TryGetValue(key, out GameObjectPoolHandle handle))
        {
            handle.Pool.Clear(destroyInstances);
            if (destroyInstances)
            {
                pools.Remove(key);
            }
        }
    }

    private void RegisterEntries(IReadOnlyList<PoolEntry> entries)
    {
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RegisterEntry(entries[i]);
        }
    }

    private void RegisterEntry(PoolEntry entry)
    {
        if (entry == null || !entry.IsValid)
        {
            return;
        }

        Transform parent = entry.Parent != null ? entry.Parent : transform;
        int prewarm = entry.InitialCount;
        ConfigManager configManager = ServiceLocator.TryGet(out ConfigManager manager) ? manager : null;
        if (prewarm <= 0 && configManager != null && configManager.GameConfig != null)
        {
            prewarm = configManager.GameConfig.DefaultPoolPrewarmCount;
        }

        bool canExpand = entry.CanExpand;
        if (configManager != null && configManager.GameConfig != null && !configManager.GameConfig.AllowPoolGrowth)
        {
            canExpand = false;
        }

        var pool = new ObjectPool<Transform>(
            entry.Prefab.transform,
            parent,
            entry.MaxCount,
            canExpand,
            entry.OverflowPolicy,
            prefabTransform => Instantiate(entry.Prefab, parent).transform);

        pool.Prewarm(prewarm);
        pools[entry.Key] = new GameObjectPoolHandle(entry, pool);
    }

    private bool TryGetPool(string key, out GameObjectPoolHandle handle)
    {
        if (string.IsNullOrEmpty(key))
        {
            handle = null;
            return false;
        }

        return pools.TryGetValue(key, out handle);
    }

    private bool TryFindPoolForInstance(GameObject instance, out GameObjectPoolHandle handle)
    {
        foreach (KeyValuePair<string, GameObjectPoolHandle> pair in pools)
        {
            if (pair.Value.Pool.Owns(instance.transform))
            {
                handle = pair.Value;
                return true;
            }
        }

        handle = null;
        return false;
    }

    private sealed class GameObjectPoolHandle
    {
        public GameObjectPoolHandle(PoolEntry entry, ObjectPool<Transform> pool)
        {
            Entry = entry;
            Pool = pool;
        }

        public PoolEntry Entry { get; }
        public ObjectPool<Transform> Pool { get; }
    }
}
