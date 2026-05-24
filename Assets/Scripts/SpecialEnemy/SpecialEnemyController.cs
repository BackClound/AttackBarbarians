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

    public bool IsSpecialSpawn => isSpecialSpawn;
    public EnemyAbilityTag AbilityTags => abilityTags;
    public int BonusExperience => Mathf.Max(0, bonusExperience);

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

    public void ResetForPool()
    {
        isSpecialSpawn = false;
        enemyConfigId = string.Empty;
        abilityTags = EnemyAbilityTag.None;
    }
}
