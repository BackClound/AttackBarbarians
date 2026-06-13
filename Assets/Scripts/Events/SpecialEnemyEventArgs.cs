using UnityEngine;

/// <summary>
/// 特殊敌人生成事件负载。
/// </summary>
public readonly struct SpecialEnemySpawnedEventArgs
{
    /// <summary>敌人 GameObject。</summary>
    public GameObject EnemyObject { get; }
    /// <summary>敌人配置 ID。</summary>
    public string EnemyConfigId { get; }
    /// <summary>特殊敌人能力标签组合。</summary>
    public EnemyAbilityTag AbilityTags { get; }

    /// <summary>构造 <see cref="SpecialEnemySpawnedEventArgs"/>。</summary>
    /// <param name="enemyObject">敌人 GameObject。</param>
    /// <param name="enemyConfigId">敌人配置 ID。</param>
    /// <param name="abilityTags">特殊敌人能力标签组合。</param>
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
    /// <summary>敌人 GameObject。</summary>
    public GameObject EnemyObject { get; }
    /// <summary>敌人配置 ID。</summary>
    public string EnemyConfigId { get; }
    /// <summary>触发的能力标签。</summary>
    public EnemyAbilityTag AbilityTag { get; }
    /// <summary>能力配置 ID。</summary>
    public string AbilityConfigId { get; }

    /// <summary>构造 <see cref="SpecialEnemyAbilityUsedEventArgs"/>。</summary>
    /// <param name="enemyObject">敌人 GameObject。</param>
    /// <param name="enemyConfigId">敌人配置 ID。</param>
    /// <param name="abilityTag">触发的能力标签。</param>
    /// <param name="abilityConfigId">能力配置 ID。</param>
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
