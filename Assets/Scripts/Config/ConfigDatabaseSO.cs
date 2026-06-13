using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 配置总表：集中引用各类 Data SO，供 <see cref="ConfigManager"/> 构建索引。
/// </summary>
/// <remarks>
/// <para><b>创建：</b>Attack Barbarians → Config → Config Database。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/ConfigDatabase.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "ConfigDatabase", menuName = "Attack Barbarians/Config/Config Database")]
public class ConfigDatabaseSO : ScriptableObject
{
    [SerializeField] private List<PlayerDataSO> players = new List<PlayerDataSO>();
    [SerializeField] private List<EnemyDataSO> enemies = new List<EnemyDataSO>();
    [SerializeField] private List<SkillDataSO> skills = new List<SkillDataSO>();
    [SerializeField] private SkillUnlockTableSO skillUnlockTable;
    [SerializeField] private List<BuffDataSO> buffs = new List<BuffDataSO>();
    [SerializeField] private List<WaveDataSO> waves = new List<WaveDataSO>();
    [SerializeField] private List<BossDataSO> bosses = new List<BossDataSO>();
    [SerializeField] private List<BossSkillDataSO> bossSkills = new List<BossSkillDataSO>();
    [SerializeField] private List<SpecialEnemyAbilityDataSO> specialEnemyAbilities = new List<SpecialEnemyAbilityDataSO>();
    [SerializeField] private List<DropTableSO> dropTables = new List<DropTableSO>();
    [SerializeField] private List<AutoAttackDataSO> autoAttacks = new List<AutoAttackDataSO>();
    [SerializeField] private List<UpgradeOptionSO> upgradeOptions = new List<UpgradeOptionSO>();
    [SerializeField] private List<RewardPoolSO> rewardPools = new List<RewardPoolSO>();
    [SerializeField] private List<TalentDataSO> talents = new List<TalentDataSO>();
    [SerializeField] private List<EquipmentDataSO> equipment = new List<EquipmentDataSO>();
    [SerializeField] private List<MapDataSO> maps = new List<MapDataSO>();
    [SerializeField] private List<GameplayEventDataSO> gameplayEvents = new List<GameplayEventDataSO>();
    [SerializeField] private List<ShopItemSO> shopItems = new List<ShopItemSO>();
    [SerializeField] private List<AchievementDataSO> achievements = new List<AchievementDataSO>();
    [SerializeField] private List<DailyRewardEntrySO> dailyRewardEntries = new List<DailyRewardEntrySO>();
    [SerializeField] private List<UpgradeCardSO> upgradeCards = new List<UpgradeCardSO>();
    [SerializeField] private List<UpgradeCardRewardPoolSO> upgradeCardRewardPools = new List<UpgradeCardRewardPoolSO>();

