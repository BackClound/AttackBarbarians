using UnityEngine;

/// <summary>
/// 护盾能力：激活可吸收伤害的护盾层。
/// </summary>
/// <remarks>由 <see cref="SpecialEnemyAbilityFactory"/> 在生成时挂载，通常无需手动拖到 Prefab。</remarks>
[DisallowMultipleComponent]
public class EnemyShieldAbility : EnemyAbilityBase
{
    private EnemyDamageShield damageShield;

    /// <inheritdoc />
    public override EnemyAbilityTag Tag => EnemyAbilityTag.Shield;

    /// <summary>生成时确保存在 <see cref="EnemyDamageShield"/> 组件。</summary>
    protected override void OnAbilitySpawn()
    {
        damageShield = EnemyRef != null ? EnemyRef.GetComponent<EnemyDamageShield>() : null;
        if (damageShield == null && EnemyRef != null)
        {
            damageShield = EnemyRef.gameObject.AddComponent<EnemyDamageShield>();
        }
    }

    /// <summary>激活护盾层（已有护盾时不重复执行）。</summary>
    /// <returns>激活成功时为 <c>true</c>。</returns>
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

    /// <summary>死亡时清除护盾。</summary>
    /// <param name="controller">所属敌人控制器。</param>
    protected override void OnAbilityDeath(EnemyController controller)
    {
        damageShield?.Clear();
    }
}
