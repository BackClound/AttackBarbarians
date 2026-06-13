/// <summary>
/// 装备变更事件参数：穿戴、卸下或强化。
/// </summary>
public sealed class EquipmentChangedEventArgs
{
    /// <summary>变更涉及的装备配置 ID。</summary>
    public string EquipmentConfigId { get; }
    /// <summary>变更涉及的装备部位。</summary>
    public EquipmentSlot Slot { get; }
    /// <summary>变更类型。</summary>
    public EquipmentChangeKind ChangeKind { get; }
    /// <summary>强化前的等级（非强化事件时为 0）。</summary>
    public int PreviousEnhanceLevel { get; }
    /// <summary>强化后的等级（非强化事件时为 0）。</summary>
    public int NewEnhanceLevel { get; }
    /// <summary>强化消耗的金币（非强化事件时为 0）。</summary>
    public long GoldSpent { get; }

    /// <summary>创建装备变更事件参数。</summary>
    /// <param name="equipmentConfigId">装备配置 ID。</param>
    /// <param name="slot">装备部位。</param>
    /// <param name="changeKind">变更类型。</param>
    /// <param name="previousEnhanceLevel">强化前等级。</param>
    /// <param name="newEnhanceLevel">强化后等级。</param>
    /// <param name="goldSpent">消耗金币。</param>
    public EquipmentChangedEventArgs(
        string equipmentConfigId,
        EquipmentSlot slot,
        EquipmentChangeKind changeKind,
        int previousEnhanceLevel = 0,
        int newEnhanceLevel = 0,
        long goldSpent = 0)
    {
        EquipmentConfigId = equipmentConfigId;
        Slot = slot;
        ChangeKind = changeKind;
        PreviousEnhanceLevel = previousEnhanceLevel;
        NewEnhanceLevel = newEnhanceLevel;
        GoldSpent = goldSpent;
    }
}

/// <summary>装备系统事件种类。</summary>
public enum EquipmentChangeKind
{
    /// <summary>穿戴装备。</summary>
    Equipped = 0,
    /// <summary>卸下装备。</summary>
    Unequipped = 1,
    /// <summary>强化装备。</summary>
    Enhanced = 2,
}
