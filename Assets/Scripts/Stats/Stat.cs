using System;
using UnityEngine;

/// <summary>
/// 单个可序列化属性值，维护基础值与最终计算值。
/// </summary>
[Serializable]
public class Stat
{
    [SerializeField] private float baseValue;
    [SerializeField] private bool shouldUpdate;

    private float finalValue;

    /// <summary>
    /// 设置属性基础值，并同步更新最终值缓存。
    /// </summary>
    /// <param name="value">新的基础数值。</param>
    public void SetBaseValue(float value)
    {
        baseValue = value;
        finalValue = value;
    }

    /// <summary>
    /// 获取当前最终属性值（会触发一次重算）。
    /// </summary>
    /// <returns>计算后的最终数值。</returns>
    public float GetValue()
    {
        finalValue = GetFinalValue();

        return finalValue;
    }

    /// <summary>
    /// 根据基础值计算最终属性值。
    /// </summary>
    /// <returns>最终数值。</returns>
    private float GetFinalValue()
    {
        finalValue = baseValue;

        return finalValue;
    }
}
