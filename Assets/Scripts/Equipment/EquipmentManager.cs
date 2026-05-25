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

    public string GetEquippedAt(EquipmentSlot slot)
    {
        if (saveManager?.Current == null || slot == EquipmentSlot.None)
        {
            return null;
        }

        return EquipmentSlotSaveUtility.GetEquippedId(saveManager.Current.equippedItems, slot);
    }

    public int GetEnhanceLevel(string configId)
    {
        if (string.IsNullOrEmpty(configId) || saveManager?.Current == null)
        {
            return 0;
        }

        return saveManager.Current.GetEquipmentLevel(configId);
    }

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
        controller.RuntimeStats.SetEquipmentModifiers(combinedModifiers);
        controller.RefreshEntityStats();
    }

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

    private bool TryResolveEquipment(string configId, out EquipmentDataSO data)
    {
        data = null;
        return configManager != null && configManager.TryGetEquipment(configId, out data);
    }

    private void OnSaveLoaded(GameEventContext context)
    {
        RebuildCombinedModifiers();
        TryApplyToScenePlayer();
    }

    private void OnGameStarted(GameEventContext context) => TryApplyToScenePlayer();

    private void LogFailure(string configId, string failureReason)
    {
        if (!string.IsNullOrEmpty(failureReason) && configManager != null && configManager.ShouldLog())
        {
            Debug.LogWarning($"[EquipmentManager] {configId}: {failureReason}");
        }
    }

    [ContextMenu("Debug/Equip Default Weapon")]
    private void DebugEquipWeapon()
    {
        TryEquip(GameConstants.ConfigIds.EquipmentWeaponBattleAxe);
    }

    [ContextMenu("Debug/Enhance Equipped Weapon")]
    private void DebugEnhanceWeapon()
    {
        TryEnhance(GameConstants.ConfigIds.EquipmentWeaponBattleAxe);
    }
}
