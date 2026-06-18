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
    private readonly Dictionary<SkillBuffKind, int> skillBuffHighestTiers = new Dictionary<SkillBuffKind, int>(32);
    private readonly List<UpgradeRollCandidate> rollScratch = new List<UpgradeRollCandidate>(16);
    private readonly List<UpgradeRollCandidate> unlockRollScratch = new List<UpgradeRollCandidate>(8);
    private readonly List<UpgradeOptionSO> masterOptionScratch = new List<UpgradeOptionSO>(64);
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
    /// <summary>局内累计升级事件计数（读存档）。</summary>
    public int BuffEventCounter => GetBuffEventCounter();

    /// <summary>升级选单打开时累计一次升级事件（波次完成 / 玩家升级）。</summary>
    /// <param name="source">触发来源。</param>
    public void NotifyUpgradeSelectionOpened(UpgradeTriggerSource source)
    {
        if (source == UpgradeTriggerSource.Debug)
        {
            return;
        }

        RunProgressData run = saveManager?.Current?.runProgress;
        if (run == null)
        {
            return;
        }

        run.buffEventCounter++;
        saveManager.MarkDirty();
    }

    /// <summary>玩家确认选择后，若已达四循环阈值则扣减计数。</summary>
    public void ConsumeStatBuffCycleIfNeeded()
    {
        RunProgressData run = saveManager?.Current?.runProgress;
        if (run == null)
        {
            return;
        }

        if (run.buffEventCounter < GameConstants.Progression.StatBuffCycleThreshold)
        {
            return;
        }

        run.buffEventCounter -= GameConstants.Progression.StatBuffCycleThreshold;
        saveManager.MarkDirty();
    }

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
        GameEvents.SubscribeGameOver(OnGameOver);
        isInitialized = true;
    }

    /// <summary>每帧更新（升级系统无逐帧逻辑）。</summary>
    /// <param name="deltaTime">距上一帧的秒数。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消事件订阅并清空当前候选。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        GameEvents.UnsubscribeGameOver(OnGameOver);
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

        TryResolvePool(context.WaveIndex, out RewardPoolSO pool);
        lastResolvedPoolId = pool != null ? pool.ConfigId : defaultPoolConfigId;

        CollectMasterUpgradeOptions(pool, masterOptionScratch);
        if (masterOptionScratch.Count == 0)
        {
            Debug.LogWarning("[UpgradeManager] Buff 池为空：请配置 RewardPool 或 UpgradeOption 资产。");
            return false;
        }

        SkillManager skillManager = ResolvePlayerSkillManager()?.SkillManager;
        int runUnlockedCount = UpgradeRunPoolRules.CountRunUnlockedSkills(skillManager);
        SkillUnlockService unlockService = null;
        ServiceLocator.TryGet(out unlockService);
        SaveData save = saveManager?.Current;
        ConfigManager config = configManager;
        if (config == null)
        {
            ServiceLocator.TryGet(out config);
        }

        int metaUnlockedCount = MetaSkillBuffProgressResolver.CountMetaUnlockedSkills(unlockService, save);
        BuildRunEligiblePools(
            pool,
            context,
            skillManager,
            runUnlockedCount,
            metaUnlockedCount,
            unlockService,
            save,
            masterOptionScratch,
            rollScratch,
            unlockRollScratch);

        if (rollScratch.Count == 0 && unlockRollScratch.Count == 0)
        {
            Debug.LogWarning("[UpgradeManager] 过滤后无可用升级选项。");
            return false;
        }

        bool forceBasicAttributePool = ShouldForceBasicAttributeBuffPool();
        if (forceBasicAttributePool)
        {
            FilterToForcedStatCycleBuffsOnly(rollScratch);
            unlockRollScratch.Clear();

            if (rollScratch.Count == 0)
            {
                Debug.LogWarning("[UpgradeManager] 基础属性 Buff 池为空，重新收集基础属性候选。");
                BuildRunEligiblePools(
                    pool,
                    context,
                    skillManager,
                    runUnlockedCount,
                    metaUnlockedCount,
                    unlockService,
                    save,
                    masterOptionScratch,
                    rollScratch,
                    unlockRollScratch);
                FilterToForcedStatCycleBuffsOnly(rollScratch);
                unlockRollScratch.Clear();
            }
        }

        int choiceCount = pool != null ? pool.ChoiceCount : 3;
        bool requireUnlockCard = !forceBasicAttributePool &&
                                 runUnlockedCount < UpgradeRunPoolRules.RunSkillUnlockThreshold &&
                                 metaUnlockedCount < UpgradeRunPoolRules.RunSkillUnlockThreshold &&
                                 unlockRollScratch.Count > 0;
        RollChoices(
            pool,
            choiceCount,
            requireUnlockCard,
            rollScratch,
            unlockRollScratch);

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

        int effectiveMaxStacks = option.MaxStacks;
        if (UpgradeRunPoolRules.IsSkillBuffTierOption(option))
        {
            // 每个 tier 卡片只能选一次；更高 tier 需等前一档选中后才可进池。
            effectiveMaxStacks = 1;
        }

        if (GetStackCount(option.ConfigId) >= effectiveMaxStacks)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(option.MutuallyExclusiveGroup) &&
            activeMutualGroups.Contains(option.MutuallyExclusiveGroup))
        {
            return false;
        }

        if (!IsBaseSkillRulesSatisfied(option))
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

    /// <summary>新局开始：清空局内 Buff → 同步局外解锁 → 施加局外永久 Buff → 恢复本局已选升级。</summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnGameStarted(GameEventContext ctx)
    {
        SkillManager skillManager = ResolvePlayerSkillManager()?.SkillManager;
        skillManager?.ClearRunScopedBuffState();

        if (ServiceLocator.TryGet(out SkillUnlockService unlockService))
        {
            unlockService.RefreshMetaUnlocks();
            unlockService.SyncPlayerSkillManagerToMeta();
        }

        ApplyPersistentRunStartBuffs();

        RestoreFromSave();
        if (HasActiveRunSession())
        {
            ReapplySavedUpgrades();
        }
    }

    /// <summary>局结束时清空内存中的局内升级记录，避免泄漏到下一局。</summary>
    /// <param name="ctx">游戏事件上下文。</param>
    private void OnGameOver(GameEventContext ctx)
    {
        ClearRunSelectionState();
        ResolvePlayerSkillManager()?.SkillManager?.ClearRunScopedBuffState();
    }

    /// <summary>清空内存中的局内三选一记录（新局 / 局末 / BeginRun 时调用）。</summary>
    public void ClearRunSelectionState()
    {
        selectedStacks.Clear();
        activeMutualGroups.Clear();
        skillBuffHighestTiers.Clear();
        SeedMetaSkillBuffTiers();
        currentChoices.Clear();
    }

    private static void ApplyPersistentRunStartBuffs()
    {
        PlayerSkillManager playerSkills = ResolvePlayerSkillManager();
        if (playerSkills == null)
        {
            return;
        }

        if (ServiceLocator.TryGet(out SaveManager saveManager))
        {
            MetaProgressBuffBootstrap.TryApplyPermanentSkillBuffs(saveManager, playerSkills.BuffManager);
        }

        PlaytestBootstrap.TryApplyStartupBuffs();
    }

    /// <summary>基础技能规则（不含局内池范围过滤）。</summary>
    private static bool IsBaseSkillRulesSatisfied(UpgradeOptionSO option)
    {
        if (option == null)
        {
            return false;
        }

        if (option.EffectType == UpgradeEffectType.SkillUnlock)
        {
            if (string.IsNullOrWhiteSpace(option.SkillConfigId))
            {
                return false;
            }

            PlayerSkillManager skillManager = ResolvePlayerSkillManager();
            return skillManager?.SkillManager == null ||
                   !skillManager.SkillManager.IsSkillUnlocked(option.SkillConfigId);
        }

        if (UpgradeRunPoolRules.IsSkillExclusiveBuffOption(option))
        {
            PlayerSkillManager skillManager = ResolvePlayerSkillManager();
            SkillManager manager = skillManager?.SkillManager;
            if (manager == null)
            {
                return true;
            }

            SkillType target = SkillBuffCatalog.GetTargetSkill(option.SkillBuffKind);
            return target == SkillType.None || manager.IsSkillUnlocked(target);
        }

        return true;
    }

    /// <summary>收集主升级选项列表（奖励池条目或全库 UpgradeOption）。</summary>
    private void CollectMasterUpgradeOptions(RewardPoolSO pool, List<UpgradeOptionSO> destination)
    {
        destination.Clear();
        if (pool?.Entries != null && pool.Entries.Count > 0)
        {
            for (int i = 0; i < pool.Entries.Count; i++)
            {
                UpgradeOptionSO option = pool.Entries[i]?.Option;
                if (option != null && !destination.Contains(option))
                {
                    destination.Add(option);
                }
            }

            return;
        }

        if (configManager == null)
        {
            ServiceLocator.TryGet(out configManager);
        }

        IReadOnlyList<UpgradeOptionSO> fromDatabase = configManager?.Database?.UpgradeOptions;
        if (fromDatabase != null)
        {
            for (int i = 0; i < fromDatabase.Count; i++)
            {
                UpgradeOptionSO option = fromDatabase[i];
                if (option != null && !destination.Contains(option))
                {
                    destination.Add(option);
                }
            }
        }

        if (destination.Count > 0)
        {
            return;
        }

        UpgradeOptionSO[] loaded = Resources.LoadAll<UpgradeOptionSO>("Config/Upgrade");
        if (loaded == null)
        {
            return;
        }

        for (int i = 0; i < loaded.Length; i++)
        {
            UpgradeOptionSO option = loaded[i];
            if (option != null && !destination.Contains(option))
            {
                destination.Add(option);
            }
        }
    }

    /// <summary>按局内规则拆分 Buff 池与技能解锁卡池。</summary>
    private void BuildRunEligiblePools(
        RewardPoolSO pool,
        UpgradeSelectionContext context,
        SkillManager manager,
        int runUnlockedCount,
        int metaUnlockedCount,
        SkillUnlockService unlockService,
        SaveData save,
        IReadOnlyList<UpgradeOptionSO> masterOptions,
        List<UpgradeRollCandidate> buffDestination,
        List<UpgradeRollCandidate> unlockDestination)
    {
        buffDestination.Clear();
        unlockDestination.Clear();

        for (int i = 0; i < masterOptions.Count; i++)
        {
            UpgradeOptionSO option = masterOptions[i];
            if (!UpgradeRunPoolRules.IsEligibleForRunPool(
                    option,
                    context,
                    manager,
                    runUnlockedCount,
                    metaUnlockedCount,
                    unlockService,
                    save,
                    skillBuffHighestTiers,
                    IsOptionAvailable))
            {
                continue;
            }

            float weight = ResolveEntryWeight(pool, option);
            if (weight <= 0f)
            {
                continue;
            }

            var candidate = new UpgradeRollCandidate(option, weight);
            if (UpgradeRunPoolRules.IsSkillUnlockOption(option))
            {
                unlockDestination.Add(candidate);
            }
            else
            {
                buffDestination.Add(candidate);
            }
        }
    }

    /// <summary>解析选项在池中的有效权重。</summary>
    private static float ResolveEntryWeight(RewardPoolSO pool, UpgradeOptionSO option)
    {
        if (pool?.Entries != null)
        {
            for (int i = 0; i < pool.Entries.Count; i++)
            {
                RewardPoolEntryConfig entry = pool.Entries[i];
                if (entry?.Option == option)
                {
                    return GetEffectiveWeight(option, entry.ResolveWeight());
                }
            }
        }

        return GetEffectiveWeight(option, option.Weight);
    }

    /// <summary>抽取三选一；本局技能不足 3 个时保证含一张技能解锁卡。</summary>
    private void RollChoices(
        RewardPoolSO pool,
        int choiceCount,
        bool requireUnlockCard,
        List<UpgradeRollCandidate> buffCandidates,
        List<UpgradeRollCandidate> unlockCandidates)
    {
        currentChoices.Clear();
        var combined = new List<UpgradeRollCandidate>(buffCandidates.Count + unlockCandidates.Count);
        combined.AddRange(buffCandidates);
        combined.AddRange(unlockCandidates);

        int targetCount = Mathf.Clamp(choiceCount, 1, 5);

        if (requireUnlockCard && unlockCandidates.Count > 0)
        {
            if (TryPickWeighted(unlockCandidates, out UpgradeRollCandidate unlockPick, out int unlockIndex))
            {
                if (unlockPick.Option != null)
                {
                    currentChoices.Add(unlockPick.Option);
                }

                unlockCandidates.RemoveAt(unlockIndex);
                RemoveCandidateFromList(combined, unlockPick.Option);
                ExcludeMutualGroup(pool, unlockPick.Option, combined);
                ExcludeMutualGroup(pool, unlockPick.Option, buffCandidates);
            }
        }

        while (currentChoices.Count < targetCount && combined.Count > 0)
        {
            if (!TryPickWeighted(combined, out UpgradeRollCandidate picked, out int pickedIndex))
            {
                break;
            }

            if (picked.Option != null)
            {
                currentChoices.Add(picked.Option);
            }

            combined.RemoveAt(pickedIndex);
            RemoveCandidateByOption(buffCandidates, picked.Option);
            RemoveCandidateByOption(unlockCandidates, picked.Option);
            ExcludeMutualGroup(pool, picked.Option, combined);
            ExcludeMutualGroup(pool, picked.Option, buffCandidates);
            ExcludeMutualGroup(pool, picked.Option, unlockCandidates);
        }
    }

    private static void RemoveCandidateFromList(List<UpgradeRollCandidate> list, UpgradeOptionSO option)
    {
        if (list == null || option == null)
        {
            return;
        }

        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Option == option)
            {
                list.RemoveAt(i);
            }
        }
    }

    private static void RemoveCandidateByOption(List<UpgradeRollCandidate> list, UpgradeOptionSO option)
    {
        RemoveCandidateFromList(list, option);
    }

    private static void ExcludeMutualGroup(
        RewardPoolSO pool,
        UpgradeOptionSO picked,
        List<UpgradeRollCandidate> candidates)
    {
        if (candidates == null || picked == null || string.IsNullOrWhiteSpace(picked.MutuallyExclusiveGroup))
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

        if (pool?.BlockedMutualGroups == null)
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

    /// <summary>重新应用存档中已选升级的全部效果。</summary>
    private void ReapplySavedUpgrades()
    {
        if (!HasActiveRunSession() || selectedStacks.Count == 0)
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

        RecordSkillBuffTier(option);
    }

    /// <summary>记录 SkillBuffKind 已选的最高 tier，用于递进解锁下一档。</summary>
    private void RecordSkillBuffTier(UpgradeOptionSO option)
    {
        if (!UpgradeRunPoolRules.IsSkillBuffTierOption(option))
        {
            return;
        }

        SkillBuffKind kind = option.SkillBuffKind;
        int tier = option.SkillBuffTier;
        if (!skillBuffHighestTiers.TryGetValue(kind, out int existing) || tier > existing)
        {
            skillBuffHighestTiers[kind] = tier;
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
        ClearRunSelectionState();

        if (!HasActiveRunSession())
        {
            return;
        }

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
                configManager.TryGetUpgradeOption(pair.configId, out UpgradeOptionSO option))
            {
                if (!string.IsNullOrWhiteSpace(option.MutuallyExclusiveGroup))
                {
                    activeMutualGroups.Add(option.MutuallyExclusiveGroup);
                }

                for (int stack = 0; stack < pair.value; stack++)
                {
                    RecordSkillBuffTier(option);
                }
            }
        }
    }

    private bool HasActiveRunSession() => saveManager != null && saveManager.HasActiveRun;

    private int GetBuffEventCounter() =>
        saveManager?.Current?.runProgress?.buffEventCounter ?? 0;

    private bool ShouldForceBasicAttributeBuffPool() =>
        GetBuffEventCounter() >= GameConstants.Progression.StatBuffCycleThreshold;

    private static void FilterToForcedStatCycleBuffsOnly(List<UpgradeRollCandidate> candidates)
    {
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (!UpgradeRunPoolRules.IsForcedStatCycleBuffOption(candidates[i].Option))
            {
                candidates.RemoveAt(i);
            }
        }
    }

    /// <summary>将局外永久 SkillBuff tier 写入基线，供局内递进抽池使用。</summary>
    private void SeedMetaSkillBuffTiers()
    {
        ConfigManager config = configManager;
        if (config == null)
        {
            ServiceLocator.TryGet(out config);
        }

        GameConfig gameConfig = config?.GameConfig;
        MetaSkillBuffProgressResolver.MergeMetaHighestTiers(
            skillBuffHighestTiers,
            saveManager?.Current,
            config,
            gameConfig);
    }
}
