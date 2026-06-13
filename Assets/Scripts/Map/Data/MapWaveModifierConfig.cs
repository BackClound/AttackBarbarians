using UnityEngine;

/// <summary>
/// 地图对波次刷怪与敌人属性的全局修正。
/// </summary>
[System.Serializable]
public struct MapWaveModifierConfig
{
    [Tooltip("敌人属性乘算，1 为不变。")]
    [SerializeField] private float enemyStatMultiplier;
    [Tooltip("刷怪间隔乘算，小于 1 刷怪更快。")]
    [SerializeField] private float spawnIntervalMultiplier;
    [Tooltip("单波最大刷怪数量乘算。")]
    [SerializeField] private float maxSpawnCountMultiplier;

    /// <summary>敌人属性倍率。</summary>
    public float EnemyStatMultiplier => Mathf.Max(0.01f, enemyStatMultiplier <= 0f ? 1f : enemyStatMultiplier);
    /// <summary>刷怪间隔倍率。</summary>
    public float SpawnIntervalMultiplier => Mathf.Max(0.01f, spawnIntervalMultiplier <= 0f ? 1f : spawnIntervalMultiplier);
    /// <summary>单波最大刷怪数量倍率。</summary>
    public float MaxSpawnCountMultiplier => Mathf.Max(0.01f, maxSpawnCountMultiplier <= 0f ? 1f : maxSpawnCountMultiplier);

    /// <summary>无修正的默认配置（各倍率为 1）。</summary>
    public static MapWaveModifierConfig Identity =>
        new MapWaveModifierConfig
        {
            enemyStatMultiplier = 1f,
            spawnIntervalMultiplier = 1f,
            maxSpawnCountMultiplier = 1f
        };
}
