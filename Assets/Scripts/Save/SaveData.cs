using System;
using System.Collections.Generic;

/// <summary>
/// 根存档数据结构：版本、资源、永久成长、技能、设置、统计与局内进度。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="SaveManager"/> 读写 JSON 文件。</para>
/// <para><b>序列化：</b>字段须为 public，供 <c>JsonUtility</c> 使用；引用类型仅使用 configId 字符串。</para>
/// </remarks>
[Serializable]
public class SaveData
{
    public int version;
    public long lastSavedUtcTicks;

    public long gold;
    public long diamonds;

    public int dailyRewardStreak;
    public long lastDailyRewardClaimUtcTicks;
    public long lastFreeDiamondClaimUtcTicks;

    public List<ConfigIdIntPair> shopPurchaseCounts = new List<ConfigIdIntPair>(8);
    public List<ConfigIdLongPair> shopLastPurchaseUtcTicks = new List<ConfigIdLongPair>(8);

    public List<ConfigIdIntPair> permanentUpgrades = new List<ConfigIdIntPair>(16);
    public List<ConfigIdIntPair> talentLevels = new List<ConfigIdIntPair>(16);
    public List<ConfigIdIntPair> equipmentLevels = new List<ConfigIdIntPair>(8);
    public List<EquipmentSlotSaveEntry> equippedItems = new List<EquipmentSlotSaveEntry>(6);
    public List<ConfigIdIntPair> skillLevels = new List<ConfigIdIntPair>(16);

    public SettingsData settings = new SettingsData();
    public SaveStatisticsData statistics = new SaveStatisticsData();
    public RunProgressData runProgress = new RunProgressData();

    public static SaveData CreateDefault()
    {
        return new SaveData
        {
            version = SaveConstants.CurrentVersion,
            lastSavedUtcTicks = DateTime.UtcNow.Ticks,
            gold = 0,
            diamonds = 0,
            dailyRewardStreak = 0,
            lastDailyRewardClaimUtcTicks = 0,
            lastFreeDiamondClaimUtcTicks = 0,
            shopPurchaseCounts = new List<ConfigIdIntPair>(4),
            shopLastPurchaseUtcTicks = new List<ConfigIdLongPair>(4),
            permanentUpgrades = new List<ConfigIdIntPair>(4),
            talentLevels = new List<ConfigIdIntPair>(4),
            equipmentLevels = new List<ConfigIdIntPair>(4),
            equippedItems = new List<EquipmentSlotSaveEntry>(4),
            skillLevels = new List<ConfigIdIntPair>(4),
            settings = SettingsData.CreateDefault(),
            statistics = SaveStatisticsData.CreateDefault(),
            runProgress = RunProgressData.CreateDefault(),
        };
    }

    public int GetPermanentUpgradeLevel(string configId) =>
        ConfigIdIntPairListUtility.GetValue(permanentUpgrades, configId);

    public void SetPermanentUpgradeLevel(string configId, int level) =>
        ConfigIdIntPairListUtility.SetValue(permanentUpgrades, configId, level);

    public int GetSkillLevel(string configId) =>
        ConfigIdIntPairListUtility.GetValue(skillLevels, configId, 0);

    public bool IsSkillUnlocked(string configId) =>
        configId == GameConstants.ConfigIds.SkillShoot || GetSkillLevel(configId) > 0;

    public void SetSkillLevel(string configId, int level) =>
        ConfigIdIntPairListUtility.SetValue(skillLevels, configId, level);

    public int GetTalentLevel(string configId) =>
        ConfigIdIntPairListUtility.GetValue(talentLevels, configId);

    public void SetTalentLevel(string configId, int level) =>
        ConfigIdIntPairListUtility.SetValue(talentLevels, configId, level);

    public int GetEquipmentLevel(string configId) =>
        ConfigIdIntPairListUtility.GetValue(equipmentLevels, configId);

    public void SetEquipmentLevel(string configId, int level) =>
        ConfigIdIntPairListUtility.SetValue(equipmentLevels, configId, level);

    public string GetEquippedAt(EquipmentSlot slot) =>
        EquipmentSlotSaveUtility.GetEquippedId(equippedItems, slot);

    public void SetEquippedAt(EquipmentSlot slot, string equipmentConfigId) =>
        EquipmentSlotSaveUtility.SetEquippedId(equippedItems, slot, equipmentConfigId);

    public int GetShopPurchaseCount(string configId) =>
        ConfigIdIntPairListUtility.GetValue(shopPurchaseCounts, configId);

    public void SetShopPurchaseCount(string configId, int count) =>
        ConfigIdIntPairListUtility.SetValue(shopPurchaseCounts, configId, count);
}
