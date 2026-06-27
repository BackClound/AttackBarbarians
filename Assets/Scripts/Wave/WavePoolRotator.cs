using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 固定大小敌人/Boss 池轮换：每 N 波加新成员并随机移除旧成员。
/// </summary>
public static class WavePoolRotator
{
    /// <summary>构建当前波次对应的固定大小敌人池。</summary>
    public static List<string> BuildEnemyPool(int wave, WaveProceduralRulesSO rules)
    {
        List<string> pool = new List<string>(8);
        if (rules == null)
        {
            return pool;
        }

        List<string> catalog = BuildEnemyCatalog(rules);
        if (catalog.Count == 0)
        {
            return pool;
        }

        int poolSize = Mathf.Clamp(rules.EnemyPoolSize, 1, catalog.Count);
        for (int i = 0; i < poolSize && i < catalog.Count; i++)
        {
            pool.Add(catalog[i]);
        }

        int rotationCount = GetRotationCount(wave, rules.EnemyUnlockWaveInterval);
        for (int r = 1; r <= rotationCount; r++)
        {
            ApplyRotation(pool, catalog, rules.EnemyUnlockOrder, r, rules);
        }

        return pool;
    }

    /// <summary>构建当前波次可用的 Boss configId 列表（固定大小轮换池）。</summary>
    public static List<string> BuildBossCatalog(WaveProceduralRulesSO rules)
    {
        List<string> catalog = new List<string>(8);
        if (rules?.BossPool == null)
        {
            return catalog;
        }

        IReadOnlyList<WaveBossEntry> entries = rules.BossPool.BossEntries;
        if (entries == null)
        {
            return catalog;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            WaveBossEntry entry = entries[i];
            if (entry != null && !string.IsNullOrEmpty(entry.BossConfigId) && !catalog.Contains(entry.BossConfigId))
            {
                catalog.Add(entry.BossConfigId);
            }
        }

        return catalog;
    }

    /// <summary>构建当前波次 Boss 轮换池。</summary>
    public static List<string> BuildBossPool(int wave, WaveProceduralRulesSO rules)
    {
        List<string> catalog = BuildBossCatalog(rules);
        List<string> pool = new List<string>(4);
        if (catalog.Count == 0 || rules == null)
        {
            return pool;
        }

        int poolSize = Mathf.Clamp(rules.BossActivePoolSize, 1, catalog.Count);
        for (int i = 0; i < poolSize; i++)
        {
            pool.Add(catalog[i]);
        }

        int rotationCount = GetRotationCount(wave, rules.EnemyUnlockWaveInterval);
        for (int r = 1; r <= rotationCount; r++)
        {
            ApplyBossRotation(pool, catalog, r, rules);
        }

        return pool;
    }

    /// <summary>从当前 Boss 轮换池中选取 configId。</summary>
    public static string PickBossFromPool(int wave, int pickIndex, WaveProceduralRulesSO rules)
    {
        List<string> pool = BuildBossPool(wave, rules);
        if (pool.Count == 0)
        {
            return rules?.BossPool?.PickBossConfigId(wave, 1);
        }

        int index = pickIndex % pool.Count;
        return pool[index];
    }

    /// <summary>已完成的全量解锁次数（每 30 波 +1）。</summary>
    private static int GetRotationCount(int wave, int interval)
    {
        wave = Mathf.Max(1, wave);
        interval = Mathf.Max(1, interval);
        return (wave - 1) / interval;
    }

    /// <summary>敌人总目录：基础池 + 解锁顺序。</summary>
    private static List<string> BuildEnemyCatalog(WaveProceduralRulesSO rules)
    {
        List<string> catalog = new List<string>(16);
        AppendUnique(catalog, rules.BaseEnemyConfigIds);
        AppendUnique(catalog, rules.EnemyUnlockOrder);
        return catalog;
    }

