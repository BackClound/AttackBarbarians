using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 装备管理器：穿戴/卸下/强化、存档持久化，并将局外加成合并到 <see cref="PlayerRuntimeStats"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。由 <see cref="GameBootstrapper"/> 在 SaveManager 之后初始化。</para>
/// <para><b>推荐挂载对象：</b><c>GameSystems</c> 根物体或子物体。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、UI。</para>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;EquipmentManager&gt;()</c>。</para>
/// </remarks>
public class EquipmentManager : MonoBehaviour, IGameSystem
{
    private readonly List<StatModifierConfig> combinedModifiers = new List<StatModifierConfig>(32);
    private readonly List<StatModifierConfig> scratchModifiers = new List<StatModifierConfig>(8);
    private readonly Dictionary<string, int> setPieceCounts = new Dictionary<string, int>(4);
    private readonly List<string> appliedSetIds = new List<string>(4);

    private SaveManager saveManager;
    private ConfigManager configManager;
    private bool isInitialized;

    /// <summary>是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;

    /// <summary>注册存档与游戏事件，并尝试将装备加成应用到场景玩家。</summary>
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

    /// <summary>每帧更新（装备系统无逐帧逻辑）。</summary>
    /// <param name="deltaTime">距上一帧的秒数。</param>
    public void Tick(float deltaTime) { }

    /// <summary>取消事件订阅并重置初始化状态。</summary>
    public void Shutdown()
    {
        GameEvents.UnsubscribeSaveLoaded(OnSaveLoaded);
        GameEvents.UnsubscribeGameStarted(OnGameStarted);
        isInitialized = false;
    }

    /// <summary>获取指定部位当前穿戴的装备配置 ID。</summary>
    /// <param name="slot">装备部位。</param>
    /// <returns>装备配置 ID；未穿戴或存档未就绪时返回 <c>null</c>。</returns>
    public string GetEquippedAt(EquipmentSlot slot)
    {
        if (saveManager?.Current == null || slot == EquipmentSlot.None)
        {
            return null;
        }

        return EquipmentSlotSaveUtility.GetEquippedId(saveManager.Current.equippedItems, slot);
    }

    /// <summary>获取指定装备的强化等级。</summary>
    /// <param name="configId">装备配置 ID。</param>
    /// <returns>强化等级；无效 ID 或存档未就绪时返回 0。</returns>
    public int GetEnhanceLevel(string configId)
    {
        if (string.IsNullOrEmpty(configId) || saveManager?.Current == null)
        {
            return 0;
        }

        return saveManager.Current.GetEquipmentLevel(configId);
    }

    /// <summary>检查指定装备是否可穿戴。</summary>
    /// <param name="configId">装备配置 ID。</param>
    /// <param name="failureReason">失败原因；成功时为 <c>null</c>。</param>
    /// <returns>可穿戴返回 <c>true</c>。</returns>
    public bool CanEquip(string configId, out string failureReason)
    {
        failureReason = null;
        if (!isInitialized || saveManager?.Current == null)
        {
            failureReason = "存档未就绪";
            return false;
        }

        if (!TryResolveEquipment(configId, out EquipmentDataSO data))
        {
            failureReason = $"未找到装备配置: {configId}";
            return false;
        }

        if (data.Slot == EquipmentSlot.None)
        {
            failureReason = "装备未配置有效部位";
            return false;
        }

        return true;
    }

    /// <summary>尝试穿戴指定装备并刷新属性加成。</summary>
    /// <param name="configId">装备配置 ID。</param>
    /// <returns>穿戴成功返回 <c>true</c>。</returns>
    public bool TryEquip(string configId)
    {
        if (!CanEquip(configId, out string failureReason))
        {
            LogFailure(configId, failureReason);
            return false;
        }

        EquipmentDataSO data = null;
        TryResolveEquipment(configId, out data);
        EquipmentSlot slot = data.Slot;
        string previousId = GetEquippedAt(slot);

        saveManager.Current.SetEquippedAt(slot, configId);
        saveManager.MarkDirty();

        RebuildCombinedModifiers();
        TryApplyToScenePlayer();

        GameEvents.RaiseEquipmentChanged(
            this,
            new EquipmentChangedEventArgs(configId, slot, EquipmentChangeKind.Equipped));

        if (!string.IsNullOrEmpty(previousId) && previousId != configId)
        {
            GameEvents.RaiseEquipmentChanged(
                this,
                new EquipmentChangedEventArgs(previousId, slot, EquipmentChangeKind.Unequipped));
        }

        return true;
    }

