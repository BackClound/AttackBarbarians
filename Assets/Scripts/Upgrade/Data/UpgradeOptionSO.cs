using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单条升级选项配置：展示信息、稀有度、权重、前置条件与效果参数。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Upgrade/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "UpgradeOption", menuName = "Attack Barbarians/Config/Upgrade Option")]
public class UpgradeOptionSO : ConfigDataBase
{
    [Header("Presentation")]
    [SerializeField] [TextArea] private string description;
    [SerializeField] private UpgradeRarity rarity = UpgradeRarity.Common;
    [SerializeField] private int weight = 100;

    [Header("Rules")]
    [SerializeField] private UpgradeEffectType effectType = UpgradeEffectType.StatBuff;
    [SerializeField] private int maxStacks = 99;
    [SerializeField] private int minWave = 1;
    [SerializeField] private int maxWave;
    [SerializeField] private string mutuallyExclusiveGroup;
    [SerializeField] private List<string> prerequisiteOptionIds = new List<string>(4);

    [Header("Effect — Buff")]
    [SerializeField] private string buffConfigId;
    [SerializeField] private int buffStacks = 1;

    [Header("Effect — Skill")]
    [SerializeField] private string skillConfigId;
    [SerializeField] private int skillLevelDelta = 1;
    [SerializeField] private SkillBuffKind skillBuffKind;
    [SerializeField] private int skillBuffTier = 1;

    [Header("Effect — Resource")]
    [SerializeField] private long resourceAmount = 10;

    [Header("Effect — Stat (optional direct modifier)")]
    [SerializeField] private List<StatModifierConfig> directModifiers = new List<StatModifierConfig>();

    public string Description => description;
    public UpgradeRarity Rarity => rarity;
    public int Weight => Mathf.Max(1, weight);
    public UpgradeEffectType EffectType => effectType;
    public int MaxStacks => Mathf.Max(1, maxStacks);
    public int MinWave => Mathf.Max(1, minWave);
    public int MaxWave => maxWave;
    public string MutuallyExclusiveGroup => mutuallyExclusiveGroup;
    public IReadOnlyList<string> PrerequisiteOptionIds => prerequisiteOptionIds;
    public string BuffConfigId => buffConfigId;
    public int BuffStacks => Mathf.Max(1, buffStacks);
    public string SkillConfigId => skillConfigId;
    public int SkillLevelDelta => Mathf.Max(1, skillLevelDelta);
    public SkillBuffKind SkillBuffKind => skillBuffKind;
    public int SkillBuffTier => Mathf.Max(1, skillBuffTier);
    public long ResourceAmount => resourceAmount;
    public IReadOnlyList<StatModifierConfig> DirectModifiers => directModifiers;

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (weight < 1)
        {
            result.AddError(name, "weight 不能小于 1。");
        }

        if (maxWave > 0 && maxWave < minWave)
        {
            result.AddError(name, "maxWave 不能小于 minWave。");
        }

        switch (effectType)
        {
            case UpgradeEffectType.StatBuff:
                if (string.IsNullOrWhiteSpace(buffConfigId) && (directModifiers == null || directModifiers.Count == 0))
                {
                    result.AddError(name, "StatBuff 需配置 buffConfigId 或 directModifiers。");
                }

                break;

            case UpgradeEffectType.SkillBuff:
            case UpgradeEffectType.WeaponEnhance:
                if (skillBuffKind == SkillBuffKind.None)
                {
                    result.AddError(name, "SkillBuff / WeaponEnhance 需配置 skillBuffKind。");
                }

                break;

            case UpgradeEffectType.SkillUnlock:
            case UpgradeEffectType.SkillLevelUp:
                if (string.IsNullOrWhiteSpace(skillConfigId))
                {
                    result.AddError(name, "SkillUnlock / SkillLevelUp 需配置 skillConfigId。");
                }

                break;

            case UpgradeEffectType.ResourceGold:
            case UpgradeEffectType.ResourceDiamond:
                if (resourceAmount <= 0)
                {
                    result.AddError(name, "资源奖励 amount 必须大于 0。");
                }

                break;
        }
    }
}
