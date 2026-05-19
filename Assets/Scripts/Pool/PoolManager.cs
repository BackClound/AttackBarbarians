using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对象池管理器：按 Key 预热、Spawn、Despawn，供敌人、子弹、飘字等高频对象复用。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。</para>
/// <para><b>推荐挂载对象：</b>在 <c>GameSystems</c> 下创建子物体 <c>PoolRoot</c>，将本组件挂在 <c>PoolRoot</c> 上；各池的 <c>Parent</c> 可指向 <c>PoolRoot</c> 下对应子节点（如 <c>Pool_Enemy</c>、<c>Pool_Bullet</c>）。</para>
/// <para><b>不要挂载到：</b>Player、单个 Enemy Prefab、子弹 Prefab 上。</para>
/// <para><b>Inspector 配置：</b>在 <c>Pools</c> 列表中为每项填写 Key（与 <see cref="GameConstants.PoolKeys"/> 一致）、Prefab、Prewarm Count、可选 Parent。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;PoolManager&gt;()</c>；调用 <c>Spawn(key, position, rotation)</c> / <c>Despawn(instance)</c>。</para>
/// <para><b>注意：</b>未配置 Key 时 Spawn 返回 null，调用方应保留 Instantiate 回退逻辑直至迁移完成。</para>
/// </remarks>
public class PoolManager : MonoBehaviour, IGameSystem
{
    [Serializable]
    private class PoolDefinition
    {
        public string key;
        public GameObject prefab;
        public int prewarmCount = 8;
        public Transform parent;
    }

    [SerializeField] private List<PoolDefinition> pools = new List<PoolDefinition>();

    private readonly Dictionary<string, PoolDefinition> definitions = new Dictionary<string, PoolDefinition>(32);
    private readonly Dictionary<string, Queue<GameObject>> inactiveObjects = new Dictionary<string, Queue<GameObject>>(32);
    private readonly Dictionary<GameObject, string> activeObjectKeys = new Dictionary<GameObject, string>(128);

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        definitions.Clear();
        inactiveObjects.Clear();
        activeObjectKeys.Clear();

        for (int i = 0; i < pools.Count; i++)
        {
            PoolDefinition definition = pools[i];
            if (definition == null || string.IsNullOrEmpty(definition.key) || definition.prefab == null)
            {
                continue;
            }

            definitions[definition.key] = definition;
            inactiveObjects[definition.key] = new Queue<GameObject>(definition.prewarmCount);
            Prewarm(definition);
        }

        IsInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        definitions.Clear();
        inactiveObjects.Clear();
        activeObjectKeys.Clear();
        IsInitialized = false;
    }

    public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!definitions.TryGetValue(key, out PoolDefinition definition))
        {
            return null;
        }

        GameObject instance = GetOrCreateInstance(definition);
        if (instance == null)
        {
            return null;
        }

        Transform instanceTransform = instance.transform;
        instanceTransform.SetParent(parent, false);
        instanceTransform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        activeObjectKeys[instance] = key;
        return instance;
    }

    public T Spawn<T>(string key, Vector3 position, Quaternion rotation, Transform parent = null) where T : Component
    {
        GameObject instance = Spawn(key, position, rotation, parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        if (!activeObjectKeys.TryGetValue(instance, out string key) || !inactiveObjects.TryGetValue(key, out Queue<GameObject> queue))
        {
            Destroy(instance);
            return;
        }

        activeObjectKeys.Remove(instance);
        instance.SetActive(false);

        Transform parent = definitions.TryGetValue(key, out PoolDefinition definition) ? definition.parent : transform;
        instance.transform.SetParent(parent != null ? parent : transform, false);
        queue.Enqueue(instance);
    }

    private void Prewarm(PoolDefinition definition)
    {
        int count = Mathf.Max(0, definition.prewarmCount);
        for (int i = 0; i < count; i++)
        {
            GameObject instance = CreateInstance(definition);
            if (instance != null)
            {
                inactiveObjects[definition.key].Enqueue(instance);
            }
        }
    }

    private GameObject GetOrCreateInstance(PoolDefinition definition)
    {
        Queue<GameObject> queue = inactiveObjects[definition.key];
        while (queue.Count > 0)
        {
            GameObject instance = queue.Dequeue();
            if (instance != null)
            {
                return instance;
            }
        }

        ConfigManager configManager = ServiceLocator.TryGet(out ConfigManager manager) ? manager : null;
        bool canGrow = configManager == null || configManager.GameConfig == null || configManager.GameConfig.AllowPoolGrowth;
        return canGrow ? CreateInstance(definition) : null;
    }

    private GameObject CreateInstance(PoolDefinition definition)
    {
        Transform parent = definition.parent != null ? definition.parent : transform;
        GameObject instance = Instantiate(definition.prefab, parent);
        instance.name = definition.prefab.name;
        instance.SetActive(false);
        return instance;
    }
}
