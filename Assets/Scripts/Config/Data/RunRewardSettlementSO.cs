using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局末奖励结算配置：最低奖励、每 5 分钟升一级、难度系数与每级随机浮动。
/// </summary>
/// <remarks>
/// <para><b>创建：</b>Attack Barbarians → Config → Run Reward Settlement。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Meta/</c></para>
/// <para><b>用途：</b>局末结算时由经济系统读取，计算金币与钻石奖励。</para>
/// </remarks>
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

    [Header("Upgrade Card")]
    [SerializeField] private string upgradeCardPoolConfigId = UpgradeCardConstants.PoolIds.RunSettlement;
    [SerializeField] private int upgradeCardDrawCount = 1;

    public float MinutesPerTier => Mathf.Max(0.5f, minutesPerTier);
    public string UpgradeCardPoolConfigId => upgradeCardPoolConfigId;
    public int UpgradeCardDrawCount => Mathf.Max(0, upgradeCardDrawCount);
    public long MinimumGold => long.Parse(Mathf.Max(0L, minimumGold).ToString());
    public long MinimumDiamonds => long.Parse(Mathf.Max(0L, minimumDiamonds).ToString());

    /// <summary>
    /// 根据单局游玩时长计算奖励档位索引。
    /// </summary>
    /// <param name="sessionDurationSeconds">本局累计游玩秒数。</param>
    /// <returns>奖励档位索引（从 0 开始，每档对应 minutesPerTier 分钟）。</returns>
    public int GetTierIndex(float sessionDurationSeconds)
    {
        float minutes = sessionDurationSeconds / 60f;
        return Mathf.Max(0, Mathf.FloorToInt(minutes / MinutesPerTier));
    }

    /// <summary>
    /// 根据游玩时长与难度计算局末金币与钻石奖励。
    /// </summary>
    /// <param name="sessionDurationSeconds">本局累计游玩秒数。</param>
    /// <param name="difficultyLevel">难度档位（0=简单, 1=普通, 2=困难）。</param>
    /// <param name="randomSeed">随机种子；-1 时使用当前 UTC 时间戳。</param>
    /// <returns>包含档位、金币与钻石数量的结算结果。</returns>
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

    /// <summary>
    /// 解析指定档位的奖励条目，超出配置表时按增长倍率外推。
    /// </summary>
    /// <param name="tier">奖励档位索引。</param>
    /// <returns>对应档位的奖励条目配置。</returns>
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

    /// <summary>
    /// 按难度档位获取奖励乘算系数。
    /// </summary>
    /// <param name="difficultyLevel">难度档位（0=简单, 1=普通, 2=困难）。</param>
    /// <returns>难度乘算系数；未配置时返回 1。</returns>
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

/// <summary>
/// 局末奖励单档配置：基础金币/钻石与随机浮动幅度。
/// </summary>
[Serializable]
public struct RunRewardTierEntry
{
    public int tierIndex;
    public long baseGold;
    public long baseDiamonds;
    [Range(0f, 0.5f)]
    public float variance;
}

/// <summary>
/// 局末奖励结算结果：档位索引与最终资源数量。
/// </summary>
public readonly struct RunRewardResult
{
    public int Tier { get; }
    public long Gold { get; }
    public long Diamonds { get; }

    /// <summary>
    /// 构造局末奖励结算结果。
    /// </summary>
    /// <param name="tier">奖励档位索引。</param>
    /// <param name="gold">结算金币数量。</param>
    /// <param name="diamonds">结算钻石数量。</param>
    public RunRewardResult(int tier, long gold, long diamonds)
    {
        Tier = tier;
        Gold = gold;
        Diamonds = diamonds;
    }
}