    /// <summary>尝试卸下指定部位的装备。</summary>
    /// <param name="slot">装备部位。</param>
    /// <returns>卸下成功返回 <c>true</c>。</returns>
    public bool TryUnequip(EquipmentSlot slot)
    {
        if (!isInitialized || saveManager?.Current == null || slot == EquipmentSlot.None)
        {
            return false;
        }

        string previousId = GetEquippedAt(slot);
        if (string.IsNullOrEmpty(previousId))
        {
            return false;
        }

        saveManager.Current.SetEquippedAt(slot, null);
        saveManager.MarkDirty();

        RebuildCombinedModifiers();
        TryApplyToScenePlayer();

        GameEvents.RaiseEquipmentChanged(
            this,
            new EquipmentChangedEventArgs(previousId, slot, EquipmentChangeKind.Unequipped));
        return true;
    }

    /// <summary>检查指定装备是否可强化（须已穿戴且金币足够）。</summary>
    /// <param name="configId">装备配置 ID。</param>
    /// <param name="failureReason">失败原因；成功时为 <c>null</c>。</param>
    /// <returns>可强化返回 <c>true</c>。</returns>
    public bool CanEnhance(string configId, out string failureReason)
    {
        failureReason = null;
        if (!isInitialized || saveManager?.Current == null)
        {
            failureReason = "存档未就绪";
            return false;
        }

        if (!TryResolveEquipment(configId, out EquipmentDataSO data))
        {
            failureReason = $"未找到装备配置: {configId}";
            return false;
        }

        string equippedId = GetEquippedAt(data.Slot);
        if (equippedId != configId)
        {
            failureReason = "只能强化当前穿戴的装备";
            return false;
        }

        int current = GetEnhanceLevel(configId);
        if (current >= data.MaxEnhanceLevel)
        {
            failureReason = "已达最大强化等级";
            return false;
        }

        long cost = data.GetEnhanceCostForLevel(current + 1);
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

    /// <summary>尝试强化指定装备并扣除金币。</summary>
    /// <param name="configId">装备配置 ID。</param>
    /// <returns>强化成功返回 <c>true</c>。</returns>
    public bool TryEnhance(string configId)
    {
        if (!CanEnhance(configId, out string failureReason))
        {
            LogFailure(configId, failureReason);
            return false;
        }

        EquipmentDataSO data = null;
        TryResolveEquipment(configId, out data);
        int previous = GetEnhanceLevel(configId);
        int next = previous + 1;
        long cost = data.GetEnhanceCostForLevel(next);

        if (!ServiceLocator.TryGet(out ResourceManager resourceManager) &&
            !resourceManager.TrySpend(CurrencyType.Gold, cost, ResourceChangeReason.EquipmentEnhance, out string spendFailure))
        {
            LogFailure(configId, spendFailure);
            return false;
        }

        saveManager.Current.SetEquipmentLevel(configId, next);
        saveManager.MarkDirty();

        RebuildCombinedModifiers();
        TryApplyToScenePlayer();

        GameEvents.RaiseEquipmentChanged(
            this,
            new EquipmentChangedEventArgs(
                configId,
                data.Slot,
                EquipmentChangeKind.Enhanced,
                previous,
                next,
                cost));
        return true;
    }

    /// <summary>根据当前穿戴装备重建合并后的属性修正列表（含套装加成）。</summary>
    public void RebuildCombinedModifiers()
    {
        combinedModifiers.Clear();
        setPieceCounts.Clear();

        if (saveManager?.Current == null || configManager == null)
        {
            return;
        }

        List<EquipmentSlotSaveEntry> equipped = saveManager.Current.equippedItems;
        if (equipped == null || equipped.Count == 0)
        {
            return;
        }

        for (int i = 0; i < equipped.Count; i++)
        {
            EquipmentSlotSaveEntry entry = equipped[i];
            if (string.IsNullOrEmpty(entry.equipmentConfigId))
            {
                continue;
            }

            if (!configManager.TryGetEquipment(entry.equipmentConfigId, out EquipmentDataSO equipment))
            {
                continue;
            }

            int enhanceLevel = GetEnhanceLevel(entry.equipmentConfigId);
            float enhanceMult = equipment.GetEnhanceStatMultiplier(enhanceLevel);
            AppendModifiers(equipment.MainModifiers, enhanceMult);
            AppendModifiers(equipment.AffixModifiers, 1f);

            if (!string.IsNullOrWhiteSpace(equipment.SetId))
            {
                if (!setPieceCounts.TryGetValue(equipment.SetId, out int count))
                {
                    count = 0;
                }

                setPieceCounts[equipment.SetId] = count + 1;
            }
        }

        AppendActiveSetBonuses();
    }

    /// <summary>获取当前合并后的装备属性修正列表。</summary>
    /// <returns>只读属性修正列表。</returns>
    public IReadOnlyList<StatModifierConfig> GetCombinedModifiers() => combinedModifiers;

    /// <summary>若场景中存在玩家，则应用装备加成。</summary>
    public void TryApplyToScenePlayer()
    {
        if (!PlayerSceneAccess.TryGetController(out PlayerController controller))
        {
            return;
        }

        ApplyToPlayer(controller);
    }

    /// <summary>将装备加成应用到指定玩家控制器。</summary>
    /// <param name="controller">目标玩家控制器。</param>
    public void ApplyToPlayer(PlayerController controller)
    {
        if (controller == null)
        {
            return;
        }

        RebuildCombinedModifiers();
        controller.RuntimeStats.SetEquipmentModifiers(combinedModifiers);
        controller.RefreshEntityStats();
    }

    /// <summary>将满足件数要求的套装加成追加到合并列表。</summary>
    private void AppendActiveSetBonuses()
    {
        if (setPieceCounts.Count == 0)
        {
            return;
        }

        List<EquipmentSlotSaveEntry> equipped = saveManager.Current.equippedItems;
        appliedSetIds.Clear();

        for (int i = 0; i < equipped.Count; i++)
        {
            EquipmentSlotSaveEntry entry = equipped[i];
            if (string.IsNullOrEmpty(entry.equipmentConfigId))
            {
                continue;
            }

            if (!configManager.TryGetEquipment(entry.equipmentConfigId, out EquipmentDataSO equipment))
            {
                continue;
            }

            string setId = equipment.SetId;
            if (string.IsNullOrWhiteSpace(setId) || appliedSetIds.Contains(setId))
            {
                continue;
            }

            appliedSetIds.Add(setId);

            if (!setPieceCounts.TryGetValue(setId, out int pieceCount) ||
                pieceCount < equipment.SetPiecesRequired)
            {
                continue;
            }

            AppendModifiers(equipment.SetBonusModifiers, 1f);
        }
    }

    /// <summary>将属性修正按倍率缩放后追加到合并列表。</summary>
    /// <param name="sources">源属性修正列表。</param>
    /// <param name="valueMultiplier">数值倍率（强化等级影响）。</param>
    private void AppendModifiers(IReadOnlyList<StatModifierConfig> sources, float valueMultiplier)
    {
        if (sources == null || sources.Count == 0)
        {
            return;
        }

        scratchModifiers.Clear();
        for (int i = 0; i < sources.Count; i++)
        {
            StatModifierConfig source = sources[i];
            if (source == null)
            {
                continue;
            }

            float scaledValue = source.Value * valueMultiplier;
            scratchModifiers.Add(new StatModifierConfig(
                source.StatType,
                source.ModifierType,
                scaledValue,
                source.Order));
        }

        combinedModifiers.AddRange(scratchModifiers);
    }

    /// <summary>从配置管理器解析装备数据。</summary>
    /// <param name="configId">装备配置 ID。</param>
    /// <param name="data">解析到的装备配置。</param>
    /// <returns>解析成功返回 <c>true</c>。</returns>
    private bool TryResolveEquipment(string configId, out EquipmentDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetEquipment(configId, out data);
    }

    /// <summary>存档加载完成后重建装备加成并应用到玩家。</summary>
    /// <param name="context">游戏事件上下文。</param>
    private void OnSaveLoaded(GameEventContext context)
    {
        RebuildCombinedModifiers();
        TryApplyToScenePlayer();
    }

    /// <summary>游戏开始时尝试将装备加成应用到玩家。</summary>
    /// <param name="context">游戏事件上下文。</param>
    private void OnGameStarted(GameEventContext context) => TryApplyToScenePlayer();

    /// <summary>记录装备操作失败日志。</summary>
    /// <param name="configId">装备配置 ID。</param>
    /// <param name="failureReason">失败原因。</param>
    private void LogFailure(string configId, string failureReason)
    {
        if (!string.IsNullOrEmpty(failureReason) && configManager != null && configManager.ShouldLog())
        {
            Debug.LogWarning($"[EquipmentManager] {configId}: {failureReason}");
        }
    }

    /// <summary>调试：穿戴默认武器。</summary>
    [ContextMenu("Debug/Equip Default Weapon")]
    private void DebugEquipWeapon()
    {
        TryEquip(GameConstants.ConfigIds.EquipmentWeaponBattleAxe);
    }

    /// <summary>调试：强化已穿戴的默认武器。</summary>
    [ContextMenu("Debug/Enhance Equipped Weapon")]
    private void DebugEnhanceWeapon()
    {
        TryEnhance(GameConstants.ConfigIds.EquipmentWeaponBattleAxe);
    }
}
