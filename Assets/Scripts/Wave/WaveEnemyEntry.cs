using System;
using UnityEngine;

/// <summary>
/// 单条波次敌人生成条目：权重、时间窗与属性倍率。
/// </summary>
/// <remarks>纯数据结构，无需挂载。由 <see cref="WaveDataSO"/> 序列化引用。</remarks>
[Serializable]
public class WaveEnemyEntry
{
    [SerializeField] private string enemyConfigId;
    [SerializeField] private int weight = 1;
    [Tooltip("该条目在波次内最多生成次数；0 表示仅参与权重随机，不单独限额。")]
    [SerializeField] private int maxSpawnCount;
    [Tooltip("相对波次开始后的最早生成时间（秒）。")]
    [SerializeField] private float spawnWindowStart;
    [Tooltip("相对波次开始后的最晚生成时间（秒）；≤0 表示持续到波次结束。")]
    [SerializeField] private float spawnWindowEnd;
    [Tooltip("在波次全局属性倍率之上再乘算。")]
    [SerializeField] private float statMultiplier = 1f;

    public string EnemyConfigId => enemyConfigId;
    public int Weight => Mathf.Max(1, weight);
    public int MaxSpawnCount => Mathf.Max(0, maxSpawnCount);
    public float SpawnWindowStart => Mathf.Max(0f, spawnWindowStart);
    public float SpawnWindowEnd => spawnWindowEnd;
    public float StatMultiplier => Mathf.Max(0.1f, statMultiplier);

    public bool IsActiveAt(float waveElapsedSeconds, float waveDurationSeconds)
    {
        if (waveElapsedSeconds < SpawnWindowStart)
        {
            return false;
        }

        float end = SpawnWindowEnd;
        if (end <= 0f)
        {
            end = waveDurationSeconds;
        }

        return waveElapsedSeconds <= end;
    }
}
