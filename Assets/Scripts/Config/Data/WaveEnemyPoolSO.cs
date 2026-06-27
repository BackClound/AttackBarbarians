using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可复用的敌人组合池，供多个波次段引用。
/// </summary>
[CreateAssetMenu(fileName = "WaveEnemyPool", menuName = "Attack Barbarians/Config/Wave Enemy Pool")]
public class WaveEnemyPoolSO : ScriptableObject
{
    [SerializeField] private List<WaveEnemyEntry> enemyEntries = new List<WaveEnemyEntry>();
    [SerializeField] private List<string> enemyConfigIds = new List<string>();

    /// <summary>加权敌人条目（优先于 enemyConfigIds）。</summary>
    public IReadOnlyList<WaveEnemyEntry> EnemyEntries => enemyEntries;

    /// <summary>旧式敌人 configId 列表。</summary>
    public IReadOnlyList<string> EnemyConfigIds => enemyConfigIds;

    /// <summary>是否配置了有效敌人。</summary>
    public bool HasContent =>
        (enemyEntries != null && enemyEntries.Count > 0) ||
        (enemyConfigIds != null && enemyConfigIds.Count > 0);
}
