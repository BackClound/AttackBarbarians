using System;
using UnityEngine;

/// <summary>
/// 主要属性分组：移动、生命与攻击速度相关属性。
/// </summary>
[Serializable]
public class MajorGroupStats
{
    /// <summary>移动速度。</summary>
    public Stat moveSpeed;
    /// <summary>最大生命值。</summary>
    public Stat maxHp;
    /// <summary>攻击速度。</summary>
    public Stat attackSpeed;
    /// <summary>攻击速度倍率。</summary>
    public Stat attackSpeedMulti;
}
