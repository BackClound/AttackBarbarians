using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人数值、Prefab 与对象池 Key 配置。
/// </summary>
[CreateAssetMenu(fileName = "EnemyData", menuName = "Attack Barbarians/Config/Enemy Data")]
public class EnemyDataSO : ConfigDataBase
{
    [Header("Stats")]
    [SerializeField] private StatBlockConfig baseStats = new StatBlockConfig();

    [Header("Combat")]
    [SerializeField] private float attackDistance = 1.5f;
    [SerializeField] private float contactDamage = 10f;
    [SerializeField] private float attackCooldown = 1f;

    [Header("Spawn")]
    [SerializeField] private GameObject prefab;
    [SerializeField] private string poolKey = GameConstants.PoolKeys.Enemy;
    [SerializeField] private int spawnWeight = 1;

    [Header("Classification")]
    [SerializeField] private EnemyAbilityTag abilityTags = EnemyAbilityTag.Normal;
    [SerializeField] private List<SpecialEnemyAbilityBinding> abilityBindings = new List<SpecialEnemyAbilityBinding>();
    [Tooltip("击杀特殊敌人时额外经验（由 SpecialEnemyController 读取）。")]
    [SerializeField] private int specialBonusExperience = 3;

    [Header("Rewards")]
    [Tooltip("击杀时授予玩家经验；金币/钻石仅在局末结算，死亡不掉落。")]
    [SerializeField] private int experienceReward = 5;

    public StatBlockConfig BaseStats => baseStats;
    public EnemyAbilityTag AbilityTags => abilityTags;
    public IReadOnlyList<SpecialEnemyAbilityBinding> AbilityBindings => abilityBindings;
    public int SpecialBonusExperience => Mathf.Max(0, specialBonusExperience);

    public string TryGetAbilityConfigId(EnemyAbilityTag tag)
    {
        if (abilityBindings != null)
        {
            for (int i = 0; i < abilityBindings.Count; i++)
            {
                SpecialEnemyAbilityBinding binding = abilityBindings[i];
                if (binding != null && binding.Tag == tag && !string.IsNullOrWhiteSpace(binding.AbilityConfigId))
                {
                    return binding.AbilityConfigId;
                }
            }
        }

        return string.Empty;
    }
    public float AttackDistance => Mathf.Max(0.1f, attackDistance);
    public float ContactDamage => Mathf.Max(0f, contactDamage);
    public float AttackCooldown => Mathf.Max(0.05f, attackCooldown);
    public GameObject Prefab => prefab;
    public string PoolKey => string.IsNullOrEmpty(poolKey) ? GameConstants.PoolKeys.Enemy : poolKey;
    public int SpawnWeight => Mathf.Max(1, spawnWeight);
    public int ExperienceReward => Mathf.Max(0, experienceReward);

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (baseStats != null && baseStats.MaxHp <= 0f)
        {
            result.AddError(name, "MaxHp 必须大于 0。");
        }

        if (spawnWeight < 1)
        {
            result.AddError(name, "spawnWeight 不能小于 1。");
        }

        if (prefab == null)
        {
            result.AddWarning(name, "未指定 Prefab，生成时需使用场景内备用引用。");
        }
    }
}