    /// <summary>所有玩家配置条目列表。</summary>
    public IReadOnlyList<PlayerDataSO> Players => players;
    /// <summary>所有敌人配置条目列表。</summary>
    public IReadOnlyList<EnemyDataSO> Enemies => enemies;
    /// <summary>所有技能配置条目列表。</summary>
    public IReadOnlyList<SkillDataSO> Skills => skills;
    /// <summary>技能累计时长解锁表。</summary>
    public SkillUnlockTableSO SkillUnlockTable => skillUnlockTable;
    /// <summary>所有 Buff 配置条目列表。</summary>
    public IReadOnlyList<BuffDataSO> Buffs => buffs;
    /// <summary>所有波次配置条目列表。</summary>
    public IReadOnlyList<WaveDataSO> Waves => waves;
    /// <summary>所有 Boss 配置条目列表。</summary>
    public IReadOnlyList<BossDataSO> Bosses => bosses;
    /// <summary>所有 Boss 技能配置条目列表。</summary>
    public IReadOnlyList<BossSkillDataSO> BossSkills => bossSkills;
    /// <summary>所有特殊敌人能力配置条目列表。</summary>
    public IReadOnlyList<SpecialEnemyAbilityDataSO> SpecialEnemyAbilities => specialEnemyAbilities;
    /// <summary>所有掉落表配置条目列表。</summary>
    public IReadOnlyList<DropTableSO> DropTables => dropTables;
    /// <summary>所有自动攻击配置条目列表。</summary>
    public IReadOnlyList<AutoAttackDataSO> AutoAttacks => autoAttacks;
    /// <summary>所有局内升级选项配置条目列表。</summary>
    public IReadOnlyList<UpgradeOptionSO> UpgradeOptions => upgradeOptions;
    /// <summary>所有奖励池配置条目列表。</summary>
    public IReadOnlyList<RewardPoolSO> RewardPools => rewardPools;
    /// <summary>所有天赋配置条目列表。</summary>
    public IReadOnlyList<TalentDataSO> Talents => talents;
    /// <summary>所有装备配置条目列表。</summary>
    public IReadOnlyList<EquipmentDataSO> Equipment => equipment;
    /// <summary>所有地图配置条目列表。</summary>
    public IReadOnlyList<MapDataSO> Maps => maps;
    /// <summary>所有局内事件配置条目列表。</summary>
    public IReadOnlyList<GameplayEventDataSO> GameplayEvents => gameplayEvents;
    /// <summary>所有商店商品配置条目列表。</summary>
    public IReadOnlyList<ShopItemSO> ShopItems => shopItems;
    /// <summary>所有成就配置条目列表。</summary>
    public IReadOnlyList<AchievementDataSO> Achievements => achievements;
    /// <summary>所有每日签到奖励配置条目列表。</summary>
    public IReadOnlyList<DailyRewardEntrySO> DailyRewardEntries => dailyRewardEntries;
    /// <summary>所有升级卡配置条目列表。</summary>
    public IReadOnlyList<UpgradeCardSO> UpgradeCards => upgradeCards;
    /// <summary>所有升级卡奖励池配置条目列表。</summary>
    public IReadOnlyList<UpgradeCardRewardPoolSO> UpgradeCardRewardPools => upgradeCardRewardPools;

    /// <summary>按 configId 查找玩家配置条目。</summary>
    /// <param name="configId">玩家配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetPlayer(string configId, out PlayerDataSO data) =>
        TryGet(players, configId, out data);

    /// <summary>按 configId 查找敌人配置条目。</summary>
    /// <param name="configId">敌人配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetEnemy(string configId, out EnemyDataSO data) =>
        TryGet(enemies, configId, out data);

    /// <summary>按 configId 查找技能配置条目。</summary>
    /// <param name="configId">技能配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetSkill(string configId, out SkillDataSO data) =>
        TryGet(skills, configId, out data);

    /// <summary>按 configId 查找 Buff 配置条目。</summary>
    /// <param name="configId">Buff 配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetBuff(string configId, out BuffDataSO data) =>
        TryGet(buffs, configId, out data);

    /// <summary>按 configId 查找波次配置条目。</summary>
    /// <param name="configId">波次配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetWave(string configId, out WaveDataSO data) =>
        TryGet(waves, configId, out data);

    /// <summary>按 configId 查找 Boss 配置条目。</summary>
    /// <param name="configId">Boss 配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetBoss(string configId, out BossDataSO data) =>
        TryGet(bosses, configId, out data);

    /// <summary>按 configId 查找 Boss 技能配置条目。</summary>
    /// <param name="configId">Boss 技能配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetBossSkill(string configId, out BossSkillDataSO data) =>
        TryGet(bossSkills, configId, out data);

    /// <summary>按 configId 查找特殊敌人能力配置条目。</summary>
    /// <param name="configId">能力配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetSpecialEnemyAbility(string configId, out SpecialEnemyAbilityDataSO data) =>
        TryGet(specialEnemyAbilities, configId, out data);

