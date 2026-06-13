using System;
using System.Collections.Generic;

/// <summary>
/// 存档中的装备槽位记录：部位 + 装备 configId。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。嵌套在 <see cref="SaveData.equippedItems"/> 列表中。</para>
/// </remarks>
[Serializable]
public struct EquipmentSlotSaveEntry
{
    /// <summary>装备槽位整型值（对应 <see cref="EquipmentSlot"/> 枚举）。</summary>
    public int slot;

    /// <summary>已装备物品的 configId。</summary>
    public string equipmentConfigId;

    /// <summary>
    /// 创建装备槽位存档条目。
    /// </summary>
    /// <param name="equipmentSlot">装备槽位枚举。</param>
    /// <param name="configId">已装备物品的 configId。</param>
    public EquipmentSlotSaveEntry(EquipmentSlot equipmentSlot, string configId)
    {
        slot = (int)equipmentSlot;
        equipmentConfigId = configId;
    }

    /// <summary>将槽位整型值转换为 <see cref="EquipmentSlot"/> 枚举。</summary>
    public EquipmentSlot EquipmentSlot => (EquipmentSlot)slot;
}

/// <summary>
/// <see cref="EquipmentSlotSaveEntry"/> 列表读写辅助。
/// </summary>
public static class EquipmentSlotSaveUtility
{
    /// <summary>
    /// 查询指定槽位当前装备的 configId。
    /// </summary>
    /// <param name="list">装备槽位存档列表。</param>
    /// <param name="slot">要查询的装备槽位。</param>
    /// <returns>已装备的 configId；未装备时返回 null。</returns>
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

    /// <summary>
    /// 设置指定槽位的装备 configId；传入空值时卸下装备。
    /// </summary>
    /// <param name="list">装备槽位存档列表。</param>
    /// <param name="slot">目标装备槽位。</param>
    /// <param name="equipmentConfigId">装备 configId；为空或 null 时移除该槽位记录。</param>
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
