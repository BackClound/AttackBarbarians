using System;
using System.Collections.Generic;

/// <summary>
/// 根存档数据结构：版本、资源、永久成长、技能、设置、统计与局内进度。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="SaveManager"/> 读写 JSON 文件。</para>
/// <para><b>序列化：</b>字段须为 public，供 <c>JsonUtility</c> 使用。</para>
/// <para><b>字段分组：</b></para>
/// <list type="bullet">
/// <item><description>元信息：<c>version</c>、<c>lastSavedUtcTicks</c></description></item>
/// <item><description>经济资源：<c>gold</c>、<c>diamonds</c>、<c>energy</c>、<c>adTickets</c>、<c>techPoints</c></description></item>
/// <item><description>成长数据：<c>talentLevels</c>、<c>equipmentLevels</c>、<c>skillLevels</c>、<c>upgradeCardInventory</c> 等</description></item>
/// <item><description>商店/成就：<c>shopPurchaseCounts</c>、<c>achievementProgress</c> 等</description></item>
/// <item><description>嵌套对象：<c>settings</c>、<c>statistics</c>、<c>runProgress</c></description></item>
/// </list>
/// <para>所有配置引用均使用 <c>configId</c> 字符串，不存 Unity 对象引用。</para>
/// </remarks>
[Serializable]
public class SaveData
{
    public int version;
    public long lastSavedUtcTicks;

    public long gold;
    public long diamonds;
    public int energy;
    public int maxEnergy;
    public long lastEnergyRecoverUtcTicks;
    public int adTickets;
    public long techPoints;

    public int dailyRewardStreak;
    public long lastDailyRewardClaimUtcTicks;
    public long lastFreeDiamondClaimUtcTicks;

    public List<ConfigIdIntPair> shopPurchaseCounts = new List<ConfigIdIntPair>(8);
    public List<ConfigIdLongPair> shopLastPurchaseUtcTicks = new List<ConfigIdLongPair>(8);

    public List<ConfigIdIntPair> achievementProgress = new List<ConfigIdIntPair>(16);
    public List<ConfigIdIntPair> achievementClaimed = new List<ConfigIdIntPair>(16);

    public List<ConfigIdIntPair> permanentUpgrades = new List<ConfigIdIntPair>(16);
    public List<ConfigIdIntPair> talentLevels = new List<ConfigIdIntPair>(16);
    public List<ConfigIdIntPair> equipmentLevels = new List<ConfigIdIntPair>(8);
    public List<EquipmentSlotSaveEntry> equippedItems = new List<EquipmentSlotSaveEntry>(6);
    public List<ConfigIdIntPair> skillLevels = new List<ConfigIdIntPair>(16);
    public List<ConfigIdIntPair> upgradeCardInventory = new List<ConfigIdIntPair>(32);
    public List<ConfigIdIntPair> buffUnlockCardInventory = new List<ConfigIdIntPair>(16);
    public List<ConfigIdIntPair> skillBuffUnlockProgress = new List<ConfigIdIntPair>(16);
    public List<ConfigIdIntPair> attributeBaseLevels = new List<ConfigIdIntPair>(8);

    public int onlinePlayTimeSeconds;
    public int accumulatedOfflineSeconds;
    public long lastSessionEndUtcTicks;
    public long lastOnlineRewardClaimUtcTicks;
    public long lastOfflineRewardClaimUtcTicks;
    public long lastLotteryUtcTicks;
    public long lastStageRewardClaimUtcTicks;

    public SettingsData settings = new SettingsData();
    public SaveStatisticsData statistics = new SaveStatisticsData();
    public RunProgressData runProgress = new RunProgressData();

