using UnityEngine;

/// <summary>
/// 冲锋能力：短暂向下加速冲刺。
/// </summary>
/// <remarks>由 <see cref="SpecialEnemyAbilityFactory"/> 在生成时挂载，通常无需手动拖到 Prefab。</remarks>
[DisallowMultipleComponent]
public class EnemyChargeAbility : EnemyAbilityBase
{
    /// <inheritdoc />
    public override EnemyAbilityTag Tag => EnemyAbilityTag.Charge;

    /// <inheritdoc />
    protected override bool CanExecute() => Owner != null && Owner.IsWallInAttackRange();

    /// <inheritdoc />
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
