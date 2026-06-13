using UnityEngine;

/// <summary>
/// 实体生命值组件：接收伤害结算结果并扣减 HP。
/// </summary>
/// <remarks>挂载在 Player/Enemy 等实体上，需同物体存在 <see cref="Entity_Stats"/>。</remarks>
public class Entity_Health : MonoBehaviour
{

    /// <summary>关联的属性组件。</summary>
    public Entity_Stats entity_Stats;


    /// <summary>兼容 float 伤害入口，委托 <see cref="DamagePipeline"/> 结算。</summary>
    /// <param name="damage">伤害数值。</param>
    public void TakeDamage(float damage)
    {
        DamagePipeline.Apply(DamageInfo.FromFloat(damage, gameObject));
    }

    /// <summary>由 <see cref="DamageSystem"/> 或 <see cref="DamagePipeline"/> 在结算后调用。</summary>
    /// <param name="result">最终伤害结果。</param>
    /// <param name="info">原始伤害上下文。</param>
    public virtual void ApplyResolvedDamage(DamageResult result, DamageInfo info)
    {
        if (result.FinalDamage <= 0f)
        {
            return;
        }

        OnBeforeDamageApplied(info, result);
        ReduceHp(result.FinalDamage);
    }

    /// <summary>扣血前的钩子，子类可覆写以处理无敌、护盾等。</summary>
    /// <param name="info">伤害上下文。</param>
    /// <param name="result">结算结果。</param>
    protected virtual void OnBeforeDamageApplied(DamageInfo info, DamageResult result) { }

    /// <summary>缓存 <see cref="Entity_Stats"/> 引用。</summary>
    public virtual void Awake()
    {
        entity_Stats = GetComponent<Entity_Stats>();
    }

    /// <summary>当前是否可被伤害，子类覆写以反映无敌等状态。</summary>
    /// <returns>可受伤时返回 true。</returns>
    public virtual bool CanBeDamage()
    {
        return false;
    }

    /// <summary>扣减生命值，子类实现具体 HP 变更与死亡逻辑。</summary>
    /// <param name="damage">最终伤害量。</param>
    protected virtual void ReduceHp(float damage)
    {
        // Debug.Log("Entity health reduce HP " + damage);
    }

    /// <summary>恢复生命值，子类实现具体逻辑。</summary>
    /// <param name="healing">治疗量。</param>
    public virtual void RaiseHp(float healing) { }

    /// <summary>生命值归零时的死亡处理，子类实现。</summary>
    public virtual void Die() { }
}
