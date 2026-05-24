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

    public GameplayEventTriggerType TriggerType => triggerType;
    public int MinWaveIndex => Mathf.Max(1, minWaveIndex);
    public int MaxWaveIndex => Mathf.Max(MinWaveIndex, maxWaveIndex);
    public float TriggerChance => Mathf.Clamp01(triggerChance);
}