    /// <summary>执行一次池轮换。</summary>
    private static void ApplyRotation(
        List<string> pool,
        List<string> catalog,
        IReadOnlyList<string> unlockOrder,
        int rotationIndex,
        WaveProceduralRulesSO rules)
    {
        if (pool.Count == 0 || catalog.Count == 0)
        {
            return;
        }

        string newId = ResolveNewMemberId(catalog, unlockOrder, pool, rotationIndex, salt: 4111);
        if (string.IsNullOrEmpty(newId) || pool.Contains(newId))
        {
            newId = PickRandomNotInPool(catalog, pool, rotationIndex, salt: 5227);
        }

        if (string.IsNullOrEmpty(newId) || pool.Count == 0)
        {
            return;
        }

        int removeIndex = SampleIndex(pool.Count, rotationIndex, salt: 7123);
        pool.RemoveAt(removeIndex);

        if (!string.IsNullOrEmpty(newId) && !pool.Contains(newId))
        {
            pool.Add(newId);
        }
    }

    /// <summary>Boss 池轮换。</summary>
    private static void ApplyBossRotation(
        List<string> pool,
        List<string> catalog,
        int rotationIndex,
        WaveProceduralRulesSO rules)
    {
        if (pool.Count == 0 || catalog.Count == 0)
        {
            return;
        }

        string newId = ResolveNewMemberId(catalog, catalog, pool, rotationIndex, salt: 8831);
        if (string.IsNullOrEmpty(newId) || pool.Contains(newId))
        {
            newId = PickRandomNotInPool(catalog, pool, rotationIndex, salt: 9443);
        }

        if (string.IsNullOrEmpty(newId))
        {
            return;
        }

        int removeIndex = SampleIndex(pool.Count, rotationIndex, salt: 9127);
        pool.RemoveAt(removeIndex);

        if (!pool.Contains(newId))
        {
            pool.Add(newId);
        }
    }

    /// <summary>从 catalog 中选取不在 pool 内的 Id。</summary>
    private static string PickRandomNotInPool(
        List<string> catalog,
        List<string> pool,
        int rotationIndex,
        int salt)
    {
        List<string> candidates = new List<string>(catalog.Count);
        for (int i = 0; i < catalog.Count; i++)
        {
            string id = catalog[i];
            if (!string.IsNullOrEmpty(id) && !pool.Contains(id))
            {
                candidates.Add(id);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates[SampleIndex(candidates.Count, rotationIndex, salt)];
    }

    /// <summary>解析本次轮换应加入的成员 Id。</summary>
    private static string ResolveNewMemberId(
        List<string> catalog,
        IReadOnlyList<string> unlockOrder,
        List<string> currentPool,
        int rotationIndex,
        int salt)
    {
        if (unlockOrder != null && rotationIndex <= unlockOrder.Count)
        {
            string ordered = unlockOrder[rotationIndex - 1];
            if (!string.IsNullOrEmpty(ordered))
            {
                return ordered;
            }
        }

        List<string> candidates = new List<string>(catalog.Count);
        for (int i = 0; i < catalog.Count; i++)
        {
            string id = catalog[i];
            if (!string.IsNullOrEmpty(id) && !currentPool.Contains(id))
            {
                candidates.Add(id);
            }
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        int pick = SampleIndex(candidates.Count, rotationIndex, salt);
        return candidates[pick];
    }

    /// <summary>确定性下标采样。</summary>
    private static int SampleIndex(int count, int rotationIndex, int salt)
    {
        if (count <= 0)
        {
            return 0;
        }

        Random.State previous = Random.state;
        Random.InitState(rotationIndex * 19349663 ^ salt);
        int index = Random.Range(0, count);
        Random.state = previous;
        return index;
    }

    /// <summary>去重追加。</summary>
    private static void AppendUnique(List<string> target, IReadOnlyList<string> source)
    {
        if (source == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            string id = source[i];
            if (!string.IsNullOrEmpty(id) && !target.Contains(id))
            {
                target.Add(id);
            }
        }
    }
}
