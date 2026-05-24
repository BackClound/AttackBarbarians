using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次生成配置：持续时间、敌人组合、Boss 与奖励表引用。
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "Attack Barbarians/Config/Wave Data")]
public class WaveDataSO : ConfigDataBase
{
    [Header("Wave")]
    [SerializeField] private int waveIndex = 1;
    [SerializeField] private float waveDuration = 30f;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int maxSpawnCount = 20;
    [Tooltip("每经过一波，敌人四维属性额外乘算 (waveIndex-1)*该系数。")]
    [SerializeField] private float statScalePerWave = 0.08f;

    [Header("Enemies")]
    [SerializeField] private List<WaveEnemyEntry> enemyEntries = new List<WaveEnemyEntry>();
    [SerializeField] private List<string> enemyConfigIds = new List<string>();

    [Header("Boss")]
    [SerializeField] private bool hasBoss;
    [SerializeField] private string bossConfigId;
    [SerializeField] private float bossSpawnAtElapsed = 25f;
    [SerializeField] private bool requireBossDefeatToComplete = true;

    [Header("Elite")]
    [Tooltip("每成功生成一名普通敌人时，额外以该概率生成精英个体（0~1）。")]
    [SerializeField] private float eliteSpawnChance;
    [SerializeField] private bool pauseNormalSpawnsWhileBossAlive;

    [Header("Special Enemy")]
    [Tooltip("每成功生成一名普通敌人时，额外以该概率从 specialEnemyConfigIds 中刷一只特殊怪（0~1）。")]
    [SerializeField] private float specialSpawnChance;
    [SerializeField] private List<string> specialEnemyConfigIds = new List<string>();

    [Header("Rewards")]
    [SerializeField] private string rewardTableId;

    public int WaveIndex => Mathf.Max(1, waveIndex);
    public float WaveDuration => Mathf.Max(1f, waveDuration);
    public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
    public int MaxSpawnCount => Mathf.Max(1, maxSpawnCount);
    public float StatScalePerWave => Mathf.Max(0f, statScalePerWave);
    public IReadOnlyList<WaveEnemyEntry> EnemyEntries => enemyEntries;
    public IReadOnlyList<string> EnemyConfigIds => enemyConfigIds;
    public bool HasBoss => hasBoss;
    public string BossConfigId => bossConfigId;
    public float BossSpawnAtElapsed => Mathf.Max(0f, bossSpawnAtElapsed);
    public bool RequireBossDefeatToComplete => requireBossDefeatToComplete;
    public float EliteSpawnChance => Mathf.Clamp01(eliteSpawnChance);
    public bool PauseNormalSpawnsWhileBossAlive => pauseNormalSpawnsWhileBossAlive;
    public float SpecialSpawnChance => Mathf.Clamp01(specialSpawnChance);
    public IReadOnlyList<string> SpecialEnemyConfigIds => specialEnemyConfigIds;
    public string RewardTableId => rewardTableId;

    public float GetStatMultiplierForWave(int currentWaveIndex)
    {
        int index = Mathf.Max(1, currentWaveIndex);
        return 1f + (index - 1) * StatScalePerWave;
    }

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (waveIndex < 1)
        {
            result.AddError(name, "waveIndex 不能小于 1。");
        }

        if (spawnInterval <= 0f)
        {
            result.AddError(name, "spawnInterval 必须大于 0。");
        }

        bool hasEntries = enemyEntries != null && enemyEntries.Count > 0;
        bool hasLegacyIds = enemyConfigIds != null && enemyConfigIds.Count > 0;
        if (!hasEntries && !hasLegacyIds)
        {
            result.AddWarning(name, "未配置 enemyEntries 或 enemyConfigIds。");
        }

        if (hasBoss && string.IsNullOrWhiteSpace(bossConfigId))
        {
            result.AddWarning(name, "hasBoss 为 true 但 bossConfigId 为空。");
        }
    }
}
