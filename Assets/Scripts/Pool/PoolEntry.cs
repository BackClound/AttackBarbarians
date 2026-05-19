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

    public string Key => key;
    public GameObject Prefab => prefab;
    public int InitialCount => Mathf.Max(0, initialCount);
    public int MaxCount => Mathf.Max(1, maxCount);
    public bool CanExpand => canExpand;
    public PoolOverflowPolicy OverflowPolicy => overflowPolicy;
    public Transform Parent => parent;

    public bool IsValid => !string.IsNullOrEmpty(key) && prefab != null;
}
