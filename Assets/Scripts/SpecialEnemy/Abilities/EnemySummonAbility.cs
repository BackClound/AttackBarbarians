using UnityEngine;

/// <summary>
/// 召唤能力：在周围生成额外小怪。
/// </summary>
/// <remarks>由 <see cref="SpecialEnemyAbilityFactory"/> 在生成时挂载，通常无需手动拖到 Prefab。</remarks>
[DisallowMultipleComponent]
public class EnemySummonAbility : EnemyAbilityBase
{
    /// <inheritdoc />
    public override EnemyAbilityTag Tag => EnemyAbilityTag.Summon;

    /// <inheritdoc />
    protected override bool TryExecute()
    {
        if (Config == null || string.IsNullOrEmpty(Config.SummonEnemyConfigId))
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out EnemySpawnerManager spawner))
        {
            return false;
        }

        int count = Config.SummonCount;
        for (int i = 0; i < count; i++)
        {
            spawner.TrySpawnEnemy(
                Config.SummonEnemyConfigId,
                SpecialEnemySpawnContext.StatMultiplier,
                SpecialEnemySpawnContext.WaveIndex);
        }

        return true;
    }
}
