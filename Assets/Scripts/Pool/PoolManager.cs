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
    private int runtimeDefaultPrewarm;
    private bool runtimeAllowPoolGrowth = true;

    /// <summary>管理器是否已完成初始化。</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// 由 Bootstrap 在 <see cref="Initialize"/> 前注入，避免 Pool 程序集反向依赖 Config。
    /// </summary>
    /// <param name="defaultPrewarm">条目未指定 InitialCount 时使用的默认预热数量。</param>
    /// <param name="allowPoolGrowth">是否允许运行时池扩容。</param>
    public void ConfigureRuntimePolicy(int defaultPrewarm, bool allowPoolGrowth)
    {
        runtimeDefaultPrewarm = Mathf.Max(0, defaultPrewarm);
        runtimeAllowPoolGrowth = allowPoolGrowth;
    }

    /// <summary>
    /// 注册配置条目、预热各池并完成初始化。
    /// </summary>
    public void Initialize()
    {
        pools.Clear();
        RegisterEntries(poolConfig != null ? poolConfig.Entries : null);
        RegisterEntries(inspectorPools);
        IsInitialized = true;
    }

    /// <summary>
    /// 每帧更新（当前无逻辑）。
    /// </summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭管理器并销毁所有池内实例。
    /// </summary>
    public void Shutdown()
    {
        ClearAll(destroyInstances: true);
        IsInitialized = false;
    }

    /// <summary>
    /// 检查指定 Key 的对象池是否已注册。
    /// </summary>
    /// <param name="key">池标识。</param>
    /// <returns>已注册返回 true，否则返回 false。</returns>
    public bool HasPool(string key) => !string.IsNullOrEmpty(key) && pools.ContainsKey(key);

    /// <summary>
    /// 从指定池中借出并激活一个 GameObject。
    /// </summary>
    /// <param name="key">池标识。</param>
    /// <param name="position">世界坐标位置。</param>
    /// <param name="rotation">世界坐标旋转。</param>
    /// <param name="parent">父节点，为 null 时使用条目默认父节点。</param>
    /// <returns>借出的 GameObject；池不存在或已满时返回 null。</returns>
    public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!TryGetPool(key, out GameObjectPoolHandle handle))
        {
            return null;
        }

        Transform instance = handle.Pool.Spawn(position, rotation, parent != null ? parent : handle.Entry.Parent);
        return instance != null ? instance.gameObject : null;
    }

    /// <summary>
    /// 从指定池中借出并激活指定组件类型的实例。
    /// </summary>
    /// <typeparam name="T">要获取的组件类型。</typeparam>
    /// <param name="key">池标识。</param>
    /// <param name="position">世界坐标位置。</param>
    /// <param name="rotation">世界坐标旋转。</param>
    /// <param name="parent">父节点，为 null 时使用条目默认父节点。</param>
    /// <returns>借出的组件；池不存在或已满时返回 null。</returns>
    public T Spawn<T>(string key, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        GameObject instance = Spawn(key, position, rotation, parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    /// <summary>
    /// 借出未激活实例（不触发 OnSpawn），供 Skill 预创建子弹列表等场景。
    /// </summary>
    /// <param name="key">池标识。</param>
    /// <param name="position">世界坐标位置。</param>
    /// <param name="rotation">世界坐标旋转。</param>
    /// <param name="parent">父节点，为 null 时使用条目默认父节点。</param>
    /// <returns>借出的未激活 GameObject；池不存在或已满时返回 null。</returns>
    public GameObject Allocate(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!TryGetPool(key, out GameObjectPoolHandle handle))
        {
            return null;
        }

        Transform instance = handle.Pool.Allocate(position, rotation, parent != null ? parent : handle.Entry.Parent);
        return instance != null ? instance.gameObject : null;
    }

    /// <summary>
    /// 将实例回收至其所属对象池。
    /// </summary>
    /// <param name="instance">要回收的 GameObject。</param>
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

    /// <summary>
    /// 归还未通过 <see cref="Spawn"/> 激活的租借实例。
    /// </summary>
    /// <param name="instance">要归还的 GameObject。</param>
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

    /// <summary>
    /// 判断指定 GameObject 是否由本管理器托管。
    /// </summary>
    /// <param name="instance">待检查的 GameObject。</param>
    /// <returns>受管返回 true，否则返回 false。</returns>
    public bool IsManagedInstance(GameObject instance)
    {
        return instance != null && TryFindPoolForInstance(instance, out _);
    }

    /// <summary>
    /// 清空所有已注册对象池。
    /// </summary>
    /// <param name="destroyInstances">为 true 时销毁实例并移除池注册。</param>
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

    /// <summary>
    /// 清空指定 Key 的对象池。
    /// </summary>
    /// <param name="key">池标识。</param>
    /// <param name="destroyInstances">为 true 时销毁实例并移除池注册。</param>
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

    /// <summary>
    /// 批量注册对象池条目。
    /// </summary>
    /// <param name="entries">条目列表，可为 null。</param>
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

    /// <summary>
    /// 注册单个对象池条目并执行预热。
    /// </summary>
    /// <param name="entry">池配置条目。</param>
    private void RegisterEntry(PoolEntry entry)
    {
        if (entry == null || !entry.IsValid)
        {
            return;
        }

        Transform parent = entry.Parent != null ? entry.Parent : transform;
        int prewarm = entry.InitialCount;
        if (prewarm <= 0)
        {
            prewarm = runtimeDefaultPrewarm;
        }

        bool canExpand = entry.CanExpand && runtimeAllowPoolGrowth;

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

    /// <summary>
    /// 按 Key 查找已注册的对象池句柄。
    /// </summary>
    /// <param name="key">池标识。</param>
    /// <param name="handle">找到时输出的池句柄。</param>
    /// <returns>找到返回 true，否则返回 false。</returns>
    private bool TryGetPool(string key, out GameObjectPoolHandle handle)
    {
        if (string.IsNullOrEmpty(key))
        {
            handle = null;
            return false;
        }

        return pools.TryGetValue(key, out handle);
    }

    /// <summary>
    /// 根据实例反查其所属对象池句柄。
    /// </summary>
    /// <param name="instance">待查找的 GameObject。</param>
    /// <param name="handle">找到时输出的池句柄。</param>
    /// <returns>找到返回 true，否则返回 false。</returns>
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

    /// <summary>
    /// GameObject 对象池的运行时句柄，关联配置条目与底层池实例。
    /// </summary>
    private sealed class GameObjectPoolHandle
    {
        /// <summary>
        /// 创建池句柄。
        /// </summary>
        /// <param name="entry">池配置条目。</param>
        /// <param name="pool">底层 Transform 对象池。</param>
        public GameObjectPoolHandle(PoolEntry entry, ObjectPool<Transform> pool)
        {
            Entry = entry;
            Pool = pool;
        }

        /// <summary>池配置条目。</summary>
        public PoolEntry Entry { get; }

        /// <summary>底层对象池实例。</summary>
        public ObjectPool<Transform> Pool { get; }
    }
}
