using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 从波次 <see cref="WaveSpawnProfile.SpecialEnemyConfigIds"/> 中随机选取特殊敌人 configId。
/// </summary>
/// <remarks>纯逻辑类，无需挂载。</remarks>
public sealed class WaveSpecialEnemySelector
{
    private readonly List<string> pool = new List<string>(8);

    /// <summary>
    /// 根据波次快照筛选带机制的特殊敌人并构建随机池。
    /// </summary>
    /// <param name="profile">运行时波次刷怪快照。</param>
    /// <param name="configManager">配置管理器。</param>
    public void Configure(WaveSpawnProfile profile, ConfigManager configManager)
    {
        pool.Clear();
        if (profile == null || configManager == null)
        {
            return;
        }

        IReadOnlyList<string> ids = profile.SpecialEnemyConfigIds;
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

    /// <summary>
    /// 从池中随机选取一个特殊敌人 configId。
    /// </summary>
    /// <param name="configId">输出的 configId。</param>
    /// <returns>选取成功返回 true，池为空时返回 false。</returns>
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
