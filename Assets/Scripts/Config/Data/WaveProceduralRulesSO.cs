using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 程序化波次规则：按档位自动计算难度、敌人解锁与 Boss 生成，无需手工配置上千段。
/// </summary>
[CreateAssetMenu(fileName = "WaveProceduralRules", menuName = "Attack Barbarians/Config/Wave Procedural Rules")]
public class WaveProceduralRulesSO : ScriptableObject
{
    [Header("Tier Size")]
    [Tooltip("每 N 波为一个难度档；档内小幅提升，跨档阶跃。")]
    [SerializeField] private int difficultyTierWaveSize = 20;

    [Tooltip("每 N 波从解锁列表增加 1 种敌人。")]
    [SerializeField] private int enemyUnlockWaveInterval = 30;

    [Header("Enemy Pool")]
    [Tooltip("每 30 波窗口内保持固定的池大小；加新时随机移除旧成员。")]
    [SerializeField] private int enemyPoolSize = 3;
    [SerializeField] private List<string> baseEnemyConfigIds = new List<string>();
    [Tooltip("按顺序每隔 enemyUnlockWaveInterval 尝试加入 1 种；全加入后从总池随机替换。")]
    [SerializeField] private List<string> enemyUnlockOrder = new List<string>();
    [SerializeField] private List<string> specialEnemyConfigIds = new List<string>();

    [Header("Boss Pool Rotation")]
    [Tooltip("Boss 轮换池大小（与敌人池同样每 30 波替换逻辑）。")]
    [SerializeField] private int bossActivePoolSize = 1;
    [SerializeField] private WaveBossPoolSO bossPool;
    [SerializeField] private float bossSpawnAtElapsed = 22f;
    [SerializeField] private float bossSpawnStaggerSeconds = 4f;
    [SerializeField] private bool requireBossDefeatToComplete = true;
    [SerializeField] private bool pauseNormalSpawnsWhileBossAlive = true;

    [Header("Boss Milestone (整十波)")]
    [Tooltip("整十波（10/20/30…）触发 Boss 判定的间隔。")]
    [SerializeField] private int bossMilestoneInterval = 10;
    [Tooltip("整十波基础随机概率（0~1）。")]
    [SerializeField] private float bossMilestoneBaseChance = 0.35f;
    [Tooltip("每档难度额外增加的 Boss 概率。")]
    [SerializeField] private float bossMilestoneChancePerTier = 0.08f;
    [Tooltip("难度档边界波次（20/40/60…）必定出 Boss。")]
    [SerializeField] private bool guaranteedBossOnDifficultyTierBoundary = true;

    [Header("Boss Scaling")]
    [SerializeField] private float bossBaseStatMultiplier = 1.2f;
    [SerializeField] private float bossStatMultiplierPerTier = 0.15f;
    [SerializeField] private float bossStatMultiplierMax = 4f;
    [SerializeField] private int bossBaseCount = 1;
    [SerializeField] private int bossCountPerTier = 1;
    [SerializeField] private int bossCountMax = 4;

    [Header("Difficulty Curve (Per Tier)")]
    [Tooltip("每档内（20 波）战斗属性线性增量；跨档由饱和曲线产生更大阶跃。")]
    [SerializeField] private float withinTierCombatIncrement = 0.025f;
    [Tooltip("每档内移速乘算上限；下一档重置为 1。")]
    [SerializeField] private float maxMoveSpeedMultiplierInTier = 1.35f;
    [Tooltip("难度档内战斗属性乘算上限。")]
    [SerializeField] private float maxDifficultyMultiplier = 3.5f;
    [Tooltip("饱和曲线系数；越大前期升得越快。")]
    [SerializeField] private float difficultySaturationRate = 0.45f;
    [Tooltip("达到上限的难度档索引（0=第1档）；超出后系数不再增加。")]
    [SerializeField] private int maxDifficultyTierIndex = 12;

    [Header("Spawn Pressure")]
    [SerializeField] private float eliteSpawnChanceBase = 0.08f;
    [SerializeField] private float eliteSpawnChancePerTier = 0.015f;
    [SerializeField] private float eliteSpawnChanceMax = 0.35f;
    [SerializeField] private float specialSpawnChanceBase = 0.12f;
    [SerializeField] private float specialSpawnChancePerTier = 0.02f;
    [SerializeField] private float specialSpawnChanceMax = 0.40f;

    public int DifficultyTierWaveSize => Mathf.Max(1, difficultyTierWaveSize);
    public int EnemyUnlockWaveInterval => Mathf.Max(1, enemyUnlockWaveInterval);
    public int EnemyPoolSize => Mathf.Max(1, enemyPoolSize);
    public int BossActivePoolSize => Mathf.Max(1, bossActivePoolSize);
    public IReadOnlyList<string> BaseEnemyConfigIds => baseEnemyConfigIds;
    public IReadOnlyList<string> EnemyUnlockOrder => enemyUnlockOrder;
    public IReadOnlyList<string> SpecialEnemyConfigIds => specialEnemyConfigIds;
    public WaveBossPoolSO BossPool => bossPool;
    public float BossSpawnAtElapsed => Mathf.Max(0f, bossSpawnAtElapsed);
    public float BossSpawnStaggerSeconds => Mathf.Max(0.5f, bossSpawnStaggerSeconds);
    public bool RequireBossDefeatToComplete => requireBossDefeatToComplete;
    public bool PauseNormalSpawnsWhileBossAlive => pauseNormalSpawnsWhileBossAlive;
    public int BossMilestoneInterval => Mathf.Max(1, bossMilestoneInterval);
    public float BossMilestoneBaseChance => Mathf.Clamp01(bossMilestoneBaseChance);
    public float BossMilestoneChancePerTier => Mathf.Max(0f, bossMilestoneChancePerTier);
    public bool GuaranteedBossOnDifficultyTierBoundary => guaranteedBossOnDifficultyTierBoundary;
    public float BossBaseStatMultiplier => Mathf.Max(0.1f, bossBaseStatMultiplier);
    public float BossStatMultiplierPerTier => Mathf.Max(0f, bossStatMultiplierPerTier);
    public float BossStatMultiplierMax => Mathf.Max(BossBaseStatMultiplier, bossStatMultiplierMax);
    public int BossBaseCount => Mathf.Max(0, bossBaseCount);
    public int BossCountPerTier => Mathf.Max(0, bossCountPerTier);
    public int BossCountMax => Mathf.Max(1, bossCountMax);
    public float WithinTierCombatIncrement => Mathf.Max(0f, withinTierCombatIncrement);
    public float MaxMoveSpeedMultiplierInTier => Mathf.Max(1f, maxMoveSpeedMultiplierInTier);
    public float MaxDifficultyMultiplier => Mathf.Max(1f, maxDifficultyMultiplier);
    public float DifficultySaturationRate => Mathf.Max(0.01f, difficultySaturationRate);
    public int MaxDifficultyTierIndex => Mathf.Max(0, maxDifficultyTierIndex);
    public float EliteSpawnChanceBase => Mathf.Clamp01(eliteSpawnChanceBase);
    public float EliteSpawnChancePerTier => Mathf.Max(0f, eliteSpawnChancePerTier);
    public float EliteSpawnChanceMax => Mathf.Clamp01(eliteSpawnChanceMax);
    public float SpecialSpawnChanceBase => Mathf.Clamp01(specialSpawnChanceBase);
    public float SpecialSpawnChancePerTier => Mathf.Max(0f, specialSpawnChancePerTier);
    public float SpecialSpawnChanceMax => Mathf.Clamp01(specialSpawnChanceMax);
}
