using System;
using System.Collections.Generic;

/// <summary>
/// 存档中的装备槽位记录：部位 + 装备 configId。
/// </summary>
[Serializable]
public struct EquipmentSlotSaveEntry
{
    public int slot;
    public string equipmentConfigId;

    public EquipmentSlotSaveEntry(EquipmentSlot equipmentSlot, string configId)
    {
        slot = (int)equipmentSlot;
        equipmentConfigId = configId;
    }

    public EquipmentSlot EquipmentSlot => (EquipmentSlot)slot;
}

/// <summary>
/// <see cref="EquipmentSlotSaveEntry"/> 列表读写辅助。
/// </summary>
public static class EquipmentSlotSaveUtility
{
    public static string GetEquippedId(IReadOnlyList<EquipmentSlotSaveEntry> list, EquipmentSlot slot)
    {
        if (list == null)
        {
            return null;
        }

        int slotValue = (int)slot;
        for (int i = 0; i < list.Count; i++)
        {
            EquipmentSlotSaveEntry entry = list[i];
            if (entry.slot == slotValue && !string.IsNullOrEmpty(entry.equipmentConfigId))
            {
                return entry.equipmentConfigId;
            }
        }

        return null;
    }

    public static void SetEquippedId(List<EquipmentSlotSaveEntry> list, EquipmentSlot slot, string equipmentConfigId)
    {
        if (list == null)
        {
            return;
        }

        int slotValue = (int)slot;
        for (int i = 0; i < list.Count; i++)
        {
            EquipmentSlotSaveEntry entry = list[i];
            if (entry.slot != slotValue)
            {
                continue;
            }

            if (string.IsNullOrEmpty(equipmentConfigId))
            {
                list.RemoveAt(i);
            }
            else
            {
                list[i] = new EquipmentSlotSaveEntry(slot, equipmentConfigId);
            }

            return;
        }

        if (!string.IsNullOrEmpty(equipmentConfigId))
        {
            list.Add(new EquipmentSlotSaveEntry(slot, equipmentConfigId));
        }
    }
}
