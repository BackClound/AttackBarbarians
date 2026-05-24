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

    public bool IsInitialized => isInitialized;

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

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        isInitialized = false;
    }

    public int GetTalentLevel(string configId)
    {
        if (string.IsNullOrEmpty(configId) || saveManager?.Current == null)
        {
            return 0;
        }

        return saveManager.Current.GetTalentLevel(configId);
    }

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
        if (saveManager.Gold < cost)
        {
            failureReason = $"金币不足（需要 {cost}）";
            return false;
        }

        return true;
    }

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

        saveManager.Gold -= cost;
        saveManager.Current.SetTalentLevel(configId, next);
        saveManager.MarkDirty();

        RebuildCombinedModifiers();
        TryApplyToScenePlayer();

        GameEvents.RaiseTalentChanged(this, new TalentChangedEventArgs(configId, previous, next, cost));
        return true;
    }

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

    public IReadOnlyList<StatModifierConfig> GetCombinedModifiers() => combinedModifiers;

    public void TryApplyToScenePlayer()
    {
        if (!PlayerSceneAccess.TryGetController(out PlayerController controller))
        {
            return;
        }

        ApplyToPlayer(controller);
    }

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

    private bool TryResolveTalent(string configId, out TalentDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetTalent(configId, out data);
    }

    private void OnSaveLoaded(GameEventContext context)
    {
        RebuildCombinedModifiers();
        TryApplyToScenePlayer();
    }

    private void OnGameStarted(GameEventContext context) => TryApplyToScenePlayer();

    [ContextMenu("Debug/Upgrade Max Hp Talent")]
    private void DebugUpgradeMaxHp()
    {
        TryUpgrade(GameConstants.ConfigIds.TalentMaxHp);
    }
}
