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
                default:
                    Debug.LogWarning($"[SaveVersionMigrator] 未知版本 {version}，重置为默认存档。");
                    return SaveData.CreateDefault();
            }

            data.version = version;
        }

        EnsureCollections(data);
        return data;
    }

    private static void MigrateV0ToV1(SaveData data)
    {
        data.settings ??= SettingsData.CreateDefault();
        data.statistics ??= SaveStatisticsData.CreateDefault();
        data.runProgress ??= RunProgressData.CreateDefault();
        EnsureCollections(data);
    }

    private static void EnsureCollections(SaveData data)
    {
        data.permanentUpgrades ??= new List<ConfigIdIntPair>(4);
        data.talentLevels ??= new List<ConfigIdIntPair>(4);
        data.equipmentLevels ??= new List<ConfigIdIntPair>(4);
        data.skillLevels ??= new List<ConfigIdIntPair>(4);
        data.runProgress.activeBuffs ??= new List<ConfigIdIntPair>(4);
    }
}
