using UnityEngine;

/// <summary>
/// 局外升级卡配置：技能卡、属性卡及通用卡。
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

    public UpgradeCardCategory Category => category;
    public string Description => description;
    public UpgradeRarity Rarity => rarity;
    public int LevelsPerCard => Mathf.Max(1, levelsPerCard);
    public string SkillConfigId => skillConfigId;
    public StatType TargetStat => targetStat;

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
