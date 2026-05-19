using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 配置：基于敌人模板叠加阶段与专属修正。
/// </summary>
[CreateAssetMenu(fileName = "BossData", menuName = "Attack Barbarians/Config/Boss Data")]
public class BossDataSO : ConfigDataBase
{
    [Header("Base")]
    [SerializeField] private string baseEnemyConfigId;
    [SerializeField] private StatBlockConfig statOverrides = new StatBlockConfig();
    [SerializeField] private bool useStatOverrides;

    // 阶段
    [Header("Phases")]
    [SerializeField] private int phaseCount = 1;
    [SerializeField] private List<float> phaseHpThresholds = new List<float> { 0.5f };

    [Header("Modifiers")]
    [SerializeField] private List<StatModifierConfig> phaseModifiers = new List<StatModifierConfig>();

    [Header("Rewards")]
    [SerializeField] private string dropTableId;
    [SerializeField] private int bonusExperience = 50;

    public string BaseEnemyConfigId => baseEnemyConfigId;
    public StatBlockConfig StatOverrides => statOverrides;
    public bool UseStatOverrides => useStatOverrides;
    public int PhaseCount => Mathf.Max(1, phaseCount);
    public IReadOnlyList<float> PhaseHpThresholds => phaseHpThresholds;
    public IReadOnlyList<StatModifierConfig> PhaseModifiers => phaseModifiers;
    public string DropTableId => dropTableId;
    public int BonusExperience => Mathf.Max(0, bonusExperience);

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (string.IsNullOrWhiteSpace(baseEnemyConfigId))
        {
            result.AddError(name, "baseEnemyConfigId 不能为空。");
        }

        if (phaseCount < 1)
        {
            result.AddError(name, "phaseCount 不能小于 1。");
        }
    }
}
