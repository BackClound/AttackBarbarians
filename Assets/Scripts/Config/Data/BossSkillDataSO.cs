using UnityEngine;

/// <summary>
/// Boss 技能配置：类型、冷却、伤害与范围等参数。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Boss/Skill/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "BossSkillData", menuName = "Attack Barbarians/Config/Boss Skill Data")]
public class BossSkillDataSO : ConfigDataBase
{
    [Header("Skill")]
    [SerializeField] private BossSkillType skillType = BossSkillType.AreaAttack;
    [SerializeField] private float cooldownSeconds = 5f;
    [SerializeField] private float castDelaySeconds;
    [SerializeField] private float durationSeconds = 1f;

    [Header("Combat")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float range = 2f;
    [SerializeField] private int hitCount = 1;
    [SerializeField] private string summonEnemyConfigId;
    [SerializeField] private int summonCount = 2;

    [Header("Phase Gate")]
    [Tooltip("仅在该阶段及之后可释放；0 表示所有阶段。")]
    [SerializeField] private int minPhaseIndex;

    public BossSkillType SkillType => skillType;
    public float CooldownSeconds => Mathf.Max(0.1f, cooldownSeconds);
    public float CastDelaySeconds => Mathf.Max(0f, castDelaySeconds);
    public float DurationSeconds => Mathf.Max(0f, durationSeconds);
    public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
    public float Range => Mathf.Max(0.1f, range);
    public int HitCount => Mathf.Max(1, hitCount);
    public string SummonEnemyConfigId => summonEnemyConfigId;
    public int SummonCount => Mathf.Max(1, summonCount);
    public int MinPhaseIndex => Mathf.Max(0, minPhaseIndex);

    /// <summary>
    /// 收集 Boss 技能配置的校验错误与警告。
    /// </summary>
    /// <param name="result">校验结果容器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (skillType == BossSkillType.Summon && string.IsNullOrWhiteSpace(summonEnemyConfigId))
        {
            result.AddWarning(name, "Summon 技能未配置 summonEnemyConfigId。");
        }
    }
}
