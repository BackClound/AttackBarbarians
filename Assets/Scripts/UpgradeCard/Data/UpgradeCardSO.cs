using UnityEngine;

/// <summary>
/// Meta 升级卡配置资产：定义技能卡、属性卡及通用卡的种类、稀有度与目标。
/// </summary>
/// <remarks>
/// <para><b>路径：</b><c>Assets/Resources/Config/UpgradeCard/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "UpgradeCard", menuName = "Attack Barbarians/Config/Upgrade Card")]
public class UpgradeCardSO : ConfigDataBase
{
    [Header("Card")]
    [SerializeField] private UpgradeCardCategory category = UpgradeCardCategory.Skill;
    [SerializeField] [TextArea] private string description;
    [SerializeField] private UpgradeRarity rarity = UpgradeRarity.Common;
    [SerializeField] private int levelsPerCard = 1;

    [Header("Target — Skill")]
    [SerializeField] private string skillConfigId;

    [Header("Target — Attribute")]
    [SerializeField] private StatType targetStat = StatType.None;

    /// <summary>卡片种类。</summary>
    public UpgradeCardCategory Category => category;
    /// <summary>卡片描述文本。</summary>
    public string Description => description;
    /// <summary>稀有度。</summary>
    public UpgradeRarity Rarity => rarity;
    /// <summary>每张卡提供的升级等级数。</summary>
    public int LevelsPerCard => Mathf.Max(1, levelsPerCard);
    /// <summary>目标技能配置 ID（技能卡专用）。</summary>
    public string SkillConfigId => skillConfigId;
    /// <summary>目标属性类型（属性卡专用）。</summary>
    public StatType TargetStat => targetStat;

    /// <summary>收集升级卡配置校验错误。</summary>
    /// <param name="result">校验结果收集器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        switch (category)
        {
            case UpgradeCardCategory.Skill:
                if (string.IsNullOrWhiteSpace(skillConfigId))
                {
                    result.AddError(name, "技能升级卡需配置 skillConfigId。");
                }

                break;

            case UpgradeCardCategory.Attribute:
                if (targetStat == StatType.None)
                {
                    result.AddError(name, "属性升级卡需配置 targetStat。");
                }

                break;
        }
    }
}
