using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 波次段定义：指定序号区间内共享的敌人/Boss 池与难度修正。
/// </summary>
[Serializable]
public class WaveSegmentDefinition
{
    [Header("Range")]
    [Tooltip("起始波次（含）。")]
    [SerializeField] private int startWave = 1;

    [Tooltip("结束波次（含）；0 表示无限延续至后续所有波次。")]
    [SerializeField] private int endWave;

    [Header("Enemy Pool")]
    [Tooltip("引用可复用敌人池；留空则使用下方内联配置。")]
    [SerializeField] private WaveEnemyPoolSO enemyPool;

    [SerializeField] private List<WaveEnemyEntry> enemyEntries = new List<WaveEnemyEntry>();
    [SerializeField] private List<string> enemyConfigIds = new List<string>();

    [Header("Boss")]
    [Tooltip("引用可复用 Boss 池；留空则使用下方内联配置。")]
    [SerializeField] private WaveBossPoolSO bossPool;

    [SerializeField] private List<WaveBossEntry> bossEntries = new List<WaveBossEntry>();
    [SerializeField] private bool hasBoss = true;
    [SerializeField] private string bossConfigId;
    [SerializeField] private float bossSpawnAtElapsed = 25f;
    [SerializeField] private bool requireBossDefeatToComplete = true;
    [SerializeField] private bool pauseNormalSpawnsWhileBossAlive = true;

    [Header("Elite / Special")]
    [SerializeField] private float eliteSpawnChance = 0.08f;
    [SerializeField] private float specialSpawnChance = 0.12f;
    [SerializeField] private List<string> specialEnemyConfigIds = new List<string>();

    [Header("Difficulty")]
    [SerializeField] private WaveDifficultyOverride difficulty = WaveDifficultyOverride.Identity;

    [Header("Legacy Spawn (V2 关闭时使用)")]
    [SerializeField] private float waveDuration = 30f;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private int maxSpawnCount = 20;
    [SerializeField] private float statScalePerWave = 0.08f;

    [Header("Rewards")]
    [SerializeField] private string rewardTableId;

    /// <summary>起始波次（含）。</summary>
    public int StartWave => Mathf.Max(1, startWave);

    /// <summary>结束波次（含）；0 = 无限。</summary>
    public int EndWave => endWave;

    /// <summary>波次是否落在本段区间内。</summary>
    public bool ContainsWave(int waveIndex)
    {
        waveIndex = Mathf.Max(1, waveIndex);
        if (waveIndex < StartWave)
        {
            return false;
        }

        return EndWave <= 0 || waveIndex <= EndWave;
    }

    /// <summary>解析本段敌人条目。</summary>
    public IReadOnlyList<WaveEnemyEntry> ResolveEnemyEntries()
    {
        if (enemyPool != null && enemyPool.EnemyEntries != null && enemyPool.EnemyEntries.Count > 0)
        {
            return enemyPool.EnemyEntries;
        }

        return enemyEntries;
    }

    /// <summary>解析本段敌人 configId 列表。</summary>
    public IReadOnlyList<string> ResolveEnemyConfigIds()
    {
        if (enemyPool != null && enemyPool.EnemyConfigIds != null && enemyPool.EnemyConfigIds.Count > 0)
        {
            return enemyPool.EnemyConfigIds;
        }

        if (enemyEntries != null && enemyEntries.Count > 0)
        {
            return null;
        }

        return enemyConfigIds;
    }

    /// <summary>解析本段是否生成 Boss。</summary>
    public bool ResolveHasBoss(int waveIndex)
    {
        if (bossPool != null && bossPool.HasContent)
        {
            return true;
        }

        if (bossEntries != null && bossEntries.Count > 0)
        {
            return true;
        }

        return hasBoss && !string.IsNullOrEmpty(bossConfigId);
    }

    /// <summary>解析本段 Boss configId。</summary>
    public string ResolveBossConfigId(int waveIndex)
    {
        if (bossPool != null && bossPool.HasContent)
        {
            return bossPool.PickBossConfigId(waveIndex, StartWave);
        }

        if (bossEntries != null && bossEntries.Count > 0)
        {
            return PickFromInlineBossEntries(bossEntries, waveIndex, StartWave);
        }

        return hasBoss ? bossConfigId : null;
    }

    /// <summary>解析 Boss 出现时间。</summary>
    public float ResolveBossSpawnAtElapsed()
    {
        if (bossPool != null)
        {
            return bossPool.BossSpawnAtElapsed;
        }

        return Mathf.Max(0f, bossSpawnAtElapsed);
    }

    /// <summary>是否必须击败 Boss 才能完成波次。</summary>
    public bool ResolveRequireBossDefeatToComplete()
    {
        return bossPool != null ? bossPool.RequireBossDefeatToComplete : requireBossDefeatToComplete;
    }

    /// <summary>Boss 存活时是否暂停普通刷怪。</summary>
    public bool ResolvePauseNormalSpawnsWhileBossAlive()
    {
        return bossPool != null ? bossPool.PauseNormalSpawnsWhileBossAlive : pauseNormalSpawnsWhileBossAlive;
    }

