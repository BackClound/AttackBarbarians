using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 存档版本迁移：将旧版 <see cref="SaveData"/> 升级到 <see cref="SaveConstants.CurrentVersion"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="SaveManager"/> 在加载后调用。</para>
/// </remarks>
public static class SaveVersionMigrator
{
    /// <summary>
    /// 将存档数据迁移到当前版本，补齐缺失字段与集合。
    /// </summary>
    /// <param name="data">待迁移的存档数据；为 null 时返回默认存档。</param>
    /// <returns>版本号已升级且集合已补齐的存档数据。</returns>
    public static SaveData Migrate(SaveData data)
    {
        if (data == null)
        {
            return SaveData.CreateDefault();
        }

        int version = data.version;
        if (version <= 0)
        {
            version = 0;
        }

        while (version < SaveConstants.CurrentVersion)
        {
            switch (version)
            {
                case 0:
                    MigrateV0ToV1(data);
                    version = 1;
                    break;
                case 1:
                    MigrateV1ToV2(data);
                    version = 2;
                    break;
                case 2:
                    MigrateV2ToV3(data);
                    version = 3;
                    break;
                case 3:
                    MigrateV3ToV4(data);
                    version = 4;
                    break;
                default:
                    Debug.LogWarning($"[SaveVersionMigrator] 未知版本 {version}，重置为默认存档。");
                    return SaveData.CreateDefault();
            }

            data.version = version;
        }

        EnsureCollections(data);
        return data;
    }

    /// <summary>
    /// 版本 0 → 1：补齐 settings、statistics、runProgress 等基础嵌套对象。
    /// </summary>
    /// <param name="data">待迁移的存档数据。</param>
    private static void MigrateV0ToV1(SaveData data)
    {
        data.settings ??= SettingsData.CreateDefault();
        data.statistics ??= SaveStatisticsData.CreateDefault();
        data.runProgress ??= RunProgressData.CreateDefault();
        EnsureCollections(data);
    }

    /// <summary>
    /// 版本 1 → 2：规范化广告券、体力上限与自然恢复时间戳。
    /// </summary>
    /// <param name="data">待迁移的存档数据。</param>
    private static void MigrateV1ToV2(SaveData data)
    {
        if (data.adTickets < 0)
        {
            data.adTickets = 0;
        }

        if (data.maxEnergy <= 0)
        {
            data.maxEnergy = StaminaConstants.DefaultMaxStamina;
        }

        if (data.energy <= 0)
        {
            data.energy = data.maxEnergy;
        }

        if (data.lastEnergyRecoverUtcTicks <= 0)
        {
            data.lastEnergyRecoverUtcTicks = DateTime.UtcNow.Ticks;
        }

        EnsureCollections(data);
    }

    /// <summary>
    /// 版本 2 → 3：补齐升级卡库存、属性基础等级与会话结束时间戳。
    /// </summary>
    /// <param name="data">待迁移的存档数据。</param>
    private static void MigrateV2ToV3(SaveData data)
    {
        data.upgradeCardInventory ??= new List<ConfigIdIntPair>(4);
        data.attributeBaseLevels ??= new List<ConfigIdIntPair>(4);

        if (data.lastSessionEndUtcTicks <= 0)
        {
            data.lastSessionEndUtcTicks = DateTime.UtcNow.Ticks;
        }

        EnsureCollections(data);
    }

    /// <summary>
    /// 版本 3 → 4：补齐 Buff 解锁卡库存与技能 Buff 解锁路径进度。
    /// </summary>
    private static void MigrateV3ToV4(SaveData data)
    {
        data.buffUnlockCardInventory ??= new List<ConfigIdIntPair>(4);
        data.skillBuffUnlockProgress ??= new List<ConfigIdIntPair>(4);
        EnsureCollections(data);
    }

    /// <summary>
    /// 确保所有 List 型字段与嵌套对象非 null，并修正默认音量。
    /// </summary>
    /// <param name="data">待补齐的存档数据。</param>
    private static void EnsureCollections(SaveData data)
    {
        data.settings ??= SettingsData.CreateDefault();
        data.statistics ??= SaveStatisticsData.CreateDefault();
        data.runProgress ??= RunProgressData.CreateDefault();
        data.permanentUpgrades ??= new List<ConfigIdIntPair>(4);
        data.talentLevels ??= new List<ConfigIdIntPair>(4);
        data.equipmentLevels ??= new List<ConfigIdIntPair>(4);
        data.equippedItems ??= new List<EquipmentSlotSaveEntry>(4);
        data.skillLevels ??= new List<ConfigIdIntPair>(4);
        data.upgradeCardInventory ??= new List<ConfigIdIntPair>(4);
        data.buffUnlockCardInventory ??= new List<ConfigIdIntPair>(4);
        data.skillBuffUnlockProgress ??= new List<ConfigIdIntPair>(4);
        data.attributeBaseLevels ??= new List<ConfigIdIntPair>(4);
        data.runProgress.activeBuffs ??= new List<ConfigIdIntPair>(4);
        data.shopPurchaseCounts ??= new List<ConfigIdIntPair>(4);
        data.shopLastPurchaseUtcTicks ??= new List<ConfigIdLongPair>(4);
        data.achievementProgress ??= new List<ConfigIdIntPair>(4);
        data.achievementClaimed ??= new List<ConfigIdIntPair>(4);

        if (data.settings != null && data.settings.uiVolume <= 0f)
        {
            data.settings.uiVolume = Mathf.Clamp01(data.settings.sfxVolume > 0f ? data.settings.sfxVolume : 1f);
        }
    }
}
