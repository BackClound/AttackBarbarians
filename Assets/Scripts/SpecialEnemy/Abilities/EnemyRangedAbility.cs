using UnityEngine;

/// <summary>远程：在攻击距离外对墙体造成额外伤害（多段）。</summary>
[DisallowMultipleComponent]
public class EnemyRangedAbility : EnemyAbilityBase
{
    public override EnemyAbilityTag Tag => EnemyAbilityTag.Ranged;

    protected override bool CanExecute()
    {
        if (Owner == null)
        {
            return false;
        }

        return !Owner.IsWallInAttackRange();
    }

    protected override bool TryExecute()
    {
        if (Owner == null || Config == null)
        {
            return false;
        }

        float damage = Owner.GetMeleeDamage() * Config.DamageMultiplier;
        for (int i = 0; i < Config.HitCount; i++)
        {
            Owner.ExecuteWallAttack();
            TryBonusWallDamage(damage);
        }

        return true;
    }

    private void TryBonusWallDamage(float damage)
    {
        if (EnemyRef == null || damage <= 0f)
        {
            return;
        }

        if (ServiceLocator.TryGet(out WallControlManager wall))
        {
            wall.TakeDamageFromEnemy(EnemyRef, damage * 0.25f);
        }
    }
}
