using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 根据 <see cref="WaveDataSO"/> 与波次已过时间构建权重池并随机选取敌人 configId。
/// </summary>
/// <remarks>纯逻辑类，无需挂载。</remarks>
public sealed class WaveSpawnSelector
{
    private readonly List<string> weightedIds = new List<string>(32);
    private readonly List<WaveEnemyEntry> activeEntries = new List<WaveEnemyEntry>(8);
    private WaveDataSO waveData;

    public void Configure(WaveDataSO wave, ConfigManager configManager)
    {
        waveData = wave;
        weightedIds.Clear();
        activeEntries.Clear();

        if (wave == null)
        {
            return;
        }

        IReadOnlyList<WaveEnemyEntry> entries = wave.EnemyEntries;
        if (entries != null && entries.Count > 0)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                WaveEnemyEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.EnemyConfigId))
                {
                    continue;
                }

                activeEntries.Add(entry);
            }

            return;
        }

        BuildPoolFromConfigIds(wave.EnemyConfigIds, configManager);
    }

    public string PickEnemyId(float waveElapsedSeconds)
    {
        if (waveData == null)
        {
            return GameConstants.ConfigIds.EnemyBat;
        }

        if (activeEntries.Count > 0)
        {
            return PickFromEntries(waveElapsedSeconds);
        }

        if (weightedIds.Count == 0)
        {
            return GameConstants.ConfigIds.EnemyBat;
        }

        return weightedIds[Random.Range(0, weightedIds.Count)];
    }

    public float GetEntryStatMultiplier(string configId, float waveStatMultiplier)
    {
        if (string.IsNullOrEmpty(configId) || activeEntries.Count == 0)
        {
            return waveStatMultiplier;
        }

        for (int i = 0; i < activeEntries.Count; i++)
        {
            WaveEnemyEntry entry = activeEntries[i];
            if (entry != null && entry.EnemyConfigId == configId)
            {
                return waveStatMultiplier * entry.StatMultiplier;
            }
        }

        return waveStatMultiplier;
    }

    private string PickFromEntries(float waveElapsedSeconds)
    {
        weightedIds.Clear();
        float duration = waveData.WaveDuration;

        for (int i = 0; i < activeEntries.Count; i++)
        {
            WaveEnemyEntry entry = activeEntries[i];
            if (entry == null || !entry.IsActiveAt(waveElapsedSeconds, duration))
            {
                continue;
            }

            for (int w = 0; w < entry.Weight; w++)
            {
                weightedIds.Add(entry.EnemyConfigId);
            }
        }

        if (weightedIds.Count == 0)
        {
            return GameConstants.ConfigIds.EnemyBat;
        }

        return weightedIds[Random.Range(0, weightedIds.Count)];
    }

    private void BuildPoolFromConfigIds(IReadOnlyList<string> enemyConfigIds, ConfigManager configManager)
    {
        if (enemyConfigIds == null || configManager == null)
        {
            return;
        }

        for (int i = 0; i < enemyConfigIds.Count; i++)
        {
            string id = enemyConfigIds[i];
            if (string.IsNullOrEmpty(id) || !configManager.TryGetEnemy(id, out EnemyDataSO enemyData))
            {
                continue;
            }

            int weight = enemyData.SpawnWeight;
            for (int w = 0; w < weight; w++)
            {
                weightedIds.Add(id);
            }
        }
    }
}
