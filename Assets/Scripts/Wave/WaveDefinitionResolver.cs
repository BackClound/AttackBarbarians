using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 将 <see cref="WaveScheduleSO"/> 或 Legacy <see cref="WaveDataSO"/> 解析为运行时 <see cref="WaveSpawnProfile"/>。
/// </summary>
public static class WaveDefinitionResolver
{
    /// <summary>
    /// 从波次表解析指定波次的刷怪快照。
    /// </summary>
    public static bool TryResolveFromSchedule(
        int waveIndex,
        WaveScheduleSO schedule,
        out WaveSpawnProfile profile)
    {
        profile = null;
        if (schedule == null)
        {
            return false;
        }

        waveIndex = Mathf.Max(1, waveIndex);
        if (schedule.UseProceduralRules && schedule.ProceduralRules != null)
        {
            profile = BuildProceduralProfile(waveIndex, schedule.ProceduralRules);
            return profile != null;
        }

        WaveSegmentDefinition segment = schedule.ResolveSegmentForWave(waveIndex);
        profile = BuildProfile(waveIndex, segment);
        return profile != null;
    }

    /// <summary>
    /// Legacy：从逐波 WaveData 列表解析（waveIndex 超出时钳制到最后一条）。
    /// </summary>
    public static bool TryResolveFromLegacyWaveList(
        int waveIndex,
        string[] waveConfigIds,
        ConfigManager configManager,
        out WaveSpawnProfile profile)
    {
        profile = null;
        if (configManager == null)
        {
            return false;
        }

        waveIndex = Mathf.Max(1, waveIndex);
        WaveDataSO data = null;
        if (waveConfigIds != null && waveConfigIds.Length > 0)
        {
            int listIndex = Mathf.Clamp(waveIndex - 1, 0, waveConfigIds.Length - 1);
            configManager.TryGetWave(waveConfigIds[listIndex], out data);
        }

        if (data == null)
        {
            configManager.TryGetWave(GameConstants.ConfigIds.Wave01, out data);
        }

        if (data == null)
        {
            return false;
        }

        profile = WaveSpawnProfile.FromWaveData(data, waveIndex);
        return true;
    }

    /// <summary>
    /// 统一入口：优先波次表，回退 Legacy 列表。
    /// </summary>
    public static bool TryResolve(
        int waveIndex,
        WaveScheduleSO schedule,
        string[] legacyWaveConfigIds,
        ConfigManager configManager,
        out WaveSpawnProfile profile)
    {
        if (schedule != null && TryResolveFromSchedule(waveIndex, schedule, out profile))
        {
            return true;
        }

        return TryResolveFromLegacyWaveList(waveIndex, legacyWaveConfigIds, configManager, out profile);
    }

    /// <summary>由程序化规则构建运行时快照。</summary>
    private static WaveSpawnProfile BuildProceduralProfile(int waveIndex, WaveProceduralRulesSO rules)
    {
        List<string> enemyPool = WaveTierCalculator.BuildUnlockedEnemyPool(waveIndex, rules);
        List<WaveBossSpawnPlan> bossPlans = WaveTierCalculator.BuildBossSpawnPlans(waveIndex, rules);
        bool hasBoss = bossPlans.Count > 0;
        string primaryBossId = hasBoss ? bossPlans[0].BossConfigId : null;
        float bossElapsed = hasBoss ? bossPlans[0].SpawnAtElapsed : rules.BossSpawnAtElapsed;

        WaveDifficultySnapshot snapshot = WaveTierCalculator.ComputeDifficultySnapshot(waveIndex, rules);

        WaveSpawnProfile profile = new WaveSpawnProfile();
        profile.ApplyResolvedData(
            waveIndex,
            null,
            enemyPool,
            hasBoss,
            primaryBossId,
            bossElapsed,
            rules.RequireBossDefeatToComplete,
            rules.PauseNormalSpawnsWhileBossAlive,
            WaveTierCalculator.GetEliteSpawnChance(waveIndex, rules),
            WaveTierCalculator.GetSpecialSpawnChance(waveIndex, rules),
            rules.SpecialEnemyConfigIds,
            null,
            WaveDifficultyOverride.WithStatMultiplier(snapshot.CombatMultiplier),
            GameConstants.Progression.WaveDurationSeconds,
            1.5f,
            20,
            0.08f,
            bossPlans,
            snapshot);
        return profile;
    }

    /// <summary>由段定义构建运行时快照。</summary>
    private static WaveSpawnProfile BuildProfile(int waveIndex, WaveSegmentDefinition segment)
    {
        if (segment == null)
        {
            return new WaveSpawnProfile();
        }

        bool hasBoss = segment.ResolveHasBoss(waveIndex);
        string bossId = hasBoss ? segment.ResolveBossConfigId(waveIndex) : null;
        if (hasBoss && string.IsNullOrEmpty(bossId))
        {
            hasBoss = false;
        }

        WaveSpawnProfile profile = new WaveSpawnProfile();
        profile.ApplyResolvedData(
            waveIndex,
            segment.ResolveEnemyEntries(),
            segment.ResolveEnemyConfigIds(),
            hasBoss,
            bossId,
            segment.ResolveBossSpawnAtElapsed(),
            segment.ResolveRequireBossDefeatToComplete(),
            segment.ResolvePauseNormalSpawnsWhileBossAlive(),
            segment.EliteSpawnChance,
            segment.SpecialSpawnChance,
            segment.SpecialEnemyConfigIds,
            segment.RewardTableId,
            segment.Difficulty,
            segment.WaveDuration,
            segment.SpawnInterval,
            segment.MaxSpawnCount,
            segment.StatScalePerWave);
        return profile;
    }
}
