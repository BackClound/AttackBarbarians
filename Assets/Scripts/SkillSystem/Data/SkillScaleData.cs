using System;
using UnityEngine;

/// <summary>
/// TODO 需要考虑等级会提升哪些属性值，攻击频率，伤害值，爆击概率，爆击值， 攻击范围
/// 护甲值（NO），特殊元素伤害？
/// coolDown - 冷却值随等级上升，冷却变短，减少的最大阈值为50%， 
/// </summary>
[Serializable]
public class SkillScaleData
{
    //0 - 100
    [Range(0, 0.5f)]
    public float coolDownScaleMulti;
    public float attackSpeedScaleMulti;
    public float damageScaleMulti;
    public float critChanceScaleMulti;
    public float critDamageScaleMulti;
    public float attackRadiusScaleMulti;

}
