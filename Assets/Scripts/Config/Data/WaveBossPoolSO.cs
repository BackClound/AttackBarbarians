using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可复用的 Boss 池，支持权重随机或按波次轮替。
/// </summary>
[CreateAssetMenu(fileName = "WaveBossPool", menuName = "Attack Barbarians/Config/Wave Boss Pool")]
public class WaveBossPoolSO : ScriptableObject
{
    public enum BossSelectionMode
    {
        RoundRobin,
        WeightedRandom
    }

    [SerializeField] private List<WaveBossEntry> bossEntries = new List<WaveBossEntry>();
    [SerializeField] private BossSelectionMode selectionMode = BossSelectionMode.RoundRobin;
    [SerializeField] private float bossSpawnAtElapsed = 25f;
    [SerializeField] private bool requireBossDefeatToComplete = true;
    [SerializeField] private bool pauseNormalSpawnsWhileBossAlive = true;

    /// <summary>Boss 条目列表。</summary>
    public IReadOnlyList<WaveBossEntry> BossEntries => bossEntries;

    /// <summary>Boss 选取模式。</summary>
    public BossSelectionMode SelectionMode => selectionMode;

    /// <summary>默认 Boss 出现时间（秒）。</summary>
    public float BossSpawnAtElapsed => Mathf.Max(0f, bossSpawnAtElapsed);

    /// <summary>是否必须击败 Boss 才能完成波次。</summary>
    public bool RequireBossDefeatToComplete => requireBossDefeatToComplete;

    /// <summary>Boss 存活时是否暂停普通刷怪。</summary>
    public bool PauseNormalSpawnsWhileBossAlive => pauseNormalSpawnsWhileBossAlive;

    /// <summary>是否配置了有效 Boss。</summary>
    public bool HasContent => bossEntries != null && bossEntries.Count > 0;

    /// <summary>按波次序号从池中选取 Boss configId。</summary>
    public string PickBossConfigId(int waveIndex, int segmentStartWave = 1)
    {
        if (!HasContent)
        {
            return null;
        }

        List<WaveBossEntry> valid = CollectValidEntries();
        if (valid.Count == 0)
        {
            return null;
        }

        if (selectionMode == BossSelectionMode.RoundRobin)
        {
            int offset = Mathf.Max(0, waveIndex - segmentStartWave);
            return valid[offset % valid.Count].BossConfigId;
        }

        return PickWeighted(valid, waveIndex);
    }

    /// <summary>收集有效 Boss 条目。</summary>
    private List<WaveBossEntry> CollectValidEntries()
    {
        List<WaveBossEntry> valid = new List<WaveBossEntry>(bossEntries.Count);
        for (int i = 0; i < bossEntries.Count; i++)
        {
            WaveBossEntry entry = bossEntries[i];
            if (entry != null && !string.IsNullOrEmpty(entry.BossConfigId))
            {
                valid.Add(entry);
            }
        }

        return valid;
    }

    /// <summary>按波次种子做加权随机，保证同波次结果稳定。</summary>
    private static string PickWeighted(List<WaveBossEntry> entries, int waveIndex)
    {
        int totalWeight = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            totalWeight += entries[i].Weight;
        }

        if (totalWeight <= 0)
        {
            return entries[0].BossConfigId;
        }

        Random.State previous = Random.state;
        Random.InitState(waveIndex * 92821 + 17);
        int roll = Random.Range(0, totalWeight);
        Random.state = previous;

        for (int i = 0; i < entries.Count; i++)
        {
            roll -= entries[i].Weight;
            if (roll < 0)
            {
                return entries[i].BossConfigId;
            }
        }

        return entries[entries.Count - 1].BossConfigId;
    }
}
