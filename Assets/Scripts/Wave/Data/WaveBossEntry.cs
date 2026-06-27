using System;
using UnityEngine;

/// <summary>
/// Boss 池单条：configId 与随机权重。
/// </summary>
[Serializable]
public class WaveBossEntry
{
    [SerializeField] private string bossConfigId;
    [SerializeField] private int weight = 1;

    /// <summary>Boss 配置 Id。</summary>
    public string BossConfigId => bossConfigId;

    /// <summary>随机权重。</summary>
    public int Weight => Mathf.Max(1, weight);
}
