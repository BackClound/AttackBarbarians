using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备配置：部位、品质、主属性、词条、套装与强化参数。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Equipment/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "EquipmentData", menuName = "Attack Barbarians/Config/Equipment Data")]
public class EquipmentDataSO : ConfigDataBase
{
    [Header("Equipment")]
    [SerializeField] private EquipmentSlot slot = EquipmentSlot.Weapon;
    [SerializeField] private EquipmentQuality quality = EquipmentQuality.Common;

    [Header("Stats")]
    [SerializeField] private List<StatModifierConfig> mainModifiers = new List<StatModifierConfig>(4);
    [SerializeField] private List<StatModifierConfig> affixModifiers = new List<StatModifierConfig>(2);

    [Header("Set Bonus")]
    [SerializeField] private string setId;
    [SerializeField] private int setPiecesRequired = 2;
    [SerializeField] private List<StatModifierConfig> setBonusModifiers = new List<StatModifierConfig>(2);

    [Header("Enhance")]
    [SerializeField] private int maxEnhanceLevel = 10;
    [SerializeField] private long baseEnhanceCostGold = 50;
    [SerializeField] private float enhanceCostGrowthPerLevel = 1.15f;
    [SerializeField] private float enhanceStatBonusPerLevel = 0.1f;

    [Header("Presentation")]
    [SerializeField] private string description;

    /// <summary>装备部位。</summary>
    public EquipmentSlot Slot => slot;
    /// <summary>装备品质。</summary>
    public EquipmentQuality Quality => quality;
    /// <summary>主属性修正列表。</summary>
    public IReadOnlyList<StatModifierConfig> MainModifiers => mainModifiers;
    /// <summary>词条属性修正列表。</summary>
    public IReadOnlyList<StatModifierConfig> AffixModifiers => affixModifiers;
    /// <summary>套装 ID。</summary>
    public string SetId => setId;
    /// <summary>激活套装加成所需的件数。</summary>
    public int SetPiecesRequired => Mathf.Max(1, setPiecesRequired);
    /// <summary>套装加成属性修正列表。</summary>
    public IReadOnlyList<StatModifierConfig> SetBonusModifiers => setBonusModifiers;
    /// <summary>最大强化等级。</summary>
    public int MaxEnhanceLevel => Mathf.Max(0, maxEnhanceLevel);
    /// <summary>每级强化带来的属性加成比例。</summary>
    public float EnhanceStatBonusPerLevel => Mathf.Max(0f, enhanceStatBonusPerLevel);
    /// <summary>装备描述文本。</summary>
    public string Description => description;

    /// <summary>计算达到指定强化等级所需的金币消耗。</summary>
    /// <param name="targetLevel">目标强化等级。</param>
    /// <returns>所需金币数量。</returns>
    public long GetEnhanceCostForLevel(int targetLevel)
    {
        int clamped = Mathf.Clamp(targetLevel, 1, MaxEnhanceLevel);
        if (clamped <= 1)
        {
            return (long)Mathf.Max(0f, baseEnhanceCostGold * GetQualityCostMultiplier());
        }

        long cost = (long)Mathf.Max(0f, baseEnhanceCostGold * GetQualityCostMultiplier());
        for (int level = 2; level <= clamped; level++)
        {
            cost = (long)(cost * enhanceCostGrowthPerLevel);
        }

        return System.Math.Max(0L, cost);
    }

    /// <summary>获取指定强化等级下的属性倍率。</summary>
    /// <param name="enhanceLevel">强化等级。</param>
    /// <returns>属性倍率（1 表示无加成）。</returns>
    public float GetEnhanceStatMultiplier(int enhanceLevel)
    {
        int clamped = Mathf.Clamp(enhanceLevel, 0, MaxEnhanceLevel);
        return 1f + EnhanceStatBonusPerLevel * clamped;
    }

    /// <summary>根据品质获取强化消耗倍率。</summary>
    /// <returns>消耗倍率。</returns>
    private float GetQualityCostMultiplier()
    {
        return quality switch
        {
            EquipmentQuality.Uncommon => 1.1f,
            EquipmentQuality.Rare => 1.25f,
            EquipmentQuality.Epic => 1.5f,
            EquipmentQuality.Legendary => 2f,
            _ => 1f,
        };
    }

    /// <summary>收集装备配置校验错误与警告。</summary>
    /// <param name="result">校验结果收集器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (slot == EquipmentSlot.None)
        {
            result.AddError(name, "slot 不能为 None。");
        }

        if (mainModifiers == null || mainModifiers.Count == 0)
        {
            result.AddWarning(name, "未配置 mainModifiers，穿戴后无属性加成。");
        }

        if (!string.IsNullOrWhiteSpace(setId) &&
            (setBonusModifiers == null || setBonusModifiers.Count == 0))
        {
            result.AddWarning(name, "配置了 setId 但未配置 setBonusModifiers。");
        }
    }
}