    /// <summary>按 configId 查找掉落表配置条目。</summary>
    /// <param name="configId">掉落表配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetDropTable(string configId, out DropTableSO data) =>
        TryGet(dropTables, configId, out data);

    /// <summary>按 configId 查找自动攻击配置条目。</summary>
    /// <param name="configId">自动攻击配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetAutoAttack(string configId, out AutoAttackDataSO data) =>
        TryGet(autoAttacks, configId, out data);

    /// <summary>按 configId 查找局内升级选项配置条目。</summary>
    /// <param name="configId">升级选项配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetUpgradeOption(string configId, out UpgradeOptionSO data) =>
        TryGet(upgradeOptions, configId, out data);

    /// <summary>按 configId 查找奖励池配置条目。</summary>
    /// <param name="configId">奖励池配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetRewardPool(string configId, out RewardPoolSO data) =>
        TryGet(rewardPools, configId, out data);

    /// <summary>按 configId 查找天赋配置条目。</summary>
    /// <param name="configId">天赋配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetTalent(string configId, out TalentDataSO data) =>
        TryGet(talents, configId, out data);

    /// <summary>按 configId 查找装备配置条目。</summary>
    /// <param name="configId">装备配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetEquipment(string configId, out EquipmentDataSO data) =>
        TryGet(equipment, configId, out data);

    /// <summary>按 configId 查找地图配置条目。</summary>
    /// <param name="configId">地图配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetMap(string configId, out MapDataSO data) =>
        TryGet(maps, configId, out data);

    /// <summary>按 configId 查找局内事件配置条目。</summary>
    /// <param name="configId">局内事件配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetGameplayEvent(string configId, out GameplayEventDataSO data) =>
        TryGet(gameplayEvents, configId, out data);

    /// <summary>按 configId 查找商店商品配置条目。</summary>
    /// <param name="configId">商店商品配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetShopItem(string configId, out ShopItemSO data) =>
        TryGet(shopItems, configId, out data);

    /// <summary>按 configId 查找成就配置条目。</summary>
    /// <param name="configId">成就配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetAchievement(string configId, out AchievementDataSO data) =>
        TryGet(achievements, configId, out data);

    /// <summary>按 configId 查找升级卡配置条目。</summary>
    /// <param name="configId">升级卡配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetUpgradeCard(string configId, out UpgradeCardSO data) =>
        TryGet(upgradeCards, configId, out data);

    /// <summary>按 configId 查找升级卡奖励池配置条目。</summary>
    /// <param name="configId">奖励池配置唯一标识。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetUpgradeCardRewardPool(string configId, out UpgradeCardRewardPoolSO data) =>
        TryGet(upgradeCardRewardPools, configId, out data);

    /// <summary>按签到天数索引查找每日奖励配置条目。</summary>
    /// <param name="dayIndex">签到天数（从 1 开始）。</param>
    /// <param name="data">查找到的配置资产；未找到时为 null。</param>
    /// <returns>找到时返回 true，否则返回 false。</returns>
    public bool TryGetDailyRewardEntryByDay(int dayIndex, out DailyRewardEntrySO data)
    {
        data = null;
        if (dailyRewardEntries == null || dayIndex < 1)
        {
            return false;
        }

        for (int i = 0; i < dailyRewardEntries.Count; i++)
        {
            DailyRewardEntrySO entry = dailyRewardEntries[i];
            if (entry != null && entry.DayIndex == dayIndex)
            {
                data = entry;
                return true;
            }
        }

        return false;
    }

    private static bool TryGet<T>(List<T> list, string configId, out T data) where T : ConfigDataBase
    {
        data = null;
        if (list == null || string.IsNullOrWhiteSpace(configId))
        {
            return false;
        }

        for (int i = 0; i < list.Count; i++)
        {
            T entry = list[i];
            if (entry != null && entry.ConfigId == configId)
            {
                data = entry;
                return true;
            }
        }

        return false;
    }
}
