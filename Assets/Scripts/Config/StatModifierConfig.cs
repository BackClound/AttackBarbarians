using System;
using UnityEngine;

/// <summary>
/// 单条属性修正配置，支持加法、百分比与最终倍率。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（可序列化在 SO 或 Buff 配置中）。</para>
/// </remarks>
[Serializable]
public class StatModifierConfig
{
    [SerializeField] private StatType statType;
    [SerializeField] private ConfigModifierType modifierType = ConfigModifierType.Flat;
    [SerializeField] private float value;
    [SerializeField] private int order;

    public StatType StatType => statType;
    public ConfigModifierType ModifierType => modifierType;
    public float Value => value;
    public int Order => order;

    /// <summary>
    /// 创建空的属性修正配置实例。
    /// </summary>
    public StatModifierConfig() { }

    /// <summary>
    /// 创建指定属性、修正类型与数值的属性修正配置。
    /// </summary>
    /// <param name="statType">目标属性类型。</param>
    /// <param name="modifierType">修正运算类型。</param>
    /// <param name="value">修正数值。</param>
    /// <param name="order">同类型修正的应用顺序，数值越小越先应用。</param>
    public StatModifierConfig(StatType statType, ConfigModifierType modifierType, float value, int order = 0)
    {
        this.statType = statType;
        this.modifierType = modifierType;
        this.value = value;
        this.order = order;
    }

    /// <summary>
    /// 将修正应用到当前累计值（按 <see cref="ModifierType"/> 语义）。
    /// </summary>
    /// <param name="baseValue">属性的原始基础值，用于百分比类修正计算。</param>
    /// <param name="currentValue">已累计修正后的当前值。</param>
    /// <returns>应用本修正后的新数值。</returns>
    public float Apply(float baseValue, float currentValue)
    {
        switch (modifierType)
        {
            case ConfigModifierType.Flat:
                return currentValue + value;
            case ConfigModifierType.PercentAdd:
                return currentValue + baseValue * value;
            case ConfigModifierType.PercentMultiply:
                return currentValue * value;
            case ConfigModifierType.FinalMultiplier:
                return currentValue * value;
            default:
                return currentValue;
        }
    }
}
