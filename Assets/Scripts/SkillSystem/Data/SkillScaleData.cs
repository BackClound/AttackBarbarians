using System;
using UnityEngine;

/// <summary>
/// 技能等级成长缩放系数（Legacy）；用于 <see cref="SkillLevelData"/> 按等级调整冷却、伤害等。
/// </summary>
[Serializable]
public class SkillScaleData
{
    /// <summary>冷却缩减倍率（0~0.5）。</summary>
    [Range(0, 0.5f)]
    public float coolDownScaleMulti;
    /// <summary>攻击速度缩放倍率。</summary>
    public float attackSpeedScaleMulti;
    /// <summary>伤害缩放倍率。</summary>
    public float damageScaleMulti;
    /// <summary>暴击几率缩放倍率。</summary>
    public float critChanceScaleMulti;
    /// <summary>暴击伤害缩放倍率。</summary>
    public float critDamageScaleMulti;
    /// <summary>攻击范围缩放倍率。</summary>
    public float attackRadiusScaleMulti;

}
