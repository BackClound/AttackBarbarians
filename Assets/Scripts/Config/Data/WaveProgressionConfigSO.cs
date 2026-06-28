using UnityEngine;

/// <summary>
/// 局内波次成长全局曲线：玩家升级门槛（仅随等级）、敌人属性、击杀经验与刷怪节奏。
/// </summary>
/// <remarks>
/// <para><b>路径：</b><c>Assets/Resources/Config/Wave/WaveProgression_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "WaveProgression", menuName = "Attack Barbarians/Config/Wave Progression")]
public class WaveProgressionConfigSO : ScriptableObject
{
    [Header("Player Level")]
    [SerializeField] private float expBase = 80f;
    [SerializeField] private float expGrowthPower = 1.35f;
    [SerializeField] private float expLambda = 0.02f;
    [SerializeField] private int expLambdaStartLevel = 8;
    [SerializeField] private float passiveExpPerSecond = 6f;

    [Header("Enemy Stats Per Wave")]
    [SerializeField] private WaveStatCurve hpCurve = WaveStatCurve.Multiplicative(0.10f, 1.30f);
    [SerializeField] private WaveStatCurve damageCurve = WaveStatCurve.Multiplicative(0.08f, 1.25f);
    [SerializeField] private WaveStatCurve moveSpeedCurve = WaveStatCurve.Multiplicative(0.025f, 1.15f, 1.45f);
    [SerializeField] private WaveStatCurve attackSpeedCurve = WaveStatCurve.Multiplicative(0.03f, 1.20f, 1.35f);
    [SerializeField] private WaveStatCurve critChanceCurve = WaveStatCurve.Additive(0.004f, 1.10f, 0.15f);
    [SerializeField] private WaveStatCurve critPowerCurve = WaveStatCurve.Additive(0.02f, 1.10f, 0.40f);

    [Header("Enemy Experience")]
    [SerializeField] private float expWaveCoeff = 0.06f;
    [SerializeField] private float expWavePower = 1.20f;
    [SerializeField] private float normalEnemyExpWeight = 1f;
    [SerializeField] private float specialEnemyExpWeight = 1.5f;
    [SerializeField] private float eliteEnemyExpWeight = 2f;

    [Header("Spawn")]
    [SerializeField] private float spawnIntervalBase = 1.5f;
    [SerializeField] private float spawnIntervalDecay = 0.965f;
    [SerializeField] private float spawnIntervalMin = 0.35f;
    [SerializeField] private int spawnCountBase = 20;
    [SerializeField] private int spawnCountPerWave = 3;
    [SerializeField] private int spawnCountMax = 80;
    [SerializeField] private float waveDurationBase = 30f;
    [SerializeField] private int waveDurationStep = 5;
    [SerializeField] private float waveDurationMax = 45f;

    [Header("Optional Spawn Chances")]
    [SerializeField] private float eliteChancePerWave = 0.004f;
    [SerializeField] private float eliteChanceMax = 0.25f;
    [SerializeField] private float specialChancePerWave = 0.005f;
    [SerializeField] private float specialChanceMax = 0.30f;

    public float ExpBase => Mathf.Max(1f, expBase);
    public float ExpGrowthPower => Mathf.Max(0.01f, expGrowthPower);
    public float ExpLambda => Mathf.Max(0f, expLambda);
    public int ExpLambdaStartLevel => Mathf.Max(1, expLambdaStartLevel);
    public float PassiveExpPerSecond => Mathf.Max(0f, passiveExpPerSecond);
    public WaveStatCurve HpCurve => hpCurve;
    public WaveStatCurve DamageCurve => damageCurve;
    public WaveStatCurve MoveSpeedCurve => moveSpeedCurve;
    public WaveStatCurve AttackSpeedCurve => attackSpeedCurve;
    public WaveStatCurve CritChanceCurve => critChanceCurve;
    public WaveStatCurve CritPowerCurve => critPowerCurve;
    public float ExpWaveCoeff => Mathf.Max(0f, expWaveCoeff);
    public float ExpWavePower => Mathf.Max(0.01f, expWavePower);
    public float NormalEnemyExpWeight => Mathf.Max(0f, normalEnemyExpWeight);
    public float SpecialEnemyExpWeight => Mathf.Max(0f, specialEnemyExpWeight);
    public float EliteEnemyExpWeight => Mathf.Max(0f, eliteEnemyExpWeight);
    public float SpawnIntervalBase => Mathf.Max(0.05f, spawnIntervalBase);
    public float SpawnIntervalDecay => Mathf.Clamp(spawnIntervalDecay, 0.5f, 1f);
    public float SpawnIntervalMin => Mathf.Max(0.05f, spawnIntervalMin);
    public int SpawnCountBase => Mathf.Max(1, spawnCountBase);
    public int SpawnCountPerWave => Mathf.Max(0, spawnCountPerWave);
    public int SpawnCountMax => Mathf.Max(1, spawnCountMax);
    public float WaveDurationBase => Mathf.Max(1f, waveDurationBase);
    public int WaveDurationStep => Mathf.Max(0, waveDurationStep);
    public float WaveDurationMax => Mathf.Max(WaveDurationBase, waveDurationMax);
    public float EliteChancePerWave => Mathf.Max(0f, eliteChancePerWave);
    public float EliteChanceMax => Mathf.Clamp01(eliteChanceMax);
    public float SpecialChancePerWave => Mathf.Max(0f, specialChancePerWave);
    public float SpecialChanceMax => Mathf.Clamp01(specialChanceMax);

    /// <summary>按属性类型返回对应曲线。</summary>
    public WaveStatCurve GetCurveForStat(StatType statType)
    {
        switch (statType)
        {
            case StatType.MaxHp:
                return hpCurve;
            case StatType.Damage:
            case StatType.FireDamage:
            case StatType.IceDamage:
            case StatType.LightningDamage:
                return damageCurve;
            case StatType.MoveSpeed:
                return moveSpeedCurve;
            case StatType.AttackSpeed:
            case StatType.AttackSpeedMulti:
                return attackSpeedCurve;
            case StatType.CritChance:
                return critChanceCurve;
            case StatType.CritPower:
                return critPowerCurve;
            default:
                return damageCurve;
        }
    }
}
