using UnityEngine;

public class Player_Health : Entity_Health
{
    private float currentHp;
    private bool isDead;

    private void Start()
    {
        currentHp = entity_Stats.GetMaxHp();
    }

    public override bool CanBeDamage()
    {
        return currentHp > 0 && !isDead;
    }

    public override void ReduceHp(float damage)
    {
        currentHp -= damage;
        if (currentHp < 0)
        {
            isDead = true;
        }
    }

    public override void RaiseHp(float healing)
    {
        var newHp = currentHp + healing;
        currentHp = Mathf.Min(newHp, entity_Stats.GetMaxHp());
    }

    public override void Die()
    {
        if (isDead)
        {
            //TODO change to dead state
            // Destroy(gameObject);
        }
    }

}