    /// <summary>
    /// 创建包含默认值的完整存档数据。
    /// </summary>
    /// <returns>版本号为当前版本、资源与成长字段均已初始化的存档对象。</returns>
    public static SaveData CreateDefault()
    {
        return new SaveData
        {
            version = SaveConstants.CurrentVersion,
            lastSavedUtcTicks = DateTime.UtcNow.Ticks,
            gold = 0,
            diamonds = 0,
            energy = StaminaConstants.DefaultStartingStamina,
            maxEnergy = StaminaConstants.DefaultMaxStamina,
            lastEnergyRecoverUtcTicks = DateTime.UtcNow.Ticks,
            adTickets = 0,
            techPoints = 0,
            dailyRewardStreak = 0,
            lastDailyRewardClaimUtcTicks = 0,
            lastFreeDiamondClaimUtcTicks = 0,
            shopPurchaseCounts = new List<ConfigIdIntPair>(4),
            shopLastPurchaseUtcTicks = new List<ConfigIdLongPair>(4),
            achievementProgress = new List<ConfigIdIntPair>(4),
            achievementClaimed = new List<ConfigIdIntPair>(4),
            permanentUpgrades = new List<ConfigIdIntPair>(4),
            talentLevels = new List<ConfigIdIntPair>(4),
            equipmentLevels = new List<ConfigIdIntPair>(4),
            equippedItems = new List<EquipmentSlotSaveEntry>(4),
            skillLevels = new List<ConfigIdIntPair>(4),
            upgradeCardInventory = new List<ConfigIdIntPair>(4),
            attributeBaseLevels = new List<ConfigIdIntPair>(4),
            onlinePlayTimeSeconds = 0,
            accumulatedOfflineSeconds = 0,
            lastSessionEndUtcTicks = DateTime.UtcNow.Ticks,
            lastOnlineRewardClaimUtcTicks = 0,
            lastOfflineRewardClaimUtcTicks = 0,
            lastLotteryUtcTicks = 0,
            lastStageRewardClaimUtcTicks = 0,
            settings = SettingsData.CreateDefault(),
            statistics = SaveStatisticsData.CreateDefault(),
            runProgress = RunProgressData.CreateDefault(),
        };
    }

    /// <summary>读取永久升级等级。</summary>
    /// <param name="configId">升级项 configId。</param>
    /// <returns>对应等级；未记录时返回 0。</returns>
    public int GetPermanentUpgradeLevel(string configId) =>
        ConfigIdIntPairListUtility.GetValue(permanentUpgrades, configId);

    /// <summary>写入永久升级等级。</summary>
    /// <param name="configId">升级项 configId。</param>
    /// <param name="level">目标等级；为 0 时移除记录。</param>
    public void SetPermanentUpgradeLevel(string configId, int level) =>
        ConfigIdIntPairListUtility.SetValue(permanentUpgrades, configId, level);

    /// <summary>读取技能等级。</summary>
    /// <param name="configId">技能 configId。</param>
    /// <returns>技能等级；未记录时返回 0。</returns>
    public int GetSkillLevel(string configId) =>
        ConfigIdIntPairListUtility.GetValue(skillLevels, configId, 0);

    /// <summary>判断技能是否已解锁（射击技能默认解锁，其余需等级 &gt; 0）。</summary>
    /// <param name="configId">技能 configId。</param>
    /// <returns>已解锁时返回 true，否则返回 false。</returns>
    public bool IsSkillUnlocked(string configId) =>
        configId == GameConstants.ConfigIds.SkillShoot || GetSkillLevel(configId) > 0;

    /// <summary>写入技能等级。</summary>
    /// <param name="configId">技能 configId。</param>
    /// <param name="level">目标等级；为 0 时移除记录。</param>
    public void SetSkillLevel(string configId, int level) =>
        ConfigIdIntPairListUtility.SetValue(skillLevels, configId, level);

    /// <summary>读取升级卡持有数量。</summary>
    /// <param name="configId">升级卡 configId。</param>
    /// <returns>持有数量；未记录时返回 0。</returns>
    public int GetUpgradeCardCount(string configId) =>
        ConfigIdIntPairListUtility.GetValue(upgradeCardInventory, configId);

    /// <summary>写入升级卡持有数量。</summary>
    /// <param name="configId">升级卡 configId。</param>
    /// <param name="count">目标数量；为 0 时移除记录。</param>
    public void SetUpgradeCardCount(string configId, int count) =>
        ConfigIdIntPairListUtility.SetValue(upgradeCardInventory, configId, count);

    /// <summary>读取 Buff 解锁卡持有数量。</summary>
    public int GetBuffUnlockCardCount(string configId) =>
        ConfigIdIntPairListUtility.GetValue(buffUnlockCardInventory, configId);

    /// <summary>写入 Buff 解锁卡持有数量。</summary>
    public void SetBuffUnlockCardCount(string configId, int count) =>
        ConfigIdIntPairListUtility.SetValue(buffUnlockCardInventory, configId, count);

