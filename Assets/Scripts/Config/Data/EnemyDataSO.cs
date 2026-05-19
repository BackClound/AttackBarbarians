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

    [Header("Rewards")]
    [SerializeField] private int experienceReward = 5;
    [SerializeField] private string dropTableId;

    public StatBlockConfig BaseStats => baseStats;
    public float AttackDistance => Mathf.Max(0.1f, attackDistance);
    public float ContactDamage => Mathf.Max(0f, contactDamage);
    public float AttackCooldown => Mathf.Max(0.05f, attackCooldown);
    public GameObject Prefab => prefab;
    public string PoolKey => string.IsNullOrEmpty(poolKey) ? GameConstants.PoolKeys.Enemy : poolKey;
    public int SpawnWeight => Mathf.Max(1, spawnWeight);
    public int ExperienceReward => Mathf.Max(0, experienceReward);
    public string DropTableId => dropTableId;

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
