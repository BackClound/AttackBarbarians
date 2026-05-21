/// <summary>
/// 装备变更事件参数：穿戴、卸下或强化。
/// </summary>
public sealed class EquipmentChangedEventArgs
{
    public string EquipmentConfigId { get; }
    public EquipmentSlot Slot { get; }
    public EquipmentChangeKind ChangeKind { get; }
    public int PreviousEnhanceLevel { get; }
    public int NewEnhanceLevel { get; }
    public long GoldSpent { get; }

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
    Equipped = 0,
    Unequipped = 1,
    Enhanced = 2,
}
