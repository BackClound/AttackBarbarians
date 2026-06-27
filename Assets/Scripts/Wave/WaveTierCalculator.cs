using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 程序化波次规则纯函数：分档难度、池轮换、Boss 里程碑。
/// </summary>
public static class WaveTierCalculator
{
    /// <summary>0-based 难度档索引。</summary>
    public static int GetDifficultyTierIndex(int wave, WaveProceduralRulesSO rules)
    {
        if (rules == null)
        {
            return 0;
        }

        wave = Mathf.Max(1, wave);
        return (wave - 1) / rules.DifficultyTierWaveSize;
    }

    /// <summary>档内位置（0..tierSize-1）。</summary>
    public static int GetPositionInTier(int wave, WaveProceduralRulesSO rules)
    {
        if (rules == null)
        {
            return 0;
        }

        wave = Mathf.Max(1, wave);
        return (wave - 1) % rules.DifficultyTierWaveSize;
    }

    /// <summary>
    /// 计算完整难度快照：
    /// 档内小幅提升 + 跨档阶跃；移速在档内递增至上限后下一档重置。
    /// </summary>
    public static WaveDifficultySnapshot ComputeDifficultySnapshot(int wave, WaveProceduralRulesSO rules)
    {
        if (rules == null)
        {
            return WaveDifficultySnapshot.Identity;
        }

        wave = Mathf.Max(1, wave);
        int tierSize = rules.DifficultyTierWaveSize;
        int tier = GetDifficultyTierIndex(wave, rules);
        tier = Mathf.Min(tier, rules.MaxDifficultyTierIndex);
        int posInTier = GetPositionInTier(wave, rules);

        float tierFloor = rules.MaxDifficultyMultiplier *
                          (1f - Mathf.Exp(-rules.DifficultySaturationRate * tier));
        tierFloor = Mathf.Max(1f, tierFloor);

        float withinTierBonus = rules.WithinTierCombatIncrement * posInTier;
        float combat = Mathf.Min(rules.MaxDifficultyMultiplier, tierFloor + withinTierBonus);

        float speedProgress = tierSize <= 1 ? 0f : posInTier / (float)(tierSize - 1);
        float moveSpeed = Mathf.Lerp(1f, rules.MaxMoveSpeedMultiplierInTier, speedProgress);

        return new WaveDifficultySnapshot
        {
            TierIndex = tier,
            PositionInTier = posInTier,
            CombatMultiplier = combat,
            MoveSpeedMultiplier = moveSpeed,
            VisualTierIndex = tier
        };
    }

    /// <summary>兼容旧接口：战斗属性乘算。</summary>
    public static float GetTierDifficultyMultiplier(int wave, WaveProceduralRulesSO rules) =>
        ComputeDifficultySnapshot(wave, rules).CombatMultiplier;

    /// <summary>构建当前波次固定大小轮换敌人池。</summary>
    public static List<string> BuildUnlockedEnemyPool(int wave, WaveProceduralRulesSO rules) =>
        WavePoolRotator.BuildEnemyPool(wave, rules);

    /// <summary>本波是否应生成 Boss。</summary>
    public static bool ShouldSpawnBossThisWave(int wave, WaveProceduralRulesSO rules)
    {
        if (rules == null || rules.BossPool == null || !rules.BossPool.HasContent)
        {
            return false;
        }

        wave = Mathf.Max(1, wave);
        int interval = rules.BossMilestoneInterval;
        if (interval <= 0 || wave % interval != 0)
        {
            return false;
        }

        if (rules.GuaranteedBossOnDifficultyTierBoundary &&
            wave % rules.DifficultyTierWaveSize == 0)
        {
            return true;
        }

        int tier = GetDifficultyTierIndex(wave, rules);
        float chance = rules.BossMilestoneBaseChance + rules.BossMilestoneChancePerTier * tier;
        return SampleDeterministic(wave, 9013) < Mathf.Clamp01(chance);
    }

    /// <summary>本波 Boss 数量。</summary>
    public static int GetBossCount(int wave, WaveProceduralRulesSO rules)
    {
        if (rules == null || !ShouldSpawnBossThisWave(wave, rules))
        {
            return 0;
        }

        int tier = GetDifficultyTierIndex(wave, rules);
        int count = rules.BossBaseCount + rules.BossCountPerTier * tier;
        return Mathf.Clamp(count, 0, rules.BossCountMax);
    }

    /// <summary>单只 Boss 战斗属性乘算。</summary>
    public static float GetBossStatMultiplier(int wave, WaveProceduralRulesSO rules)
    {
        if (rules == null)
        {
            return 1f;
        }

        WaveDifficultySnapshot snapshot = ComputeDifficultySnapshot(wave, rules);
        float bossBonus = rules.BossBaseStatMultiplier + rules.BossStatMultiplierPerTier * snapshot.TierIndex;
        return Mathf.Min(snapshot.CombatMultiplier * bossBonus, rules.BossStatMultiplierMax);
    }

    /// <summary>构建本波 Boss 生成计划。</summary>
    public static List<WaveBossSpawnPlan> BuildBossSpawnPlans(int wave, WaveProceduralRulesSO rules)
    {
        List<WaveBossSpawnPlan> plans = new List<WaveBossSpawnPlan>(4);
        if (rules == null || rules.BossPool == null)
        {
            return plans;
        }

        int count = GetBossCount(wave, rules);
        if (count <= 0)
        {
            return plans;
        }

        float bossStatMult = GetBossStatMultiplier(wave, rules);
        float baseElapsed = rules.BossSpawnAtElapsed;
        float stagger = rules.BossSpawnStaggerSeconds;

        for (int i = 0; i < count; i++)
        {
            string bossId = WavePoolRotator.PickBossFromPool(wave, i, rules);
            if (string.IsNullOrEmpty(bossId))
            {
                continue;
            }

            plans.Add(new WaveBossSpawnPlan(bossId, bossStatMult, baseElapsed + stagger * i));
        }

        return plans;
    }

    /// <summary>精英怪概率。</summary>
    public static float GetEliteSpawnChance(int wave, WaveProceduralRulesSO rules)
    {
        if (rules == null)
        {
            return 0.08f;
        }

        int tier = GetDifficultyTierIndex(wave, rules);
        return Mathf.Min(
            rules.EliteSpawnChanceMax,
            rules.EliteSpawnChanceBase + rules.EliteSpawnChancePerTier * tier);
    }

    /// <summary>特殊怪概率。</summary>
    public static float GetSpecialSpawnChance(int wave, WaveProceduralRulesSO rules)
    {
        if (rules == null)
        {
            return 0.12f;
        }

        int tier = GetDifficultyTierIndex(wave, rules);
        return Mathf.Min(
            rules.SpecialSpawnChanceMax,
            rules.SpecialSpawnChanceBase + rules.SpecialSpawnChancePerTier * tier);
    }

    /// <summary>构建难度修正（战斗乘算）。</summary>
    public static WaveDifficultyOverride BuildDifficultyOverride(int wave, WaveProceduralRulesSO rules)
    {
        WaveDifficultySnapshot snapshot = ComputeDifficultySnapshot(wave, rules);
        return WaveDifficultyOverride.WithStatMultiplier(snapshot.CombatMultiplier);
    }

    /// <summary>波次种子随机采样 [0,1)。</summary>
    private static float SampleDeterministic(int wave, int salt)
    {
        Random.State previous = Random.state;
        Random.InitState(wave * 73856093 ^ salt);
        float value = Random.value;
        Random.state = previous;
        return value;
    }
}
