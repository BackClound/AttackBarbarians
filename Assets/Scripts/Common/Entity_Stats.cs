using UnityEngine;

/// <summary>
/// Player 与 Enemy 共用的属性容器：主属性、攻击属性与防御属性组。
/// </summary>
/// <remarks>挂载在实体根节点，供 <see cref="Entity_Health"/> 与战斗系统读取数值。</remarks>
public class Entity_Stats : MonoBehaviour
{
    [Header("基本数据")]
    /// <summary>主属性组（生命、移速、攻速倍率等）。</summary>
    public MajorGroupStats majorStats;

    [Header("攻击属性")]
    /// <summary>攻击属性组（伤害、暴击、元素附加等）。</summary>
    public OffenseGroupStats offenseStats;

    [Header("抗性")]
    /// <summary>防御属性组（护甲等）。</summary>
    public DefenseGroupStats defenseStats;

    /// <summary>获取最大生命值。</summary>
    /// <returns>当前最大 HP。</returns>
    public float GetMaxHp()
    {
        //TODO 是否需要添加其他的增加生命值的项
        return majorStats.maxHp.GetValue();
    }

    /// <summary>基础攻击伤害（不含暴击与元素附加），供 <see cref="DamageSystem"/> 结算。</summary>
    /// <returns>基础伤害值。</returns>
    public float GetBaseAttackDamage()
    {
        return offenseStats != null && offenseStats.damage != null
            ? offenseStats.damage.GetValue()
            : 0f;
    }

    /// <summary>旧版一次性结算（含暴击与元素）。新逻辑请走 <see cref="DamageSystem"/>。</summary>
    /// <returns>随机暴击后的总伤害。</returns>
    public float GetTotalDamage()
    {
        var isCrit = offenseStats.critChance.GetValue() > Random.Range(0, 1);
        var baseDamage = offenseStats.damage.GetValue() * (isCrit ? offenseStats.critPower.GetValue() + 1 : 1);
        return baseDamage + offenseStats.iceDamage.GetValue() + offenseStats.fireDamage.GetValue() + offenseStats.lightingDamage.GetValue();
    }

    /// <summary>获取移动速度。</summary>
    /// <returns>当前移速。</returns>
    public float GetMoveSpeed()
    {
        return majorStats.moveSpeed.GetValue();
    }

    /// <summary>获取攻击速度倍率。</summary>
    /// <returns>攻速倍率。</returns>
    public float GetAttackSpeedMultiplier()
    {
        return majorStats.attackSpeedMulti.GetValue();
    }

    /// <summary>获取护甲防御值。</summary>
    /// <returns>护甲数值。</returns>
    public float GetArmorDefense()
    {
        return defenseStats.armor.GetValue();
    }

    /// <summary>当前是否可被伤害，子类覆写。</summary>
    /// <returns>可受伤时返回 true。</returns>
    public virtual bool CanBeDamage()
    {
        return false;
    }

    /// <summary>扣减生命值，子类实现。</summary>
    /// <param name="damage">伤害量。</param>
    public virtual void ReduceHp(float damage) { }

    /// <summary>恢复生命值，子类实现。</summary>
    /// <param name="healing">治疗量。</param>
    public virtual void RaiseHp(float healing) { }

    /// <summary>死亡处理，子类实现。</summary>
    public virtual void Die() { }

}
