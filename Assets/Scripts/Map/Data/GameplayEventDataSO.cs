using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局内随机事件配置：触发条件、持续时间与效果列表。
/// </summary>
[CreateAssetMenu(fileName = "GameplayEventData", menuName = "Attack Barbarians/Config/Gameplay Event Data")]
public class GameplayEventDataSO : ConfigDataBase
{
    [Header("Trigger")]
    [SerializeField] private GameplayEventTriggerConfig trigger;

    [Header("Duration")]
    [SerializeField] private float durationSeconds = 10f;
    [SerializeField] private bool endsOnWaveComplete;

    [Header("Effects")]
    [SerializeField] private List<GameplayEventEffectConfig> effects = new List<GameplayEventEffectConfig>();

    public GameplayEventTriggerConfig Trigger => trigger;
    public float DurationSeconds => Mathf.Max(0f, durationSeconds);
    public bool EndsOnWaveComplete => endsOnWaveComplete;
    public IReadOnlyList<GameplayEventEffectConfig> Effects => effects;

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (effects == null || effects.Count == 0)
        {
            result.AddWarning(name, "未配置任何事件效果。");
        }

        if (trigger.TriggerChance <= 0f)
        {
            result.AddWarning(name, "triggerChance 为 0，事件永远不会触发。");
        }
    }
}
