using UnityEngine;

public class Entity_Health : MonoBehaviour
{

    public Entity_Stats entity_Stats;


    public void TakeDamage(float damage)
    {
        ApplyResolvedDamage(new DamageResult(damage, 0f, false, false), DamageInfo.FromLegacy(damage, gameObject));
    }

    /// <summary>由 <see cref="DamageSystem"/> 或 <see cref="DamagePipeline"/> 在结算后调用。</summary>
    public virtual void ApplyResolvedDamage(DamageResult result, DamageInfo info)
    {
        if (result.FinalDamage <= 0f)
        {
            return;
        }

        OnBeforeDamageApplied(info, result);
        ReduceHp(result.FinalDamage);
    }

    protected virtual void OnBeforeDamageApplied(DamageInfo info, DamageResult result) { }

    public virtual void Awake()
    {
        entity_Stats = GetComponent<Entity_Stats>();
    }

    public virtual bool CanBeDamage()
    {
        return false;
    }

    protected virtual void ReduceHp(float damage)
    {
        // Debug.Log("Entity health reduce HP " + damage);
    }

    public virtual void RaiseHp(float healing) { }

    public virtual void Die() { }
}
