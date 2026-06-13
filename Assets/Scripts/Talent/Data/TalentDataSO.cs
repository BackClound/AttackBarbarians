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

    /// <summary>天赋最大等级。</summary>
    public int MaxLevel => Mathf.Max(1, maxLevel);
    /// <summary>1 级升级基础金币消耗。</summary>
    public long BaseUpgradeCostGold => (long)Mathf.Max(0f, baseUpgradeCostGold);
    /// <summary>每级升级消耗的增长倍率。</summary>
    public float CostGrowthPerLevel => Mathf.Max(1f, costGrowthPerLevel);
    /// <summary>前置天赋配置 ID 列表。</summary>
    public IReadOnlyList<string> PrerequisiteTalentIds => prerequisiteTalentIds;
    /// <summary>前置天赋所需的最低等级。</summary>
    public int PrerequisiteMinLevel => Mathf.Max(1, prerequisiteMinLevel);
    /// <summary>每级天赋提供的属性修正列表。</summary>
    public IReadOnlyList<StatModifierConfig> ModifiersPerLevel => modifiersPerLevel;
    /// <summary>天赋描述文本。</summary>
    public string Description => description;

    /// <summary>
    /// 计算从 1 级升到目标等级的单次升级金币消耗。
    /// </summary>
    /// <param name="targetLevel">目标等级（1～MaxLevel）。</param>
    /// <returns>升到该等级所需的金币数量。</returns>
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

    /// <summary>
    /// 收集天赋配置校验错误与警告。
    /// </summary>
    /// <param name="result">校验结果收集器。</param>
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
