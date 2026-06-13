using UnityEngine;

/// <summary>
/// 远程能力：在攻击距离外对墙体造成额外伤害（多段）。
/// </summary>
/// <remarks>由 <see cref="SpecialEnemyAbilityFactory"/> 在生成时挂载，通常无需手动拖到 Prefab。</remarks>
[DisallowMultipleComponent]
public class EnemyRangedAbility : EnemyAbilityBase
{
    /// <inheritdoc />
    public override EnemyAbilityTag Tag => EnemyAbilityTag.Ranged;

    /// <inheritdoc />
    protected override bool CanExecute()
    {
        if (Owner == null)
        {
            return false;
        }

        return !Owner.IsWallInAttackRange();
    }

    /// <inheritdoc />
    /// <summary>对墙体执行多段远程攻击。</summary>
    /// <returns>执行成功时为 <c>true</c>。</returns>
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

    /// <summary>对墙体施加额外伤害。</summary>
    /// <param name="damage">基础伤害数值。</param>
    /// <summary>对墙体施加额外 bonus 伤害。</summary>
    /// <param name="damage">基础伤害量。</param>
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
