using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 升级卡管理：库存、随机抽取、发放与通用卡解析。
/// </summary>
/// <remarks>
/// <para><b>挂载：</b><c>GameSystems</c>，由 <see cref="GameBootstrapper"/> 初始化。</para>
/// </remarks>
public class UpgradeCardManager : MonoBehaviour, IGameSystem
{
    private SaveManager saveManager;
    private ConfigManager configManager;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

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

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        isInitialized = false;
    }

    public int GetCardCount(string cardConfigId)
    {
        if (saveManager?.Current == null || string.IsNullOrWhiteSpace(cardConfigId))
        {
            return 0;
        }

        return ConfigIdIntPairListUtility.GetValue(saveManager.Current.upgradeCardInventory, cardConfigId);
    }

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

    public IReadOnlyList<UpgradeCardSO> GetAllUpgradeCards()
    {
        if (configManager?.Database?.UpgradeCards != null && configManager.Database.UpgradeCards.Count > 0)
        {
            return configManager.Database.UpgradeCards;
        }

        UpgradeCardSO[] loaded = Resources.LoadAll<UpgradeCardSO>("Config/UpgradeCard");
        return loaded != null && loaded.Length > 0 ? loaded : System.Array.Empty<UpgradeCardSO>();
    }

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

    public bool TryResolveCard(string cardConfigId, out UpgradeCardSO card)
    {
        card = null;
        if (configManager == null || string.IsNullOrWhiteSpace(cardConfigId))
        {
            return false;
        }

        return configManager.TryGetUpgradeCard(cardConfigId, out card);
    }

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

    private string ResolveGenericSkillCardId()
    {
        int index = Random.Range(0, UpgradeCardConstants.AllSkillConfigIds.Length);
        string skillId = UpgradeCardConstants.AllSkillConfigIds[index];
        return UpgradeCardConstants.GetSkillCardConfigId(skillId);
    }

    private string ResolveGenericAttributeCardId()
    {
        int index = Random.Range(0, UpgradeCardConstants.CoreAttributeStats.Length);
        StatType stat = UpgradeCardConstants.CoreAttributeStats[index];
        return UpgradeCardConstants.GetAttributeConfigId(stat);
    }

    private string GetDisplayName(string resolvedId, UpgradeCardSO drawnCard)
    {
        if (TryResolveCard(resolvedId, out UpgradeCardSO resolved))
        {
            return resolved.DisplayName;
        }

        return drawnCard != null ? drawnCard.DisplayName : resolvedId;
    }

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

    private static void AddWeight(Dictionary<string, int> weightByCardId, string cardConfigId, int weight)
    {
        if (string.IsNullOrWhiteSpace(cardConfigId) || weight <= 0)
        {
            return;
        }

        weightByCardId.TryGetValue(cardConfigId, out int current);
        weightByCardId[cardConfigId] = current + weight;
    }

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

    private static string[] BuildSkillCardIds()
    {
        var ids = new string[UpgradeCardConstants.AllSkillConfigIds.Length];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = UpgradeCardConstants.GetSkillCardConfigId(UpgradeCardConstants.AllSkillConfigIds[i]);
        }

        return ids;
    }

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
