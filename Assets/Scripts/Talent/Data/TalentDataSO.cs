using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 天赋配置：等级上限、消耗、前置条件与每级属性修正。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Talent/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "TalentData", menuName = "Attack Barbarians/Config/Talent Data")]
public class TalentDataSO : ConfigDataBase
{
    [Header("Talent")]
    [SerializeField] private int maxLevel = 10;
    [SerializeField] private long baseUpgradeCostGold = 100;
    [SerializeField] private float costGrowthPerLevel = 1.25f;
    [SerializeField] private List<string> prerequisiteTalentIds = new List<string>(2);
    [SerializeField] private int prerequisiteMinLevel = 1;

    [Header("Modifiers (per talent level)")]
    [SerializeField] private List<StatModifierConfig> modifiersPerLevel = new List<StatModifierConfig>(4);

    [Header("Presentation")]
    [SerializeField] private string description;

    public int MaxLevel => Mathf.Max(1, maxLevel);
    public long BaseUpgradeCostGold => (long)Mathf.Max(0f, baseUpgradeCostGold);
    public float CostGrowthPerLevel => Mathf.Max(1f, costGrowthPerLevel);
    public IReadOnlyList<string> PrerequisiteTalentIds => prerequisiteTalentIds;
    public int PrerequisiteMinLevel => Mathf.Max(1, prerequisiteMinLevel);
    public IReadOnlyList<StatModifierConfig> ModifiersPerLevel => modifiersPerLevel;
    public string Description => description;

    /// <summary>从 1 级升到 targetLevel 的累计金币消耗（不含已付等级）。</summary>
    public long GetUpgradeCostForLevel(int targetLevel)
    {
        int clamped = Mathf.Clamp(targetLevel, 1, MaxLevel);
        if (clamped <= 1)
        {
            return BaseUpgradeCostGold;
        }

        long cost = BaseUpgradeCostGold;
        for (int level = 2; level <= clamped; level++)
        {
            cost = (long)(cost * CostGrowthPerLevel);
        }

        return System.Math.Max(0L, cost);
    }

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (maxLevel < 1)
        {
            result.AddError(name, "maxLevel 不能小于 1。");
        }

        if (modifiersPerLevel == null || modifiersPerLevel.Count == 0)
        {
            result.AddWarning(name, "未配置 modifiersPerLevel，升级后无属性加成。");
        }
    }
}
