using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局末奖励结算配置：最低奖励、每 5 分钟升一级、难度系数与每级随机浮动。
/// </summary>
[CreateAssetMenu(fileName = "RunRewardSettlement", menuName = "Attack Barbarians/Config/Run Reward Settlement")]
public class RunRewardSettlementSO : ScriptableObject
{
    [Header("Tier")]
    [Tooltip("每档奖励对应的游玩分钟数（默认 5 分钟升一级）。")]
    [SerializeField] private float minutesPerTier = 5f;
    [SerializeField] private long minimumGold = 30;
    [SerializeField] private long minimumDiamonds;

    [Header("Difficulty")]
    [Tooltip("索引 0=简单, 1=普通, 2=困难；超出范围时使用最后一项。")]
    [SerializeField] private float[] difficultyMultipliers = { 0.85f, 1f, 1.2f };

    [Header("Tiers")]
    [SerializeField]
    private List<RunRewardTierEntry> tiers = new List<RunRewardTierEntry>
    {
        new RunRewardTierEntry { tierIndex = 0, baseGold = 50, baseDiamonds = 0, variance = 0.08f },
        new RunRewardTierEntry { tierIndex = 1, baseGold = 80, baseDiamonds = 1, variance = 0.1f },
        new RunRewardTierEntry { tierIndex = 2, baseGold = 120, baseDiamonds = 2, variance = 0.1f },
        new RunRewardTierEntry { tierIndex = 3, baseGold = 170, baseDiamonds = 3, variance = 0.12f },
    };

    [Header("Extrapolation")]
    [Tooltip("超出配置表最高档时，每多一档金币倍率。")]
    [SerializeField] private float goldGrowthPerExtraTier = 1.15f;
    [SerializeField] private float diamondGrowthPerExtraTier = 1.1f;

    public float MinutesPerTier => Mathf.Max(0.5f, minutesPerTier);
    public long MinimumGold => long.Parse(Mathf.Max(0L, minimumGold).ToString());
    public long MinimumDiamonds => long.Parse(Mathf.Max(0L, minimumDiamonds).ToString());

    public int GetTierIndex(float sessionDurationSeconds)
    {
        float minutes = sessionDurationSeconds / 60f;
        return Mathf.Max(0, Mathf.FloorToInt(minutes / MinutesPerTier));
    }

    public RunRewardResult Calculate(float sessionDurationSeconds, int difficultyLevel, int randomSeed = -1)
    {
        int tier = GetTierIndex(sessionDurationSeconds);
        RunRewardTierEntry entry = ResolveTierEntry(tier);
        float difficultyMult = GetDifficultyMultiplier(difficultyLevel);

        System.Random rng = randomSeed >= 0
            ? new System.Random(randomSeed)
            : new System.Random(unchecked((int)DateTime.UtcNow.Ticks));

        float variance = Mathf.Clamp01(entry.variance);
        float goldRoll = 1f + (float)(rng.NextDouble() * 2.0 - 1.0) * variance;
        float diamondRoll = 1f + (float)(rng.NextDouble() * 2.0 - 1.0) * variance;

        long gold = (long)Mathf.Max(
            MinimumGold,
            Mathf.Round(entry.baseGold * difficultyMult * goldRoll));

        long diamonds = (long)Mathf.Max(
            MinimumDiamonds,
            Mathf.Round(entry.baseDiamonds * difficultyMult * diamondRoll));

        return new RunRewardResult(tier, gold, diamonds);
    }

    private RunRewardTierEntry ResolveTierEntry(int tier)
    {
        if (tiers == null || tiers.Count == 0)
        {
            return new RunRewardTierEntry { baseGold = MinimumGold, variance = 0.05f };
        }

        RunRewardTierEntry best = tiers[0];
        for (int i = 0; i < tiers.Count; i++)
        {
            if (tiers[i].tierIndex <= tier)
            {
                best = tiers[i];
            }
        }

        int maxConfiguredTier = tiers[tiers.Count - 1].tierIndex;
        if (tier <= maxConfiguredTier)
        {
            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].tierIndex == tier)
                {
                    return tiers[i];
                }
            }

            return best;
        }

        int extra = tier - maxConfiguredTier;
        float goldMult = Mathf.Pow(goldGrowthPerExtraTier, extra);
        float diamondMult = Mathf.Pow(diamondGrowthPerExtraTier, extra);
        return new RunRewardTierEntry
        {
            tierIndex = tier,
            baseGold = long.Parse((best.baseGold * goldMult).ToString()),
            baseDiamonds = long.Parse((best.baseDiamonds * diamondMult).ToString()),
            variance = best.variance,
        };
    }

    private float GetDifficultyMultiplier(int difficultyLevel)
    {
        if (difficultyMultipliers == null || difficultyMultipliers.Length == 0)
        {
            return 1f;
        }

        int index = Mathf.Clamp(difficultyLevel, 0, difficultyMultipliers.Length - 1);
        return Mathf.Max(0.1f, difficultyMultipliers[index]);
    }
}

[Serializable]
public struct RunRewardTierEntry
{
    public int tierIndex;
    public long baseGold;
    public long baseDiamonds;
    [Range(0f, 0.5f)]
    public float variance;
}

public readonly struct RunRewardResult
{
    public int Tier { get; }
    public long Gold { get; }
    public long Diamonds { get; }

    public RunRewardResult(int tier, long gold, long diamonds)
    {
        Tier = tier;
        Gold = gold;
        Diamonds = diamonds;
    }
}
