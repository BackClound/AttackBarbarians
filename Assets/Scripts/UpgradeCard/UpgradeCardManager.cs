using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Meta 升级卡管理器：维护局外卡片库存，负责奖池抽取、发放与通用卡解析。
/// </summary>
/// <remarks>
/// <para><b>挂载：</b><c>GameSystems</c>，由 <see cref="GameBootstrapper"/> 初始化。</para>
/// </remarks>
public class UpgradeCardManager : MonoBehaviour, IGameSystem
{
    private SaveManager saveManager;
    private ConfigManager configManager;
    private bool isInitialized;

    /// <summary>是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>初始化存档与配置依赖。</summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out configManager);
        isInitialized = true;
    }

    /// <summary>每帧更新（升级卡系统无逐帧逻辑）。</summary>
    /// <param name="deltaTime">距上一帧的秒数。</param>
    public void Tick(float deltaTime) { }

    /// <summary>重置初始化状态。</summary>
    public void Shutdown()
    {
        isInitialized = false;
    }

    /// <summary>获取指定升级卡的库存数量。</summary>
    /// <param name="cardConfigId">卡片配置 ID。</param>
    /// <returns>库存数量。</returns>
    public int GetCardCount(string cardConfigId)
    {
        if (saveManager?.Current == null || string.IsNullOrWhiteSpace(cardConfigId))
        {
            return 0;
        }

        return ConfigIdIntPairListUtility.GetValue(saveManager.Current.upgradeCardInventory, cardConfigId);
    }

    /// <summary>向库存中添加指定数量的升级卡。</summary>
    /// <param name="cardConfigId">卡片配置 ID。</param>
    /// <param name="count">添加数量。</param>
    /// <returns>添加成功返回 <c>true</c>。</returns>
    public bool TryAddCard(string cardConfigId, int count)
    {
        if (!isInitialized || saveManager?.Current == null || string.IsNullOrWhiteSpace(cardConfigId) || count <= 0)
        {
            return false;
        }

        int current = GetCardCount(cardConfigId);
        ConfigIdIntPairListUtility.SetValue(saveManager.Current.upgradeCardInventory, cardConfigId, current + count);
        saveManager.MarkDirty();
        return true;
    }

    /// <summary>从指定奖池随机抽取并发放升级卡。</summary>
    /// <param name="poolConfigId">奖池配置 ID。</param>
    /// <param name="drawCount">抽取次数。</param>
    /// <param name="source">奖励来源。</param>
    /// <param name="grants">发放结果列表。</param>
    /// <returns>至少发放一张卡时返回 <c>true</c>。</returns>
    public bool TryGrantFromPool(
        string poolConfigId,
        int drawCount,
        UpgradeCardRewardSource source,
        out List<UpgradeCardGrantEntry> grants)
    {
        grants = new List<UpgradeCardGrantEntry>(drawCount);

        if (!isInitialized || saveManager?.Current == null || string.IsNullOrWhiteSpace(poolConfigId) || drawCount <= 0)
        {
            return false;
        }

        if (!TryResolvePool(poolConfigId, out UpgradeCardRewardPoolSO pool))
        {
            Debug.LogWarning($"[UpgradeCardManager] 未找到奖励池: {poolConfigId}");
            return false;
        }

        for (int i = 0; i < drawCount; i++)
        {
            if (!TryDrawCard(pool, out UpgradeCardSO card))
            {
                continue;
            }

            string resolvedId = ResolveCardConfigId(card);
            if (string.IsNullOrWhiteSpace(resolvedId))
            {
                continue;
            }

            if (!TryAddCard(resolvedId, 1))
            {
                continue;
            }

            grants.Add(new UpgradeCardGrantEntry(resolvedId, GetDisplayName(resolvedId, card), 1));
        }

        if (grants.Count == 0)
        {
            return false;
        }

        saveManager.SaveImmediate();
        GameEvents.RaiseUpgradeCardGranted(this, new UpgradeCardGrantedEventArgs(source, grants));

        if (configManager != null && configManager.ShouldLog())
        {
            Debug.Log($"[UpgradeCardManager] 发放升级卡 source={source} pool={poolConfigId} count={grants.Count}");
        }

        return true;
    }

    /// <summary>获取所有升级卡配置。</summary>
    /// <returns>升级卡配置列表。</returns>
    public IReadOnlyList<UpgradeCardSO> GetAllUpgradeCards()
    {
        if (configManager?.Database?.UpgradeCards != null && configManager.Database.UpgradeCards.Count > 0)
        {
            return configManager.Database.UpgradeCards;
        }

        UpgradeCardSO[] loaded = Resources.LoadAll<UpgradeCardSO>("Config/UpgradeCard");
        return loaded != null && loaded.Length > 0 ? loaded : System.Array.Empty<UpgradeCardSO>();
    }

    /// <summary>获取指定奖池的爆率预览条目（含通用卡展开）。</summary>
    /// <param name="poolConfigId">奖池配置 ID。</param>
    /// <returns>按权重降序排列的预览条目列表。</returns>
    public IReadOnlyList<UpgradeCardPoolPreviewEntry> GetPoolPreviewEntries(string poolConfigId)
    {
        var result = new List<UpgradeCardPoolPreviewEntry>(16);
        if (!TryResolvePool(poolConfigId, out UpgradeCardRewardPoolSO pool))
        {
            return result;
        }

        var weightByCardId = new Dictionary<string, int>(16);
        AccumulateExpandedPoolWeights(pool, weightByCardId);

        int totalWeight = 0;
        foreach (KeyValuePair<string, int> pair in weightByCardId)
        {
            totalWeight += pair.Value;
        }

        if (totalWeight <= 0)
        {
            return result;
        }

        foreach (KeyValuePair<string, int> pair in weightByCardId)
        {
            if (pair.Value <= 0)
            {
                continue;
            }

            TryResolveCard(pair.Key, out UpgradeCardSO card);
            float rate = pair.Value * 100f / totalWeight;
            result.Add(new UpgradeCardPoolPreviewEntry(pair.Key, card, pair.Value, rate));
        }

        result.Sort((a, b) =>
        {
            int weightCompare = b.Weight.CompareTo(a.Weight);
            return weightCompare != 0
                ? weightCompare
                : string.Compare(a.CardConfigId, b.CardConfigId, StringComparison.Ordinal);
        });

        return result;
    }

    /// <summary>解析指定 ID 的升级卡配置。</summary>
    /// <param name="cardConfigId">卡片配置 ID。</param>
    /// <param name="card">解析到的升级卡配置。</param>
    /// <returns>解析成功返回 <c>true</c>。</returns>
    public bool TryResolveCard(string cardConfigId, out UpgradeCardSO card)
    {
        card = null;
        if (configManager == null || string.IsNullOrWhiteSpace(cardConfigId))
        {
            return false;
        }

        return configManager.TryGetUpgradeCard(cardConfigId, out card);
    }

    /// <summary>解析指定 ID 的升级卡奖励池。</summary>
    /// <param name="poolConfigId">奖池配置 ID。</param>
    /// <param name="pool">解析到的奖池配置。</param>
    /// <returns>解析成功返回 <c>true</c>。</returns>
    private bool TryResolvePool(string poolConfigId, out UpgradeCardRewardPoolSO pool)
    {
        pool = null;
        if (configManager == null)
        {
            pool = Resources.Load<UpgradeCardRewardPoolSO>($"Config/UpgradeCard/Pools/{poolConfigId.Replace('.', '_')}");
            return pool != null;
        }

        return configManager.TryGetUpgradeCardRewardPool(poolConfigId, out pool);
    }

    /// <summary>从奖池按权重随机抽取一张升级卡。</summary>
    /// <param name="pool">奖池配置。</param>
    /// <param name="card">抽中的升级卡。</param>
    /// <returns>抽取成功返回 <c>true</c>。</returns>
    private bool TryDrawCard(UpgradeCardRewardPoolSO pool, out UpgradeCardSO card)
    {
        card = null;
        if (pool?.Entries == null || pool.Entries.Count == 0)
        {
            return false;
        }

        int totalWeight = 0;
        for (int i = 0; i < pool.Entries.Count; i++)
        {
            UpgradeCardPoolEntryConfig entry = pool.Entries[i];
            if (entry?.Card != null)
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0)
        {
            return false;
        }

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < pool.Entries.Count; i++)
        {
            UpgradeCardPoolEntryConfig entry = pool.Entries[i];
            if (entry?.Card == null)
            {
                continue;
            }

            cumulative += entry.Weight;
            if (roll < cumulative)
            {
                card = entry.Card;
                return true;
            }
        }

        return false;
    }

    /// <summary>将抽中的卡片解析为实际入库的配置 ID（通用卡随机展开）。</summary>
    /// <param name="card">抽中的升级卡。</param>
    /// <returns>实际入库的配置 ID。</returns>
    private string ResolveCardConfigId(UpgradeCardSO card)
    {
        if (card == null)
        {
            return string.Empty;
        }

        switch (card.Category)
        {
            case UpgradeCardCategory.GenericSkill:
                return ResolveGenericSkillCardId();

            case UpgradeCardCategory.GenericAttribute:
                return ResolveGenericAttributeCardId();

            default:
                return card.ConfigId;
        }
    }

    /// <summary>随机解析通用技能卡为具体技能升级卡 ID。</summary>
    /// <returns>技能升级卡配置 ID。</returns>
    private string ResolveGenericSkillCardId()
    {
        int index = Random.Range(0, UpgradeCardConstants.AllSkillConfigIds.Length);
        string skillId = UpgradeCardConstants.AllSkillConfigIds[index];
        return UpgradeCardConstants.GetSkillCardConfigId(skillId);
    }

    /// <summary>随机解析通用属性卡为具体属性升级卡 ID。</summary>
    /// <returns>属性升级卡配置 ID。</returns>
    private string ResolveGenericAttributeCardId()
    {
        int index = Random.Range(0, UpgradeCardConstants.CoreAttributeStats.Length);
        StatType stat = UpgradeCardConstants.CoreAttributeStats[index];
        return UpgradeCardConstants.GetAttributeConfigId(stat);
    }

    /// <summary>获取发放卡片的展示名称。</summary>
    /// <param name="resolvedId">解析后的配置 ID。</param>
    /// <param name="drawnCard">原始抽中的卡片。</param>
    /// <returns>展示名称。</returns>
    private string GetDisplayName(string resolvedId, UpgradeCardSO drawnCard)
    {
        if (TryResolveCard(resolvedId, out UpgradeCardSO resolved))
        {
            return resolved.DisplayName;
        }

        return drawnCard != null ? drawnCard.DisplayName : resolvedId;
    }

    /// <summary>累加奖池中各卡片（含通用卡展开）的有效权重。</summary>
    /// <param name="pool">奖池配置。</param>
    /// <param name="weightByCardId">卡片 ID 到权重的映射。</param>
    private static void AccumulateExpandedPoolWeights(
        UpgradeCardRewardPoolSO pool,
        Dictionary<string, int> weightByCardId)
    {
        if (pool?.Entries == null)
        {
            return;
        }

        for (int i = 0; i < pool.Entries.Count; i++)
        {
            UpgradeCardPoolEntryConfig entry = pool.Entries[i];
            UpgradeCardSO card = entry?.Card;
            if (card == null || entry.Weight <= 0)
            {
                continue;
            }

            switch (card.Category)
            {
                case UpgradeCardCategory.GenericSkill:
                    DistributeWeight(
                        weightByCardId,
                        BuildSkillCardIds(),
                        entry.Weight);
                    break;

                case UpgradeCardCategory.GenericAttribute:
                    DistributeWeight(
                        weightByCardId,
                        BuildAttributeCardIds(),
                        entry.Weight);
                    break;

                default:
                    AddWeight(weightByCardId, card.ConfigId, entry.Weight);
                    break;
            }
        }
    }

    /// <summary>向权重映射中累加指定卡片的权重。</summary>
    /// <param name="weightByCardId">卡片 ID 到权重的映射。</param>
    /// <param name="cardConfigId">卡片配置 ID。</param>
    /// <param name="weight">累加权重。</param>
    private static void AddWeight(Dictionary<string, int> weightByCardId, string cardConfigId, int weight)
    {
        if (string.IsNullOrWhiteSpace(cardConfigId) || weight <= 0)
        {
            return;
        }

        weightByCardId.TryGetValue(cardConfigId, out int current);
        weightByCardId[cardConfigId] = current + weight;
    }

    /// <summary>将总权重均分到多个卡片 ID 并累加到映射中。</summary>
    /// <param name="weightByCardId">卡片 ID 到权重的映射。</param>
    /// <param name="cardConfigIds">目标卡片 ID 数组。</param>
    /// <param name="totalWeight">待分配的总权重。</param>
    private static void DistributeWeight(Dictionary<string, int> weightByCardId, string[] cardConfigIds, int totalWeight)
    {
        if (cardConfigIds == null || cardConfigIds.Length == 0 || totalWeight <= 0)
        {
            return;
        }

        int baseShare = totalWeight / cardConfigIds.Length;
        int remainder = totalWeight % cardConfigIds.Length;
        for (int i = 0; i < cardConfigIds.Length; i++)
        {
            int share = baseShare + (i < remainder ? 1 : 0);
            AddWeight(weightByCardId, cardConfigIds[i], share);
        }
    }

    /// <summary>构建所有技能升级卡配置 ID 数组。</summary>
    /// <returns>技能升级卡 ID 数组。</returns>
    private static string[] BuildSkillCardIds()
    {
        var ids = new string[UpgradeCardConstants.AllSkillConfigIds.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = UpgradeCardConstants.GetSkillCardConfigId(UpgradeCardConstants.AllSkillConfigIds[i]);
        }

        return ids;
    }

    /// <summary>构建所有属性升级卡配置 ID 数组。</summary>
    /// <returns>属性升级卡 ID 数组。</returns>
    private static string[] BuildAttributeCardIds()
    {
        var ids = new string[UpgradeCardConstants.CoreAttributeStats.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = UpgradeCardConstants.GetAttributeConfigId(UpgradeCardConstants.CoreAttributeStats[i]);
        }

        return ids;
    }
}
