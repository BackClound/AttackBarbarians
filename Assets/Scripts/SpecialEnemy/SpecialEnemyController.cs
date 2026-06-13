using UnityEngine;

/// <summary>
/// 特殊敌人标记：生成时发布 <see cref="GameEvents.RaiseSpecialEnemySpawned"/>，并提供额外经验奖励。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>可选。缺失时 <see cref="EnemySpawnerManager"/> 会在生成特殊敌人时自动添加。</para>
/// </remarks>
[DisallowMultipleComponent]
public class SpecialEnemyController : MonoBehaviour
{
    [SerializeField] private int bonusExperience = 3;

    private bool isSpecialSpawn;
    private string enemyConfigId = string.Empty;
    private EnemyAbilityTag abilityTags;

    /// <summary>是否为特殊敌人生成实例。</summary>
    public bool IsSpecialSpawn => isSpecialSpawn;
    /// <summary>能力标签组合。</summary>
    public EnemyAbilityTag AbilityTags => abilityTags;
    /// <summary>击败后额外经验。</summary>
    public int BonusExperience => Mathf.Max(0, bonusExperience);

    /// <summary>
    /// 标记为特殊敌人并广播生成事件。
    /// </summary>
    /// <param name="configId">敌人配置 Id。</param>
    /// <param name="tags">能力标签。</param>
    /// <param name="bonusExpOverride">额外经验覆盖（≥0 时生效）。</param>
    public void Initialize(string configId, EnemyAbilityTag tags, int bonusExpOverride = -1)
    {
        isSpecialSpawn = true;
        enemyConfigId = configId ?? string.Empty;
        abilityTags = tags;
        if (bonusExpOverride >= 0)
        {
            bonusExperience = bonusExpOverride;
        }

        GameEvents.RaiseSpecialEnemySpawned(this, new SpecialEnemySpawnedEventArgs(
            gameObject,
            enemyConfigId,
            abilityTags));
    }

    /// <summary>回收到对象池前重置特殊敌人标记。</summary>
    public void ResetForPool()
    {
        isSpecialSpawn = false;
        enemyConfigId = string.Empty;
        abilityTags = EnemyAbilityTag.None;
    }
}
