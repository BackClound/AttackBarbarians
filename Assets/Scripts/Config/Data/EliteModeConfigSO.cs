using UnityEngine;

/// <summary>
/// 精英模式四维倍率（相对普通局同时间点数值）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject）。</para>
/// <para><b>引用：</b>由 <see cref="GameConfig"/> 或 <see cref="RunDifficultyContext"/> 读取。</para>
/// </remarks>
[CreateAssetMenu(fileName = "EliteModeConfig", menuName = "Attack Barbarians/Config/Elite Mode Config")]
public class EliteModeConfigSO : ScriptableObject
{
    [Header("Elite Mode (global run)")]
    [SerializeField] private float hpMultiplier = 1.5f;
    [SerializeField] private float attackMultiplier = 1.3f;
    [SerializeField] private float moveSpeedMultiplier = 1.1f;
    [SerializeField] private float attackSpeedMultiplier = 1.15f;

    [Header("Elite Enemy (single spawn tag)")]
    [Tooltip("带 Elite 标记的个体在波次倍率之上再乘算。")]
    [SerializeField] private float eliteEnemyBonusMultiplier = 1.25f;

    public float HpMultiplier => Mathf.Max(0.1f, hpMultiplier);
    public float AttackMultiplier => Mathf.Max(0.1f, attackMultiplier);
    public float MoveSpeedMultiplier => Mathf.Max(0.1f, moveSpeedMultiplier);
    public float AttackSpeedMultiplier => Mathf.Max(0.1f, attackSpeedMultiplier);
    public float EliteEnemyBonusMultiplier => Mathf.Max(1f, eliteEnemyBonusMultiplier);

    public void ApplyToSnapshot(StatRuntimeSnapshot snapshot, bool applyEliteEnemyBonus)
    {
        if (snapshot == null)
        {
            return;
        }

        ScaleStat(snapshot, StatType.MaxHp, HpMultiplier);
        ScaleStat(snapshot, StatType.Damage, AttackMultiplier);
        ScaleStat(snapshot, StatType.MoveSpeed, MoveSpeedMultiplier);
        ScaleStat(snapshot, StatType.AttackSpeed, AttackSpeedMultiplier);
        ScaleStat(snapshot, StatType.AttackSpeedMulti, AttackSpeedMultiplier);

        if (applyEliteEnemyBonus && eliteEnemyBonusMultiplier > 1f)
        {
            ScaleStat(snapshot, StatType.MaxHp, EliteEnemyBonusMultiplier);
            ScaleStat(snapshot, StatType.Damage, EliteEnemyBonusMultiplier);
            ScaleStat(snapshot, StatType.MoveSpeed, EliteEnemyBonusMultiplier);
            ScaleStat(snapshot, StatType.AttackSpeed, EliteEnemyBonusMultiplier);
            ScaleStat(snapshot, StatType.AttackSpeedMulti, EliteEnemyBonusMultiplier);
        }
    }

    private static void ScaleStat(StatRuntimeSnapshot snapshot, StatType statType, float multiplier)
    {
        if (Mathf.Approximately(multiplier, 1f))
        {
            return;
        }

        snapshot.Set(statType, snapshot.Get(statType) * multiplier);
    }
}
