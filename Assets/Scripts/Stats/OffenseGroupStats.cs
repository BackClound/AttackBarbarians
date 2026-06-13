using System;
using UnityEngine;

/// <summary>
/// 进攻属性分组：伤害、暴击与元素攻击相关属性。
/// </summary>
[Serializable]
public class OffenseGroupStats
{
    /// <summary>基本攻击伤害。</summary>
    public Stat damage;
    /// <summary>暴击倍率。</summary>
    public Stat critPower;
    /// <summary>暴击概率。</summary>
    public Stat critChance;
    /// <summary>攻击速度增幅百分比。</summary>
    public Stat attackSpeedMulti;
    /// <summary>火焰附加伤害。</summary>
    public Stat fireDamage;
    /// <summary>冰霜附加伤害。</summary>
    public Stat iceDamage;
    /// <summary>闪电附加伤害。</summary>
    public Stat lightingDamage;
}
