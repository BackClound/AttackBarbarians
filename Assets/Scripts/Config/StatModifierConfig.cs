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

    public StatModifierConfig() { }

    public StatModifierConfig(StatType statType, ConfigModifierType modifierType, float value, int order = 0)
    {
        this.statType = statType;
        this.modifierType = modifierType;
        this.value = value;
        this.order = order;
    }

    /// <summary>将修正应用到当前累计值（按 ModifierType 语义）。</summary>
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
