using UnityEngine;

/// <summary>
/// 局内随机事件触发条件。
/// </summary>
[System.Serializable]
public struct GameplayEventTriggerConfig
{
    [SerializeField] private GameplayEventTriggerType triggerType;
    [SerializeField] private int minWaveIndex;
    [SerializeField] private int maxWaveIndex;
    [Range(0f, 1f)]
    [SerializeField] private float triggerChance;

    /// <summary>触发时机类型。</summary>
    public GameplayEventTriggerType TriggerType => triggerType;
    /// <summary>可触发的最小波次序号（含）。</summary>
    public int MinWaveIndex => Mathf.Max(1, minWaveIndex);
    /// <summary>可触发的最大波次序号（含）。</summary>
    public int MaxWaveIndex => Mathf.Max(MinWaveIndex, maxWaveIndex);
    /// <summary>触发概率（0~1）。</summary>
    public float TriggerChance => Mathf.Clamp01(triggerChance);
}
