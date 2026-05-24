using UnityEngine;

/// <summary>召唤：在周围生成额外小怪。</summary>
[DisallowMultipleComponent]
public class EnemySummonAbility : EnemyAbilityBase
{
    public override EnemyAbilityTag Tag => EnemyAbilityTag.Summon;

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
