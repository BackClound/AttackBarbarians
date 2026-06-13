using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局内 Roguelike 升级管理器：负责单局三选一抽取、效果应用与已选升级持久化（与 Meta 天赋/装备互补）。
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

    /// <summary>是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前抽取到的候选升级选项。</summary>
    public IReadOnlyList<UpgradeOptionSO> CurrentChoices => currentChoices;
    /// <summary>最近一次解析到的奖励池配置 ID。</summary>
    public string LastResolvedPoolId => lastResolvedPoolId;

    /// <summary>初始化配置依赖并从存档恢复已选升级。</summary>
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

    /// <summary>每帧更新（升级系统无逐帧逻辑）。</summary>
    /// <param name="deltaTime">距上一帧的秒数。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消事件订阅并清空当前候选。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        currentChoices.Clear();
        isInitialized = false;
    }

    /// <summary>根据上下文从奖励池抽取不重复候选。</summary>
    /// <param name="context">升级抽取上下文。</param>
    /// <param name="choices">抽取到的候选列表。</param>
    /// <returns>抽取成功返回 <c>true</c>。</returns>
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
    /// <param name="option">选中的升级选项。</param>
    /// <param name="triggerSource">触发来源。</param>
    /// <returns>应用成功返回 <c>true</c>。</returns>
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

    /// <summary>获取指定升级选项的已选叠加层数。</summary>
    /// <param name="optionConfigId">升级选项配置 ID。</param>
    /// <returns>叠加层数；未选中时返回 0。</returns>
    public int GetStackCount(string optionConfigId)
    {
        if (string.IsNullOrWhiteSpace(optionConfigId))
        {
            return 0;
        }

        return selectedStacks.TryGetValue(optionConfigId, out int stacks) ? stacks : 0;
    }

    /// <summary>判断指定升级选项在当前上下文中是否可选。</summary>
    /// <param name="option">升级选项。</param>
    /// <param name="context">升级抽取上下文。</param>
    /// <returns>可选返回 <c>true</c>。</returns>
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

    /// <summary>游戏开始时重新应用已保存的升级效果。</summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnGameStarted(GameEventContext ctx)
    {
        ReapplySavedUpgrades();
    }

    /// <summary>判断技能类升级选项在当前状态下是否可用。</summary>
    /// <param name="option">升级选项。</param>
    /// <returns>可用返回 <c>true</c>。</returns>
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

    /// <summary>重新应用存档中已选升级的全部效果。</summary>
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

    /// <summary>尝试应用升级选项效果。</summary>
    /// <param name="option">升级选项。</param>
    /// <returns>应用成功返回 <c>true</c>。</returns>
    private bool TryApplyEffect(UpgradeOptionSO option)
    {
        return UpgradeApplicator.TryApply(option, ResolvePlayerSkillManager(), saveManager);
    }

    /// <summary>解析场景中的玩家技能管理器。</summary>
    /// <returns>玩家技能管理器；未找到时返回 <c>null</c>。</returns>
    private static PlayerSkillManager ResolvePlayerSkillManager()
    {
        if (Player.HasInstance && Player.Instance.skillManager != null)
        {
            return Player.Instance.skillManager;
        }

        return FindFirstObjectByType<PlayerSkillManager>();
    }

    /// <summary>根据过滤规则构建可抽取的候选条目列表。</summary>
    /// <param name="pool">奖励池配置。</param>
    /// <param name="context">升级抽取上下文。</param>
    /// <param name="destination">输出候选列表。</param>
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

    /// <summary>从候选列表中排除与已选选项互斥的条目。</summary>
    /// <param name="pool">奖励池配置。</param>
    /// <param name="picked">已选中的选项。</param>
    /// <param name="candidates">待过滤的候选列表。</param>
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

    /// <summary>按权重随机选取一个候选。</summary>
    /// <param name="candidates">候选列表。</param>
    /// <param name="picked">选中的候选。</param>
    /// <param name="pickedIndex">选中项在列表中的索引。</param>
    /// <returns>选取成功返回 <c>true</c>。</returns>
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

    /// <summary>根据稀有度计算最终抽取权重。</summary>
    /// <param name="option">升级选项。</param>
    /// <param name="baseWeight">基础权重。</param>
    /// <returns>有效权重。</returns>
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

    /// <summary>根据波次解析适用的奖励池。</summary>
    /// <param name="waveIndex">当前波次。</param>
    /// <param name="pool">解析到的奖励池。</param>
    /// <returns>解析成功返回 <c>true</c>。</returns>
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

    /// <summary>判断奖励池是否适用于指定波次。</summary>
    /// <param name="pool">奖励池配置。</param>
    /// <param name="waveIndex">当前波次。</param>
    /// <returns>适用返回 <c>true</c>。</returns>
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

    /// <summary>记录玩家选中升级并更新互斥组状态。</summary>
    /// <param name="option">选中的升级选项。</param>
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

    /// <summary>将选中升级写入局内存档。</summary>
    /// <param name="option">选中的升级选项。</param>
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

    /// <summary>从局内存档恢复已选升级记录。</summary>
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
