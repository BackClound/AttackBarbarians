using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 配置：基于敌人模板叠加阶段、技能与专属数值修正。
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
    // 阶段数量
    [SerializeField] private int phaseCount = 1;
    [SerializeField] private BossPhaseTransitionMode phaseTransitionMode = BossPhaseTransitionMode.HealthRatio;
    // 阶段血量阈值列表
    [SerializeField] private List<float> phaseHpThresholds = new List<float> { 0.5f };
    // 阶段时间阈值列表
    [SerializeField] private List<float> phaseTimeThresholds = new List<float> { 15f, 25f };
    // 阶段属性修正列表
    [SerializeField] private List<BossPhaseModifierEntry> phaseModifierEntries = new List<BossPhaseModifierEntry>();

    // 技能配置列表
    [Header("Skills")]
    [SerializeField] private List<string> skillConfigIds = new List<string>();

    [Header("Rewards")]
    [SerializeField] private string dropTableId;
    [SerializeField] private int bonusExperience = 50;

    public string BaseEnemyConfigId => baseEnemyConfigId;
    public StatBlockConfig StatOverrides => statOverrides;
    public bool UseStatOverrides => useStatOverrides;
    public int PhaseCount => Mathf.Max(1, phaseCount);
    public BossPhaseTransitionMode PhaseTransitionMode => phaseTransitionMode;
    public IReadOnlyList<float> PhaseHpThresholds => phaseHpThresholds;
    public IReadOnlyList<float> PhaseTimeThresholds => phaseTimeThresholds;
    public IReadOnlyList<string> SkillConfigIds => skillConfigIds;
    public string DropTableId => dropTableId;
    public int BonusExperience => Mathf.Max(0, bonusExperience);

    public IReadOnlyList<StatModifierConfig> GetPhaseModifiers(int phaseIndex)
    {
        if (phaseModifierEntries == null || phaseIndex < 0)
        {
            return null;
        }

        for (int i = 0; i < phaseModifierEntries.Count; i++)
        {
            BossPhaseModifierEntry entry = phaseModifierEntries[i];
            if (entry != null && entry.PhaseIndex == phaseIndex)
            {
                return entry.Modifiers;
            }
        }

        return null;
    }

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

/// <summary>
/// Boss 阶段属性修正条目。
/// </summary>
[System.Serializable]
public class BossPhaseModifierEntry
{
    [SerializeField] private int phaseIndex;
    [SerializeField] private List<StatModifierConfig> modifiers = new List<StatModifierConfig>();

    public int PhaseIndex => phaseIndex;
    public IReadOnlyList<StatModifierConfig> Modifiers => modifiers;
}
