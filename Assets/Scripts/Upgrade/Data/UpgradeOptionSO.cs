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

    /// <summary>选项描述文本。</summary>
    public string Description => description;
    /// <summary>稀有度。</summary>
    public UpgradeRarity Rarity => rarity;
    /// <summary>默认抽取权重。</summary>
    public int Weight => Mathf.Max(1, weight);
    /// <summary>效果类型。</summary>
    public UpgradeEffectType EffectType => effectType;
    /// <summary>最大可叠加层数。</summary>
    public int MaxStacks => Mathf.Max(1, maxStacks);
    /// <summary>生效的最小波次。</summary>
    public int MinWave => Mathf.Max(1, minWave);
    /// <summary>生效的最大波次（0 表示无上限）。</summary>
    public int MaxWave => maxWave;
    /// <summary>互斥组 ID（同组仅可选一次）。</summary>
    public string MutuallyExclusiveGroup => mutuallyExclusiveGroup;
    /// <summary>前置升级选项 ID 列表。</summary>
    public IReadOnlyList<string> PrerequisiteOptionIds => prerequisiteOptionIds;
    /// <summary>Buff 配置 ID。</summary>
    public string BuffConfigId => buffConfigId;
    /// <summary>Buff 叠加层数。</summary>
    public int BuffStacks => Mathf.Max(1, buffStacks);
    /// <summary>目标技能配置 ID。</summary>
    public string SkillConfigId => skillConfigId;
    /// <summary>技能等级提升量。</summary>
    public int SkillLevelDelta => Mathf.Max(1, skillLevelDelta);
    /// <summary>技能 Buff 种类。</summary>
    public SkillBuffKind SkillBuffKind => skillBuffKind;
    /// <summary>技能 Buff 层级。</summary>
    public int SkillBuffTier => Mathf.Max(1, skillBuffTier);
    /// <summary>资源奖励数量。</summary>
    public long ResourceAmount => resourceAmount;
    /// <summary>直接属性修正列表（可选）。</summary>
    public IReadOnlyList<StatModifierConfig> DirectModifiers => directModifiers;

    /// <summary>收集升级选项配置校验错误。</summary>
    /// <param name="result">校验结果收集器。</param>
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