    /// <summary>读取技能 Buff 解锁路径进度（已解锁节点数）。</summary>
    public int GetSkillBuffUnlockProgress(string skillConfigId) =>
        ConfigIdIntPairListUtility.GetValue(skillBuffUnlockProgress, skillConfigId);

    /// <summary>写入技能 Buff 解锁路径进度。</summary>
    public void SetSkillBuffUnlockProgress(string skillConfigId, int unlockedNodeCount) =>
        ConfigIdIntPairListUtility.SetValue(skillBuffUnlockProgress, skillConfigId, unlockedNodeCount);

    /// <summary>读取属性基础等级。</summary>
    /// <param name="statType">属性类型。</param>
    /// <returns>基础等级；未记录时返回 0。</returns>
    public int GetAttributeBaseLevel(StatType statType) =>
        ConfigIdIntPairListUtility.GetValue(attributeBaseLevels, statType.ToString());

    /// <summary>写入属性基础等级。</summary>
    /// <param name="statType">属性类型。</param>
    /// <param name="level">目标等级；为 0 时移除记录。</param>
    public void SetAttributeBaseLevel(StatType statType, int level) =>
        ConfigIdIntPairListUtility.SetValue(attributeBaseLevels, statType.ToString(), level);

    /// <summary>读取天赋等级。</summary>
    /// <param name="configId">天赋 configId。</param>
    /// <returns>天赋等级；未记录时返回 0。</returns>
    public int GetTalentLevel(string configId) =>
        ConfigIdIntPairListUtility.GetValue(talentLevels, configId);

    /// <summary>写入天赋等级。</summary>
    /// <param name="configId">天赋 configId。</param>
    /// <param name="level">目标等级；为 0 时移除记录。</param>
    public void SetTalentLevel(string configId, int level) =>
        ConfigIdIntPairListUtility.SetValue(talentLevels, configId, level);

    /// <summary>读取装备强化等级。</summary>
    /// <param name="configId">装备 configId。</param>
    /// <returns>装备等级；未记录时返回 0。</returns>
    public int GetEquipmentLevel(string configId) =>
        ConfigIdIntPairListUtility.GetValue(equipmentLevels, configId);

    /// <summary>写入装备强化等级。</summary>
    /// <param name="configId">装备 configId。</param>
    /// <param name="level">目标等级；为 0 时移除记录。</param>
    public void SetEquipmentLevel(string configId, int level) =>
        ConfigIdIntPairListUtility.SetValue(equipmentLevels, configId, level);

    /// <summary>读取指定槽位已装备的 configId。</summary>
    /// <param name="slot">装备槽位。</param>
    /// <returns>已装备的 configId；未装备时返回 null。</returns>
    public string GetEquippedAt(EquipmentSlot slot) =>
        EquipmentSlotSaveUtility.GetEquippedId(equippedItems, slot);

    /// <summary>设置指定槽位的装备 configId。</summary>
    /// <param name="slot">装备槽位。</param>
    /// <param name="equipmentConfigId">装备 configId；为空时卸下。</param>
    public void SetEquippedAt(EquipmentSlot slot, string equipmentConfigId) =>
        EquipmentSlotSaveUtility.SetEquippedId(equippedItems, slot, equipmentConfigId);

    /// <summary>读取商店商品累计购买次数。</summary>
    /// <param name="configId">商店商品 configId。</param>
    /// <returns>累计购买次数；未记录时返回 0。</returns>
    public int GetShopPurchaseCount(string configId) =>
        ConfigIdIntPairListUtility.GetValue(shopPurchaseCounts, configId);

    /// <summary>写入商店商品累计购买次数。</summary>
    /// <param name="configId">商店商品 configId。</param>
    /// <param name="count">累计次数；为 0 时移除记录。</param>
    public void SetShopPurchaseCount(string configId, int count) =>
        ConfigIdIntPairListUtility.SetValue(shopPurchaseCounts, configId, count);

    /// <summary>
    /// 清空所有已解锁技能与永久 Buff，仅保留射击技能 Lv.1；同时清除局内进度中的 Buff/升级记录。
    /// </summary>
    public void ResetUnlockedSkillsAndBuffsKeepShootOnly()
    {
        skillLevels?.Clear();
        SetSkillLevel(GameConstants.ConfigIds.SkillShoot, 1);

        permanentUpgrades?.Clear();

        if (runProgress != null)
        {
            runProgress.ClearRun();
        }
    }
}
