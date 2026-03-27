using UnityEngine;

public class Entity_Health : MonoBehaviour
{

    public Entity_Stats entity_Stats;


    public void TakeDamage(float damage)
    {
        ReduceHp(damage);
    }

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
