using System;
using UnityEngine;

/// <summary>
/// 攻击信息数据：关联属性类型与对应数值，用于 VFX 或伤害展示。
/// </summary>
[Serializable]
public class AttackInfo
{
    /// <summary>关联的属性类型。</summary>
    public StatType statType;
    /// <summary>属性对应的数值。</summary>
    public float value;
}
