using System;
using UnityEngine;

/// <summary>
/// 单个对象池条目：Prefab、预热数量、容量与扩容策略。
/// </summary>
[Serializable]
public class PoolEntry
{
    [SerializeField] private string key = GameConstants.PoolKeys.Bullet;
    [SerializeField] private GameObject prefab;
    [SerializeField] private int initialCount = 8;
    [SerializeField] private int maxCount = 32;
    [SerializeField] private bool canExpand = true;
    [SerializeField] private PoolOverflowPolicy overflowPolicy = PoolOverflowPolicy.ExpandOrReject;
    [SerializeField] private Transform parent;

    /// <summary>对象池唯一标识，与 <see cref="GameConstants.PoolKeys"/> 一致。</summary>
    public string Key => key;

    /// <summary>池内实例的 Prefab 模板。</summary>
    public GameObject Prefab => prefab;

    /// <summary>初始化时预热的实例数量（不小于 0）。</summary>
    public int InitialCount => Mathf.Max(0, initialCount);

    /// <summary>池内允许存在的最大实例数（不小于 1）。</summary>
    public int MaxCount => Mathf.Max(1, maxCount);

    /// <summary>达到上限后是否允许扩容。</summary>
    public bool CanExpand => canExpand;

    /// <summary>池满时的溢出处理策略。</summary>
    public PoolOverflowPolicy OverflowPolicy => overflowPolicy;

    /// <summary>实例生成与回收时的默认父节点。</summary>
    public Transform Parent => parent;

    /// <summary>Key 与 Prefab 均已正确配置时为 true。</summary>
    public bool IsValid => !string.IsNullOrEmpty(key) && prefab != null;
}
