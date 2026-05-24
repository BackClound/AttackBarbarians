using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 升级系统核心：过滤候选、加权抽取、应用效果并记录局内已选升级。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 子物体，与 <see cref="RandomRewardManager"/> 同层级。</para>
/// </remarks>
public class UpgradeManager : MonoBehaviour, IGameSystem
{
    [Header("Config")]
    [SerializeField] private string defaultPoolConfigId = GameConstants.ConfigIds.RewardPoolDefault;

    private readonly Dictionary<string, int> selectedStacks = new Dictionary<string, int>(32);
    private readonly HashSet<string> activeMutualGroups = new HashSet<string>(16);
    private readonly List<UpgradeRollCandidate> rollScratch = new List<UpgradeRollCandidate>(16);
    private readonly List<UpgradeOptionSO> currentChoices = new List<UpgradeOptionSO>(5);

    private ConfigManager configManager;
    private SaveManager saveManager;
    private string lastResolvedPoolId = string.Empty;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public IReadOnlyList<UpgradeOptionSO> CurrentChoices => currentChoices;
    public string LastResolvedPoolId => lastResolvedPoolId;

    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        ServiceLocator.TryGet(out configManager);
        ServiceLocator.TryGet(out saveManager);
        RestoreFromSave();
        GameEvents.SubscribeGameStarted(OnGameStarted);
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        currentChoices.Clear();
        isInitialized = false;
    }

    /// <summary>根据上下文从奖励池抽取不重复候选。</summary>
    public bool TryRollChoices(UpgradeSelectionContext context, out IReadOnlyList<UpgradeOptionSO> choices)
    {
        choices = null;
        currentChoices.Clear();
        lastResolvedPoolId = string.Empty;

        if (!TryResolvePool(context.WaveIndex, out RewardPoolSO pool))
        {
            Debug.LogWarning("[UpgradeManager] 未找到可用奖励池。");
            return false;
        }

        lastResolvedPoolId = pool.ConfigId;
        BuildEligibleEntries(pool, context, rollScratch);
        if (rollScratch.Count == 0)
        {
            Debug.LogWarning("[UpgradeManager] 过滤后无可用升级选项。");
            return false;
        }

        int count = Mathf.Min(pool.ChoiceCount, rollScratch.Count);
        for (int i = 0; i < count; i++)
        {
            if (!TryPickWeighted(rollScratch, out UpgradeRollCandidate picked, out int pickedIndex))
            {
                break;
            }

            if (picked.Option != null)
            {
                currentChoices.Add(picked.Option);
            }

            rollScratch.RemoveAt(pickedIndex);
            ExcludeMutualGroup(pool, picked.Option, rollScratch);
        }

        choices = currentChoices;
        return currentChoices.Count > 0;
    }

    /// <summary>应用玩家选中的升级并写入存档。</summary>
    public bool TryApplyChoice(UpgradeOptionSO option, UpgradeTriggerSource triggerSource)
    {
        if (option == null)
        {
            return false;
        }

        if (!TryApplyEffect(option))
        {
            Debug.LogWarning($"[UpgradeManager] 应用升级失败: {option.ConfigId}");
            return false;
        }

        RecordSelection(option);
        PersistSelection(option);
        GameEvents.RaiseUpgradeChoiceApplied(this, new UpgradeChoiceAppliedPayload(
            option.ConfigId,
            GetStackCount(option.ConfigId),
            triggerSource));
        return true;
    }

    public int GetStackCount(string optionConfigId)
    {
        if (string.IsNullOrWhiteSpace(optionConfigId))
        {
            return 0;
        }

        return selectedStacks.TryGetValue(optionConfigId, out int stacks) ? stacks : 0;
    }

    public bool IsOptionAvailable(UpgradeOptionSO option, UpgradeSelectionContext context)
    {
        if (option == null)
        {
            return false;
        }

        if (context.WaveIndex < option.MinWave)
        {
            return false;
        }

        if (option.MaxWave > 0 && context.WaveIndex > option.MaxWave)
        {
            return false;
        }

        if (GetStackCount(option.ConfigId) >= option.MaxStacks)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(option.MutuallyExclusiveGroup) &&
            activeMutualGroups.Contains(option.MutuallyExclusiveGroup))
        {
            return false;
        }

        if (!IsSkillEffectOptionAvailable(option))
        {
            return false;
        }

        IReadOnlyList<string> prerequisites = option.PrerequisiteOptionIds;
        if (prerequisites != null)
        {
            for (int i = 0; i < prerequisites.Count; i++)
            {
                string requiredId = prerequisites[i];
                if (!string.IsNullOrWhiteSpace(requiredId) && GetStackCount(requiredId) <= 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void OnGameStarted(GameEventContext ctx)
    {
        ReapplySavedUpgrades();
    }

    private static bool IsSkillEffectOptionAvailable(UpgradeOptionSO option)
    {
        if (option == null)
        {
            return false;
        }

        PlayerSkillManager skillManager = ResolvePlayerSkillManager();
        SkillManager manager = skillManager?.SkillManager;

        switch (option.EffectType)
        {
            case UpgradeEffectType.SkillUnlock:
                if (string.IsNullOrWhiteSpace(option.SkillConfigId))
                {
                    return false;
                }

                if (ServiceLocator.TryGet(out SkillUnlockService unlockService) &&
                    !unlockService.IsMetaUnlocked(option.SkillConfigId))
                {
                    return false;
                }

                return manager == null || !manager.IsSkillUnlocked(option.SkillConfigId);

            case UpgradeEffectType.SkillBuff:
            case UpgradeEffectType.WeaponEnhance:
                if (manager == null)
                {
                    return true;
                }

                SkillType target = option.SkillBuffKind != SkillBuffKind.None
                    ? SkillBuffCatalog.GetTargetSkill(option.SkillBuffKind)
                    : SkillType.None;
                if (target == SkillType.None)
                {
                    return true;
                }

                return manager.IsSkillUnlocked(target);

            default:
                return true;
        }
    }

    private void ReapplySavedUpgrades()
    {
        if (selectedStacks.Count == 0)
        {
            return;
        }

        PlayerSkillManager skillManager = ResolvePlayerSkillManager();
        if (skillManager == null)
        {
            return;
        }

        foreach (KeyValuePair<string, int> pair in selectedStacks)
        {
            if (pair.Value <= 0 ||
                configManager == null ||
                !configManager.TryGetUpgradeOption(pair.Key, out UpgradeOptionSO option))
            {
                continue;
            }

            for (int stack = 0; stack < pair.Value; stack++)
            {
                UpgradeApplicator.TryApply(option, skillManager, saveManager);
            }
        }
    }

    private bool TryApplyEffect(UpgradeOptionSO option)
    {
        return UpgradeApplicator.TryApply(option, ResolvePlayerSkillManager(), saveManager);
    }

    private static PlayerSkillManager ResolvePlayerSkillManager()
    {
        if (Player.HasInstance && Player.Instance.skillManager != null)
        {
            return Player.Instance.skillManager;
        }

        return FindFirstObjectByType<PlayerSkillManager>();
    }

    private void BuildEligibleEntries(
        RewardPoolSO pool,
        UpgradeSelectionContext context,
        List<UpgradeRollCandidate> destination)
    {
        destination.Clear();
        IReadOnlyList<RewardPoolEntryConfig> entries = pool.Entries;
        if (entries == null)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            RewardPoolEntryConfig entry = entries[i];
            UpgradeOptionSO option = entry?.Option;
            if (option == null || !IsOptionAvailable(option, context))
            {
                continue;
            }

            float weight = GetEffectiveWeight(option, entry.ResolveWeight());
            if (weight <= 0f)
            {
                continue;
            }

            destination.Add(new UpgradeRollCandidate(option, weight));
        }
    }

    private static void ExcludeMutualGroup(
        RewardPoolSO pool,
        UpgradeOptionSO picked,
        List<UpgradeRollCandidate> candidates)
    {
        if (picked == null || string.IsNullOrWhiteSpace(picked.MutuallyExclusiveGroup))
        {
            return;
        }

        string group = picked.MutuallyExclusiveGroup;
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            UpgradeOptionSO candidate = candidates[i].Option;
            if (candidate != null && candidate.MutuallyExclusiveGroup == group)
            {
                candidates.RemoveAt(i);
            }
        }

        if (pool.BlockedMutualGroups == null)
        {
            return;
        }

        for (int g = 0; g < pool.BlockedMutualGroups.Count; g++)
        {
            string blocked = pool.BlockedMutualGroups[g];
            if (string.IsNullOrWhiteSpace(blocked) || blocked == group)
            {
                continue;
            }

            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                UpgradeOptionSO candidate = candidates[i].Option;
                if (candidate != null && candidate.MutuallyExclusiveGroup == blocked)
                {
                    candidates.RemoveAt(i);
                }
            }
        }
    }

    private static bool TryPickWeighted(
        List<UpgradeRollCandidate> candidates,
        out UpgradeRollCandidate picked,
        out int pickedIndex)
    {
        picked = default;
        pickedIndex = -1;
        if (candidates == null || candidates.Count == 0)
        {
            return false;
        }

        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += candidates[i].Weight;
        }

        if (totalWeight <= 0f)
        {
            pickedIndex = 0;
            picked = candidates[0];
            return picked.Option != null;
        }

        float roll = Random.Range(0f, totalWeight);
        float cursor = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cursor += candidates[i].Weight;
            if (roll <= cursor)
            {
                pickedIndex = i;
                picked = candidates[i];
                return picked.Option != null;
            }
        }

        pickedIndex = candidates.Count - 1;
        picked = candidates[pickedIndex];
        return picked.Option != null;
    }

    private static float GetEffectiveWeight(UpgradeOptionSO option, float baseWeight)
    {
        if (option == null)
        {
            return 0f;
        }

        float weight = Mathf.Max(1f, baseWeight);
        switch (option.Rarity)
        {
            case UpgradeRarity.Uncommon:
                weight *= 0.75f;
                break;
            case UpgradeRarity.Rare:
                weight *= 0.5f;
                break;
            case UpgradeRarity.Epic:
                weight *= 0.3f;
                break;
            case UpgradeRarity.Legendary:
                weight *= 0.15f;
                break;
        }

        return Mathf.Max(1f, weight);
    }

    private bool TryResolvePool(int waveIndex, out RewardPoolSO pool)
    {
        pool = null;
        if (configManager == null && !ServiceLocator.TryGet(out configManager))
        {
            return false;
        }

        if (configManager.TryGetRewardPool(defaultPoolConfigId, out pool) && IsPoolValidForWave(pool, waveIndex))
        {
            return true;
        }

        IReadOnlyList<RewardPoolSO> allPools = configManager.GetAllRewardPools();
        if (allPools == null)
        {
            return false;
        }

        for (int i = allPools.Count - 1; i >= 0; i--)
        {
            RewardPoolSO candidate = allPools[i];
            if (candidate != null && IsPoolValidForWave(candidate, waveIndex))
            {
                pool = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool IsPoolValidForWave(RewardPoolSO pool, int waveIndex)
    {
        if (pool == null)
        {
            return false;
        }

        if (waveIndex < pool.MinWave)
        {
            return false;
        }

        return pool.MaxWave <= 0 || waveIndex <= pool.MaxWave;
    }

    private void RecordSelection(UpgradeOptionSO option)
    {
        string id = option.ConfigId;
        if (!selectedStacks.ContainsKey(id))
        {
            selectedStacks[id] = 0;
        }

        selectedStacks[id]++;

        if (!string.IsNullOrWhiteSpace(option.MutuallyExclusiveGroup))
        {
            activeMutualGroups.Add(option.MutuallyExclusiveGroup);
        }
    }

    private void PersistSelection(UpgradeOptionSO option)
    {
        if (saveManager?.Current?.runProgress == null)
        {
            return;
        }

        List<ConfigIdIntPair> list = saveManager.Current.runProgress.selectedUpgrades;
        ConfigIdIntPairListUtility.SetValue(list, option.ConfigId, GetStackCount(option.ConfigId));
        saveManager.MarkDirty();
    }

    private void RestoreFromSave()
    {
        selectedStacks.Clear();
        activeMutualGroups.Clear();

        List<ConfigIdIntPair> saved = saveManager?.Current?.runProgress?.selectedUpgrades;
        if (saved == null)
        {
            return;
        }

        for (int i = 0; i < saved.Count; i++)
        {
            ConfigIdIntPair pair = saved[i];
            if (string.IsNullOrWhiteSpace(pair.configId) || pair.value <= 0)
            {
                continue;
            }

            selectedStacks[pair.configId] = pair.value;

            if (configManager != null &&
                configManager.TryGetUpgradeOption(pair.configId, out UpgradeOptionSO option) &&
                !string.IsNullOrWhiteSpace(option.MutuallyExclusiveGroup))
            {
                activeMutualGroups.Add(option.MutuallyExclusiveGroup);
            }
        }
    }
}
