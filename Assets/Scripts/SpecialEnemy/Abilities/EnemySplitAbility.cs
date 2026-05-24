using UnityEngine;

/// <summary>分裂：死亡时额外生成小怪（在 <see cref="OnDeath"/> 触发）。</summary>
[DisallowMultipleComponent]
public class EnemySplitAbility : EnemyAbilityBase
{
    private bool splitTriggered;

    public override EnemyAbilityTag Tag => EnemyAbilityTag.Split;

    protected override void OnAbilitySpawn()
    {
        splitTriggered = false;
    }

    protected override bool TryExecute() => false;

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