    public float EliteSpawnChance => Mathf.Clamp01(eliteSpawnChance);
    public float SpecialSpawnChance => Mathf.Clamp01(specialSpawnChance);
    public IReadOnlyList<string> SpecialEnemyConfigIds => specialEnemyConfigIds;
    public WaveDifficultyOverride Difficulty => difficulty;
    public float WaveDuration => Mathf.Max(1f, waveDuration);
    public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
    public int MaxSpawnCount => Mathf.Max(1, maxSpawnCount);
    public float StatScalePerWave => Mathf.Max(0f, statScalePerWave);
    public string RewardTableId => rewardTableId;

    /// <summary>将另一段的非空字段覆盖到本段（用于精确波次补丁）。</summary>
    public WaveSegmentDefinition MergePatch(WaveSegmentDefinition patch)
    {
        if (patch == null)
        {
            return this;
        }

        WaveSegmentDefinition merged = CloneShallow();
        if (patch.enemyPool != null)
        {
            merged.enemyPool = patch.enemyPool;
        }

        if (patch.enemyEntries != null && patch.enemyEntries.Count > 0)
        {
            merged.enemyEntries = patch.enemyEntries;
        }

        if (patch.enemyConfigIds != null && patch.enemyConfigIds.Count > 0)
        {
            merged.enemyConfigIds = patch.enemyConfigIds;
        }

        if (patch.bossPool != null)
        {
            merged.bossPool = patch.bossPool;
        }

        if (patch.bossEntries != null && patch.bossEntries.Count > 0)
        {
            merged.bossEntries = patch.bossEntries;
        }

        if (!string.IsNullOrEmpty(patch.bossConfigId))
        {
            merged.hasBoss = true;
            merged.bossConfigId = patch.bossConfigId;
        }

        if (patch.hasBoss)
        {
            merged.hasBoss = true;
        }

        if (patch.bossSpawnAtElapsed > 0f)
        {
            merged.bossSpawnAtElapsed = patch.bossSpawnAtElapsed;
        }

        merged.requireBossDefeatToComplete = patch.requireBossDefeatToComplete;
        merged.pauseNormalSpawnsWhileBossAlive = patch.pauseNormalSpawnsWhileBossAlive;

        if (patch.eliteSpawnChance > 0f)
        {
            merged.eliteSpawnChance = patch.eliteSpawnChance;
        }

        if (patch.specialSpawnChance > 0f)
        {
            merged.specialSpawnChance = patch.specialSpawnChance;
        }

        if (patch.specialEnemyConfigIds != null && patch.specialEnemyConfigIds.Count > 0)
        {
            merged.specialEnemyConfigIds = patch.specialEnemyConfigIds;
        }

        merged.difficulty = difficulty.Combine(patch.difficulty);

        if (patch.waveDuration > 0f)
        {
            merged.waveDuration = patch.waveDuration;
        }

        if (patch.spawnInterval > 0f)
        {
            merged.spawnInterval = patch.spawnInterval;
        }

        if (patch.maxSpawnCount > 0)
        {
            merged.maxSpawnCount = patch.maxSpawnCount;
        }

        if (patch.statScalePerWave > 0f)
        {
            merged.statScalePerWave = patch.statScalePerWave;
        }

        if (!string.IsNullOrEmpty(patch.rewardTableId))
        {
            merged.rewardTableId = patch.rewardTableId;
        }

        return merged;
    }

    /// <summary>浅拷贝，供解析器合并段定义。</summary>
    public WaveSegmentDefinition CloneForResolve()
    {
        return CloneShallow();
    }

    /// <summary>浅拷贝当前段。</summary>
    private WaveSegmentDefinition CloneShallow()
    {
        return new WaveSegmentDefinition
        {
            startWave = startWave,
            endWave = endWave,
            enemyPool = enemyPool,
            enemyEntries = enemyEntries,
            enemyConfigIds = enemyConfigIds,
            bossPool = bossPool,
            bossEntries = bossEntries,
            hasBoss = hasBoss,
            bossConfigId = bossConfigId,
            bossSpawnAtElapsed = bossSpawnAtElapsed,
            requireBossDefeatToComplete = requireBossDefeatToComplete,
            pauseNormalSpawnsWhileBossAlive = pauseNormalSpawnsWhileBossAlive,
            eliteSpawnChance = eliteSpawnChance,
            specialSpawnChance = specialSpawnChance,
            specialEnemyConfigIds = specialEnemyConfigIds,
            difficulty = difficulty,
            waveDuration = waveDuration,
            spawnInterval = spawnInterval,
            maxSpawnCount = maxSpawnCount,
            statScalePerWave = statScalePerWave,
            rewardTableId = rewardTableId
        };
    }

    /// <summary>从内联 Boss 条目轮替选取。</summary>
    private static string PickFromInlineBossEntries(List<WaveBossEntry> entries, int waveIndex, int segmentStartWave)
    {
        List<WaveBossEntry> valid = new List<WaveBossEntry>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            WaveBossEntry entry = entries[i];
            if (entry != null && !string.IsNullOrEmpty(entry.BossConfigId))
            {
                valid.Add(entry);
            }
        }

        if (valid.Count == 0)
        {
            return null;
        }

        int offset = Mathf.Max(0, waveIndex - segmentStartWave);
        return valid[offset % valid.Count].BossConfigId;
    }
}
