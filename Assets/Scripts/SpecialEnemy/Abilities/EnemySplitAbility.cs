using UnityEngine;

/// <summary>
/// 分裂能力：死亡时额外生成小怪（在 <see cref="OnDeath"/> 触发）。
/// </summary>
/// <remarks>由 <see cref="SpecialEnemyAbilityFactory"/> 在生成时挂载，通常无需手动拖到 Prefab。</remarks>
[DisallowMultipleComponent]
public class EnemySplitAbility : EnemyAbilityBase
{
    private bool splitTriggered;

    /// <inheritdoc />
    public override EnemyAbilityTag Tag => EnemyAbilityTag.Split;

    /// <inheritdoc />
    protected override void OnAbilitySpawn()
    {
        splitTriggered = false;
    }

    /// <inheritdoc />
    protected override bool TryExecute() => false;

    /// <inheritdoc />
    protected override void OnAbilityDeath(EnemyController controller)
    {
        if (splitTriggered || Config == null || string.IsNullOrEmpty(Config.SummonEnemyConfigId))
        {
            return;
        }

        if (!ServiceLocator.TryGet(out EnemySpawnerManager spawner))
        {
            return;
        }

        splitTriggered = true;
        int count = Config.SplitCount;
        for (int i = 0; i < count; i++)
        {
            spawner.TrySpawnEnemy(
                Config.SummonEnemyConfigId,
                SpecialEnemySpawnContext.StatMultiplier * 0.75f,
                SpecialEnemySpawnContext.WaveIndex);
        }
    }
}
