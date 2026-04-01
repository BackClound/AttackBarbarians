using UnityEngine;

/// <summary>
///包含Player和Enemy的基础攻击数据和共有数据
/// </summary>
public class Entity_Stats : MonoBehaviour
{
    [Header("基本数据")]
    public MajorGroupStats majorStats;

    [Header("攻击属性")]
    public OffenseGroupStats offenseStats;

    [Header("抗性")]
    public DefenseGroupStats defenseStats;

    public float GetMaxHp()
    {
        //TODO 是否需要添加其他的增加生命值的项
        return majorStats.maxHp.GetValue();
    }

    public float GetTotalDamage()
    {
        var isCrit = offenseStats.critChance.GetValue() > Random.Range(0, 1);
        var baseDamage = offenseStats.damage.GetValue() * (isCrit ? offenseStats.critPower.GetValue() + 1 : 1);
        return baseDamage + offenseStats.iceDamage.GetValue() + offenseStats.fireDamage.GetValue() + offenseStats.lightingDamage.GetValue();
    }

    public float GetMoveSpeed()
    {
        return majorStats.moveSpeed.GetValue();
    }

    public float GetAttackSpeedMultiplier()
    {
        return majorStats.attackSpeedMulti.GetValue();
    }

    public float GetArmorDefense()
    {
        return defenseStats.armor.GetValue();
    }

    public virtual bool CanBeDamage()
    {
        return false;
    }

    public virtual void ReduceHp(float damage) { }

    public virtual void RaiseHp(float healing) { }

    public virtual void Die() { }

}
