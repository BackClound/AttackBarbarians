using UnityEngine;

/// <summary>冲锋：短暂向下加速冲刺。</summary>
[DisallowMultipleComponent]
public class EnemyChargeAbility : EnemyAbilityBase
{
    public override EnemyAbilityTag Tag => EnemyAbilityTag.Charge;

    protected override bool CanExecute() => Owner != null && Owner.IsWallInAttackRange();

    protected override bool TryExecute()
    {
        if (EnemyRef == null || Config == null)
        {
            return false;
        }

        float burstSpeed = EnemyRef.moveSpeed * Mathf.Max(1f, Config.DamageMultiplier);
        EnemyRef.SetVelocity(Vector2.down * burstSpeed);
        return true;
    }
}
