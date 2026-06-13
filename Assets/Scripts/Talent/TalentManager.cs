using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 天赋管理器：解锁/升级、存档持久化，并将局外加成合并到 <see cref="PlayerRuntimeStats"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 SaveManager 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 根物体或子物体。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、UI。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;TalentManager&gt;()</c>。</para>
/// </remarks>
public class TalentManager : MonoBehaviour, IGameSystem
{
    private readonly List<StatModifierConfig> combinedModifiers = new List<StatModifierConfig>(32);
    private readonly List<StatModifierConfig> scratchModifiers = new List<StatModifierConfig>(8);

    private SaveManager saveManager;
    private ConfigManager configManager;
    private bool isInitialized;

    /// <summary>天赋服务是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// 初始化天赋系统：订阅存档与开局事件，并尝试应用到场景玩家。
    /// </summary>
    public void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        ServiceLocator.TryGet(out saveManager);
        ServiceLocator.TryGet(out configManager);

        GameEvents.SubscribeSaveLoaded(OnSaveLoaded);
        GameEvents.SubscribeGameStarted(OnGameStarted);
        isInitialized = true;
        TryApplyToScenePlayer();
    }

    /// <summary>
    /// 每帧更新（天赋系统无逐帧逻辑）。
    /// </summary>
    /// <param name="deltaTime">距上一帧的时间间隔（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭天赋系统并取消事件订阅。
    /// </summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        isInitialized = false;
    }

    /// <summary>
    /// 获取指定天赋的当前等级。
    /// </summary>
    /// <param name="configId">天赋配置 ID。</param>
    /// <returns>当前等级；未解锁或存档未就绪时返回 0。</returns>
    public int GetTalentLevel(string configId)
    {
        if (string.IsNullOrEmpty(configId) || saveManager?.Current == null)
        {
            return 0;
        }

        return saveManager.Current.GetTalentLevel(configId);
    }

    /// <summary>
    /// 校验指定天赋是否可升级。
    /// </summary>
    /// <param name="configId">天赋配置 ID。</param>
    /// <param name="failureReason">不可升级时的失败说明文案。</param>
    /// <returns>可升级时返回 true。</returns>
    public bool CanUpgrade(string configId, out string failureReason)
    {
        failureReason = null;
        if (!isInitialized || saveManager?.Current == null)
        {
            failureReason = "存档未就绪";
            return false;
        }

        if (!TryResolveTalent(configId, out TalentDataSO data))
        {
            failureReason = $"未找到天赋配置: {configId}";
            return false;
        }

        int current = GetTalentLevel(configId);
        if (current >= data.MaxLevel)
        {
            failureReason = "已达最大等级";
            return false;
        }

        if (!ArePrerequisitesMet(data, out failureReason))
        {
            return false;
        }

        long cost = data.GetUpgradeCostForLevel(current + 1);
        if (!ServiceLocator.TryGet(out ResourceManager resourceManager) ||
            !resourceManager.CanAfford(CurrencyType.Gold, cost))
        {
            long goldBalance = resourceManager != null
                ? resourceManager.GetAmount(CurrencyType.Gold)
                : saveManager.Gold;
            failureReason = ResourceManager.FormatInsufficientFunds(CurrencyType.Gold, cost, goldBalance);
            return false;
        }

        return true;
    }

    /// <summary>
    /// 尝试升级指定天赋（扣费、存档、重建修正并应用到玩家）。
    /// </summary>
    /// <param name="configId">天赋配置 ID。</param>
    /// <returns>升级成功时返回 true。</returns>
    public bool TryUpgrade(string configId)
    {
        if (!CanUpgrade(configId, out string failureReason))
        {
            if (!string.IsNullOrEmpty(failureReason) && configManager != null && configManager.ShouldLog())
            {
                Debug.LogWarning($"[TalentManager] 无法升级 {configId}: {failureReason}");
            }

            return false;
        }

        TalentDataSO data = null;
        TryResolveTalent(configId, out data);
        int previous = GetTalentLevel(configId);
        int next = previous + 1;
        long cost = data.GetUpgradeCostForLevel(next);

        if (!ServiceLocator.TryGet(out ResourceManager resourceManager) &&
            !resourceManager.TrySpend(CurrencyType.Gold, cost, ResourceChangeReason.TalentUpgrade, out string spendFailure))
        {
            if (!string.IsNullOrEmpty(spendFailure) && configManager != null && configManager.ShouldLog())
            {
                Debug.LogWarning($"[TalentManager] 扣费失败 {configId}: {spendFailure}");
            }

            return false;
        }

        saveManager.Current.SetTalentLevel(configId, next);
        saveManager.MarkDirty();

        RebuildCombinedModifiers();
        TryApplyToScenePlayer();

        GameEvents.RaiseTalentChanged(this, new TalentChangedEventArgs(configId, previous, next, cost));
        return true;
    }

    /// <summary>
    /// 根据存档中全部已解锁天赋重建合并后的属性修正列表。
    /// </summary>
    public void RebuildCombinedModifiers()
    {
        combinedModifiers.Clear();
        if (saveManager?.Current == null || configManager == null)
        {
            return;
        }

        List<ConfigIdIntPair> entries = saveManager.Current.talentLevels;
        if (entries == null || entries.Count == 0)
        {
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            ConfigIdIntPair entry = entries[i];
            if (entry.value <= 0 || string.IsNullOrEmpty(entry.configId))
            {
                continue;
            }

            if (!configManager.TryGetTalent(entry.configId, out TalentDataSO talent))
            {
                continue;
            }

            AppendModifiersForLevel(talent, entry.value);
        }
    }

    /// <summary>
    /// 获取全部天赋合并后的属性修正列表。
    /// </summary>
    /// <returns>合并后的属性修正只读列表。</returns>
    public IReadOnlyList<StatModifierConfig> GetCombinedModifiers() => combinedModifiers;

    /// <summary>
    /// 若场景中存在玩家控制器，则应用天赋修正。
    /// </summary>
    public void TryApplyToScenePlayer()
    {
        if (!PlayerSceneAccess.TryGetController(out PlayerController controller))
        {
            return;
        }

        ApplyToPlayer(controller);
    }

    /// <summary>
    /// 将天赋修正应用到指定玩家控制器。
    /// </summary>
    /// <param name="controller">目标玩家控制器。</param>
    public void ApplyToPlayer(PlayerController controller)
    {
        if (controller == null)
        {
            return;
        }

        RebuildCombinedModifiers();
        controller.RuntimeStats.SetTalentModifiers(combinedModifiers);
        controller.RefreshEntityStats();
    }

    /// <summary>
    /// 将指定天赋在给定等级下的属性修正追加到合并列表。
    /// </summary>
    /// <param name="talent">天赋配置。</param>
    /// <param name="level">天赋等级。</param>
    private void AppendModifiersForLevel(TalentDataSO talent, int level)
    {
        IReadOnlyList<StatModifierConfig> perLevel = talent.ModifiersPerLevel;
        if (perLevel == null || perLevel.Count == 0)
        {
            return;
        }

        scratchModifiers.Clear();
        for (int i = 0; i < perLevel.Count; i++)
        {
            StatModifierConfig source = perLevel[i];
            if (source == null)
            {
                continue;
            }

            float scaledValue = source.Value * level;
            scratchModifiers.Add(new StatModifierConfig(
                source.StatType,
                source.ModifierType,
                scaledValue,
                source.Order));
        }

        combinedModifiers.AddRange(scratchModifiers);
    }

    /// <summary>
    /// 校验天赋的前置天赋等级是否满足要求。
    /// </summary>
    /// <param name="data">天赋配置。</param>
    /// <param name="failureReason">不满足时的失败说明文案。</param>
    /// <returns>前置条件满足时返回 true。</returns>
    private bool ArePrerequisitesMet(TalentDataSO data, out string failureReason)
    {
        failureReason = null;
        IReadOnlyList<string> prerequisites = data.PrerequisiteTalentIds;
        if (prerequisites == null || prerequisites.Count == 0)
        {
            return true;
        }

        int required = data.PrerequisiteMinLevel;
        for (int i = 0; i < prerequisites.Count; i++)
        {
            string prerequisiteId = prerequisites[i];
            if (string.IsNullOrEmpty(prerequisiteId))
            {
                continue;
            }

            if (GetTalentLevel(prerequisiteId) < required)
            {
                failureReason = $"需要先升级前置天赋: {prerequisiteId} (≥{required})";
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 从配置管理器解析天赋配置。
    /// </summary>
    /// <param name="configId">天赋配置 ID。</param>
    /// <param name="data">找到的天赋配置。</param>
    /// <returns>解析成功时返回 true。</returns>
    private bool TryResolveTalent(string configId, out TalentDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetTalent(configId, out data);
    }

    /// <summary>
    /// 存档加载完成后重建修正并应用到场景玩家。
    /// </summary>
    /// <param name="context">游戏事件上下文。</param>
    private void OnSaveLoaded(GameEventContext context)
    {
        RebuildCombinedModifiers();
        TryApplyToScenePlayer();
    }

    /// <summary>
    /// 游戏开局时将天赋修正应用到场景玩家。
    /// </summary>
    /// <param name="context">游戏事件上下文。</param>
    private void OnGameStarted(GameEventContext context) => TryApplyToScenePlayer();

    /// <summary>
    /// 调试菜单：尝试升级最大生命值天赋。
    /// </summary>
    [ContextMenu("Debug/Upgrade Max Hp Talent")]
    private void DebugUpgradeMaxHp()
    {
        TryUpgrade(GameConstants.ConfigIds.TalentMaxHp);
    }
}
