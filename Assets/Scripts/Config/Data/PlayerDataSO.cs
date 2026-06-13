using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家基础数值与展示配置。
/// </summary>
/// <remarks>
/// <para><b>创建：</b>Attack Barbarians → Config → Player Data。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Player/</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "PlayerData", menuName = "Attack Barbarians/Config/Player Data")]
public class PlayerDataSO : ConfigDataBase
{
    [Header("Stats")]
    [SerializeField] private StatBlockConfig baseStats = new StatBlockConfig();

    [Header("Progression")]
    [SerializeField] private int startLevel = 1;
    [SerializeField] private float experiencePerLevel = 100f;

    [Header("Combat")]
    [SerializeField] private float attackRadius = 25f;
    [SerializeField] private PlayerTargetPolicy targetPolicy = PlayerTargetPolicy.NearestToWall;
    [SerializeField] private string[] initialSkillIds;

    [Header("Presentation")]
    [SerializeField] private string description;

    public StatBlockConfig BaseStats => baseStats;
    public int StartLevel => Mathf.Max(1, startLevel);
    public float ExperiencePerLevel => Mathf.Max(1f, experiencePerLevel);
    public float AttackRadius => Mathf.Max(0.5f, attackRadius);
    public PlayerTargetPolicy TargetPolicy => targetPolicy;
    public IReadOnlyList<string> InitialSkillIds =>
        initialSkillIds != null ? initialSkillIds : System.Array.Empty<string>();
    public string Description => description;

    /// <summary>
    /// 收集玩家配置的校验错误与警告。
    /// </summary>
    /// <param name="result">校验结果容器。</param>
    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (baseStats != null && baseStats.MaxHp <= 0f)
        {
            result.AddError(name, "MaxHp 必须大于 0。");
        }

        if (startLevel < 1)
        {
            result.AddError(name, "startLevel 不能小于 1。");
        }
    }
}
