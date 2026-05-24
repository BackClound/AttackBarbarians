using UnityEngine;

/// <summary>
/// 特殊敌人生成事件负载。
/// </summary>
public readonly struct SpecialEnemySpawnedEventArgs
{
    public GameObject EnemyObject { get; }
    public string EnemyConfigId { get; }
    public EnemyAbilityTag AbilityTags { get; }

    public SpecialEnemySpawnedEventArgs(GameObject enemyObject, string enemyConfigId, EnemyAbilityTag abilityTags)
    {
        EnemyObject = enemyObject;
        EnemyConfigId = enemyConfigId ?? string.Empty;
        AbilityTags = abilityTags;
    }
}

/// <summary>
/// 特殊敌人能力触发事件负载（UI / Audio / 调试）。
/// </summary>
public readonly struct SpecialEnemyAbilityUsedEventArgs
{
    public GameObject EnemyObject { get; }
    public string EnemyConfigId { get; }
    public EnemyAbilityTag AbilityTag { get; }
    public string AbilityConfigId { get; }

    public SpecialEnemyAbilityUsedEventArgs(
        GameObject enemyObject,
        string enemyConfigId,
        EnemyAbilityTag abilityTag,
        string abilityConfigId)
    {
        EnemyObject = enemyObject;
        EnemyConfigId = enemyConfigId ?? string.Empty;
        AbilityTag = abilityTag;
        AbilityConfigId = abilityConfigId ?? string.Empty;
    }
}
