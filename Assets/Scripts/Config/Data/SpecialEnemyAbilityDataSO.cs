using UnityEngine;

/// <summary>
/// 特殊敌人能力参数：冷却、伤害倍率、召唤与护盾等。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/SpecialEnemy/Ability/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "SpecialEnemyAbilityData", menuName = "Attack Barbarians/Config/Special Enemy Ability Data")]
public class SpecialEnemyAbilityDataSO : ConfigDataBase
{
    [Header("Ability")]
    [SerializeField] private EnemyAbilityTag abilityTag = EnemyAbilityTag.Charge;
    [SerializeField] private float cooldownSeconds = 4f;
    [SerializeField] private float initialCooldownSeconds = 1f;
    [SerializeField] private float durationSeconds = 0.5f;

    [Header("Combat")]
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float range = 2f;
    [SerializeField] private int hitCount = 1;
    [SerializeField] private float shieldHp = 40f;

    [Header("Summon / Split")]
    [SerializeField] private string summonEnemyConfigId;
    [SerializeField] private int summonCount = 2;
    [SerializeField] private int splitCount = 2;

    public EnemyAbilityTag AbilityTag => abilityTag;
    public float CooldownSeconds => Mathf.Max(0.1f, cooldownSeconds);
    public float InitialCooldownSeconds => Mathf.Max(0f, initialCooldownSeconds);
    public float DurationSeconds => Mathf.Max(0f, durationSeconds);
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public float Range => Mathf.Max(0.1f, range);
    public int HitCount => Mathf.Max(1, hitCount);
    public float ShieldHp => Mathf.Max(1f, shieldHp);
    public string SummonEnemyConfigId => summonEnemyConfigId;
    public int SummonCount => Mathf.Max(1, summonCount);
    public int SplitCount => Mathf.Max(1, splitCount);

    /// <summary>
    /// 收集特殊敌人能力配置的校验错误与警告。
    /// </summary>
    /// <param name="result">校验结果容器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if ((abilityTag == EnemyAbilityTag.Summon || abilityTag == EnemyAbilityTag.Split) &&
            string.IsNullOrWhiteSpace(summonEnemyConfigId))
        {
            result.AddWarning(name, $"{abilityTag} 能力未配置 summonEnemyConfigId。");
        }
    }
}
