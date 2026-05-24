using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从波次 <see cref="WaveDataSO.SpecialEnemyConfigIds"/> 中随机选取特殊敌人 configId。
/// </summary>
/// <remarks>纯逻辑类，无需挂载。</remarks>
public sealed class WaveSpecialEnemySelector
{
    private readonly List<string> pool = new List<string>(8);

    public void Configure(WaveDataSO wave, ConfigManager configManager)
    {
        pool.Clear();
        if (wave == null || configManager == null)
        {
            return;
        }

        IReadOnlyList<string> ids = wave.SpecialEnemyConfigIds;
        if (ids == null)
        {
            return;
        }

        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            if (string.IsNullOrEmpty(id) || !configManager.TryGetEnemy(id, out EnemyDataSO data))
            {
                continue;
            }

            if (!SpecialEnemyRules.HasMechanics(data.AbilityTags))
            {
                continue;
            }

            pool.Add(id);
        }
    }

    public bool TryPick(out string configId)
    {
        configId = null;
        if (pool.Count == 0)
        {
            return false;
        }

        configId = pool[Random.Range(0, pool.Count)];
        return !string.IsNullOrEmpty(configId);
    }
}
