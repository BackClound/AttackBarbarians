using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次生成配置：间隔、数量与敌人 ID 权重表。
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "Attack Barbarians/Config/Wave Data")]
public class WaveDataSO : ConfigDataBase
{
    [Header("Wave")]
    [SerializeField] private int waveIndex = 1;
    [SerializeField] private float waveDuration = 30f;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int maxSpawnCount = 20;

    [Header("Enemies")]
    [SerializeField] private List<string> enemyConfigIds = new List<string>();

    public int WaveIndex => Mathf.Max(1, waveIndex);
    public float WaveDuration => Mathf.Max(1f, waveDuration);
    public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
    public int MaxSpawnCount => Mathf.Max(1, maxSpawnCount);
    public IReadOnlyList<string> EnemyConfigIds => enemyConfigIds;

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

        if (enemyConfigIds == null || enemyConfigIds.Count == 0)
        {
            result.AddWarning(name, "未配置 enemyConfigIds。");
        }
    }
}
