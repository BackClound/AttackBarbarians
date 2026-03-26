using System;
using UnityEngine;

[Serializable]
public class OffenseGroupStats
{
    public Stat damage; //基本攻击伤害
    public Stat critPower; //爆击倍率
    public Stat critChance; //爆击概率
    public Stat attackSpeedMulti; //攻击速度增幅百分比
    public Stat fireDamage;
    public Stat iceDamage;
    public Stat lightingDamage;
}
