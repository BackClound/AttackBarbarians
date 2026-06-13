using System;
using UnityEngine;

/// <summary>
/// 防御属性分组：护甲与破甲相关属性。
/// </summary>
[Serializable]
public class DefenseGroupStats
{
    /// <summary>护甲值。</summary>
    public Stat armor;
    /// <summary>破甲值；普通怪物无护甲，精英怪拥有护甲。</summary>
    public Stat armorReduce;
    //TODO 三种元素攻击抗性，暂不涉及该游戏策略
    // private Stat fireRes;
    // private Stat iceRes;
    // private Stat lightingRes;
}
