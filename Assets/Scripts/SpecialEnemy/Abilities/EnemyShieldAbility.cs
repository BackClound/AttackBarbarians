using UnityEngine;

/// <summary>护盾：激活可吸收伤害的护盾层。</summary>
[DisallowMultipleComponent]
public class EnemyShieldAbility : EnemyAbilityBase
{
    private EnemyDamageShield damageShield;

    public override EnemyAbilityTag Tag => EnemyAbilityTag.Shield;

    protected override void OnAbilitySpawn()
    {
        damageShield = EnemyRef != null ? EnemyRef.GetComponent<EnemyDamageShield>() : null;
        if (damageShield == null && EnemyRef != null)
        {
            damageShield = EnemyRef.gameObject.AddComponent<EnemyDamageShield>();
        }
    }

    protected override bool TryExecute()
    {
        if (damageShield == null || Config == null)
        {
            return false;
        }

        if (damageShield.IsActive)
        {
            return false;
        }

        damageShield.Activate(Config.ShieldHp);
        return true;
    }

    protected override void OnAbilityDeath(EnemyController controller)
    {
        damageShield?.Clear();
    }
}
