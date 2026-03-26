using UnityEngine;

public class Entity_Health : MonoBehaviour, IDamagable
{

    public Entity_Stats entity_Stats;


    public void TakeDamage(Entity_Stats entity_Stats, float damage) { }

    public virtual void Awake()
    {
        entity_Stats = GetComponent<Entity_Stats>();
    }

    public virtual bool CanBeDamage()
    {
        return false;
    }

    public virtual void ReduceHp(float damage) { }

    public virtual void RaiseHp(float healing) { }

    public virtual void Die() { }
}
