using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局配置加载器：加载 <see cref="GameConfig"/>、<see cref="ConfigDatabaseSO"/>，构建 ID 索引并在启动时校验。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是（MonoBehaviour）。</para>
/// <para><b>推荐挂载对象：</b>挂在 <c>GameSystems</c> 根物体上，或与 <see cref="GameBootstrapper"/> 同层级子物体。</para>
/// <para><b>不要挂载到：</b>Player、Enemy、子弹 Prefab。</para>
/// <para><b>Inspector 配置：</b></para>
/// <list type="bullet">
/// <item><description><c>Game Config</c>：<c>Assets/Resources/Config/GameConfig.asset</c>（留空则从 Resources 加载）。</description></item>
/// <item><description><c>Config Database</c>：可覆盖 GameConfig 内的数据库引用；留空则使用 GameConfig 或 Resources 自动加载。</description></item>
/// </list>
/// <para><b>获取方式：</b><c>ServiceLocator.Get&lt;ConfigManager&gt;()</c>（须在 Bootstrap 之后）。</para>
/// </remarks>
public class ConfigManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private GameConfig gameConfig;
    [SerializeField] private ConfigDatabaseSO configDatabaseOverride;

    private readonly Dictionary<string, PlayerDataSO> playersById = new Dictionary<string, PlayerDataSO>(8);
    private readonly Dictionary<string, EnemyDataSO> enemiesById = new Dictionary<string, EnemyDataSO>(16);
    private readonly Dictionary<string, SkillDataSO> skillsById = new Dictionary<string, SkillDataSO>(16);
    private readonly Dictionary<string, BuffDataSO> buffsById = new Dictionary<string, BuffDataSO>(16);
    private readonly Dictionary<string, WaveDataSO> wavesById = new Dictionary<string, WaveDataSO>(8);
    private readonly Dictionary<string, WaveScheduleSO> waveSchedulesById = new Dictionary<string, WaveScheduleSO>(4);
    private readonly Dictionary<string, BossDataSO> bossesById = new Dictionary<string, BossDataSO>(4);
    private readonly Dictionary<string, BossSkillDataSO> bossSkillsById = new Dictionary<string, BossSkillDataSO>(8);
    private readonly Dictionary<string, SpecialEnemyAbilityDataSO> specialEnemyAbilitiesById =
        new Dictionary<string, SpecialEnemyAbilityDataSO>(8);
    private readonly Dictionary<string, DropTableSO> dropTablesById = new Dictionary<string, DropTableSO>(8);
    private readonly Dictionary<string, AutoAttackDataSO> autoAttacksById = new Dictionary<string, AutoAttackDataSO>(4);
    private readonly Dictionary<string, UpgradeOptionSO> upgradeOptionsById = new Dictionary<string, UpgradeOptionSO>(32);
    private readonly Dictionary<string, RewardPoolSO> rewardPoolsById = new Dictionary<string, RewardPoolSO>(4);
    private readonly Dictionary<string, TalentDataSO> talentsById = new Dictionary<string, TalentDataSO>(16);
    private readonly Dictionary<string, EquipmentDataSO> equipmentById = new Dictionary<string, EquipmentDataSO>(16);
    private readonly Dictionary<string, MapDataSO> mapsById = new Dictionary<string, MapDataSO>(4);
    private readonly Dictionary<string, GameplayEventDataSO> gameplayEventsById =
        new Dictionary<string, GameplayEventDataSO>(8);
    private readonly Dictionary<string, ShopItemSO> shopItemsById = new Dictionary<string, ShopItemSO>(16);
    private readonly Dictionary<string, AchievementDataSO> achievementsById = new Dictionary<string, AchievementDataSO>(16);
    private readonly Dictionary<int, DailyRewardEntrySO> dailyRewardsByDay = new Dictionary<int, DailyRewardEntrySO>(8);
    private readonly Dictionary<string, UpgradeCardSO> upgradeCardsById = new Dictionary<string, UpgradeCardSO>(32);
    private readonly Dictionary<string, UpgradeCardRewardPoolSO> upgradeCardRewardPoolsById =
        new Dictionary<string, UpgradeCardRewardPoolSO>(8);
    private readonly List<RewardPoolSO> rewardPoolList = new List<RewardPoolSO>(4);

    public bool IsInitialized { get; private set; }
    /// <summary>游戏全局配置。</summary>
    public GameConfig GameConfig => gameConfig;
    /// <summary>性能预算配置。</summary>
    public PerformanceBudgetSO PerformanceBudget =>
        gameConfig != null ? gameConfig.PerformanceBudget : null;
    /// <summary>当前使用的配置数据库。</summary>
    public ConfigDatabaseSO Database { get; private set; }
    /// <summary>最近一次配置校验结果。</summary>
    public ConfigValidationResult LastValidation { get; private set; }

    /// <summary>加载配置、重建索引并执行校验。</summary>
    public void Initialize()
    {
        if (gameConfig == null)
        {
            gameConfig = Resources.Load<GameConfig>(GameConstants.ResourcePaths.GameConfig);
        }

        Database = configDatabaseOverride != null
            ? configDatabaseOverride
            : gameConfig != null ? gameConfig.ConfigDatabase : null;

        if (Database == null)
        {
            Database = Resources.Load<ConfigDatabaseSO>(GameConstants.ResourcePaths.ConfigDatabase);
        }

        RebuildCache();
        LastValidation = ConfigValidator.ValidateDatabase(Database);
        LogValidationResult();

        GameDebug.SyncFromConfig();
        IsInitialized = true;
    }

    /// <summary>每帧更新（当前无逻辑）。</summary>
    /// <param name="deltaTime">帧间隔时间（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>清空索引缓存并重置初始化状态。</summary>
    public void Shutdown()
    {
        ClearCache();
        IsInitialized = false;
        LastValidation = null;
    }

    /// <summary>是否启用运行时配置相关日志。</summary>
    /// <returns>启用返回 true，否则返回 false。</returns>
    public bool ShouldLog()
    {
        return gameConfig != null && gameConfig.EnableRuntimeLogs;
    }

    /// <summary>按 configId 查找玩家配置。</summary>
    public bool TryGetPlayer(string configId, out PlayerDataSO data) =>
        TryGet(playersById, configId, out data);

    /// <summary>按 configId 查找敌人配置。</summary>
    public bool TryGetEnemy(string configId, out EnemyDataSO data) =>
        TryGet(enemiesById, configId, out data);

    /// <summary>按 configId 查找技能配置。</summary>
    public bool TryGetSkill(string configId, out SkillDataSO data) =>
        TryGet(skillsById, configId, out data);

    /// <summary>按 configId 查找 Buff 配置。</summary>
    public bool TryGetBuff(string configId, out BuffDataSO data) =>
        TryGet(buffsById, configId, out data);

    /// <summary>按 configId 查找波次配置（Legacy）。</summary>
    public bool TryGetWave(string configId, out WaveDataSO data) =>
        TryGet(wavesById, configId, out data);

    /// <summary>按 configId 查找波次表配置。</summary>
    public bool TryGetWaveSchedule(string configId, out WaveScheduleSO data) =>
        TryGet(waveSchedulesById, configId, out data);

    /// <summary>按 configId 查找 Boss 配置。</summary>
    public bool TryGetBoss(string configId, out BossDataSO data) =>
        TryGet(bossesById, configId, out data);

    /// <summary>按 configId 查找 Boss 技能配置。</summary>
    public bool TryGetBossSkill(string configId, out BossSkillDataSO data) =>
        TryGet(bossSkillsById, configId, out data);

    /// <summary>按 configId 查找特殊敌人能力配置。</summary>
    public bool TryGetSpecialEnemyAbility(string configId, out SpecialEnemyAbilityDataSO data) =>
        TryGet(specialEnemyAbilitiesById, configId, out data);

    /// <summary>按 configId 查找掉落表配置。</summary>
    public bool TryGetDropTable(string configId, out DropTableSO data) =>
        TryGet(dropTablesById, configId, out data);

    /// <summary>按 configId 查找自动攻击配置。</summary>
    public bool TryGetAutoAttack(string configId, out AutoAttackDataSO data) =>
        TryGet(autoAttacksById, configId, out data);

    /// <summary>按 configId 查找升级选项配置。</summary>
    public bool TryGetUpgradeOption(string configId, out UpgradeOptionSO data) =>
        TryGet(upgradeOptionsById, configId, out data);

    /// <summary>按 configId 查找奖励池配置。</summary>
    public bool TryGetRewardPool(string configId, out RewardPoolSO data) =>
        TryGet(rewardPoolsById, configId, out data);

    /// <summary>按 configId 查找天赋配置。</summary>
    public bool TryGetTalent(string configId, out TalentDataSO data) =>
        TryGet(talentsById, configId, out data);

    /// <summary>按 configId 查找装备配置。</summary>
    public bool TryGetEquipment(string configId, out EquipmentDataSO data) =>
        TryGet(equipmentById, configId, out data);

    /// <summary>按 configId 查找地图配置。</summary>
    public bool TryGetMap(string configId, out MapDataSO data) =>
        TryGet(mapsById, configId, out data);

    /// <summary>按 configId 查找局内事件配置。</summary>
    public bool TryGetGameplayEvent(string configId, out GameplayEventDataSO data) =>
        TryGet(gameplayEventsById, configId, out data);

    /// <summary>按 configId 查找商店物品配置。</summary>
    public bool TryGetShopItem(string configId, out ShopItemSO data) =>
        TryGet(shopItemsById, configId, out data);

    /// <summary>按 configId 查找成就配置。</summary>
    public bool TryGetAchievement(string configId, out AchievementDataSO data) =>
        TryGet(achievementsById, configId, out data);

    /// <summary>按天数索引查找每日奖励条目。</summary>
    public bool TryGetDailyRewardEntry(int dayIndex, out DailyRewardEntrySO data) =>
        dailyRewardsByDay.TryGetValue(dayIndex, out data);

    /// <summary>按 configId 查找升级卡牌配置。</summary>
    public bool TryGetUpgradeCard(string configId, out UpgradeCardSO data) =>
        TryGet(upgradeCardsById, configId, out data);

    /// <summary>按 configId 查找升级卡牌奖励池配置。</summary>
    public bool TryGetUpgradeCardRewardPool(string configId, out UpgradeCardRewardPoolSO data) =>
        TryGet(upgradeCardRewardPoolsById, configId, out data);

    /// <summary>获取全部奖励池列表。</summary>
    public IReadOnlyList<RewardPoolSO> GetAllRewardPools() => rewardPoolList;

    /// <summary>
    /// 创建玩家运行时数据。
    /// </summary>
    /// <param name="configId">玩家配置 Id。</param>
    /// <param name="level">等级（-1 使用配置默认）。</param>
    /// <returns>运行时数据；未找到配置时返回 null。</returns>
    public PlayerRuntimeData CreatePlayerRuntime(string configId, int level = -1)
    {
        if (!TryGetPlayer(configId, out PlayerDataSO source))
        {
            LogMissingConfig(nameof(PlayerDataSO), configId);
            return null;
        }

        return ConfigRuntimeFactory.CreatePlayer(source, level);
    }

    /// <summary>
    /// 创建敌人运行时数据。
    /// </summary>
    /// <param name="configId">敌人配置 Id。</param>
    /// <returns>运行时数据；未找到配置时返回 null。</returns>
    public EnemyRuntimeData CreateEnemyRuntime(string configId)
    {
        if (!TryGetEnemy(configId, out EnemyDataSO source))
        {
            LogMissingConfig(nameof(EnemyDataSO), configId);
            return null;
        }

        return ConfigRuntimeFactory.CreateEnemy(source);
    }

    /// <summary>
    /// 创建技能运行时数据。
    /// </summary>
    /// <param name="configId">技能配置 Id。</param>
    /// <param name="level">技能等级。</param>
    /// <returns>运行时数据；未找到配置时返回 null。</returns>
    public SkillRuntimeData CreateSkillRuntime(string configId, int level = 1)
    {
        if (!TryGetSkill(configId, out SkillDataSO source))
        {
            LogMissingConfig(nameof(SkillDataSO), configId);
            return null;
        }

        return ConfigRuntimeFactory.CreateSkill(source, level);
    }

    /// <summary>
    /// 创建 Buff 运行时数据。
    /// </summary>
    /// <param name="configId">Buff 配置 Id。</param>
    /// <param name="stacks">层数。</param>
    /// <returns>运行时数据；未找到配置时返回 null。</returns>
    public BuffRuntimeData CreateBuffRuntime(string configId, int stacks = 1)
    {
        if (!TryGetBuff(configId, out BuffDataSO source))
        {
            LogMissingConfig(nameof(BuffDataSO), configId);
            return null;
        }

        return ConfigRuntimeFactory.CreateBuff(source, stacks);
    }

    /// <summary>从数据库重建全部 configId 索引。</summary>
    private void RebuildCache()
    {
        ClearCache();
        if (Database == null)
        {
            if (ShouldLog())
            {
                Debug.LogWarning("[ConfigManager] ConfigDatabase 未配置，仅 GameConfig 可用。");
            }

            return;
        }

        IndexList(Database.Players, playersById);
        IndexList(Database.Enemies, enemiesById);
        IndexList(Database.Skills, skillsById);
        IndexList(Database.Buffs, buffsById);
        IndexList(Database.Waves, wavesById);
        IndexList(Database.WaveSchedules, waveSchedulesById);
        IndexList(Database.Bosses, bossesById);
        IndexList(Database.BossSkills, bossSkillsById);
        IndexList(Database.SpecialEnemyAbilities, specialEnemyAbilitiesById);
        IndexList(Database.DropTables, dropTablesById);
        IndexList(Database.AutoAttacks, autoAttacksById);
        IndexList(Database.UpgradeOptions, upgradeOptionsById);
        IndexList(Database.RewardPools, rewardPoolsById);
        IndexList(Database.Talents, talentsById);
        IndexList(Database.Equipment, equipmentById);
        IndexList(Database.Maps, mapsById);
        IndexList(Database.GameplayEvents, gameplayEventsById);
        IndexList(Database.ShopItems, shopItemsById);
        IndexList(Database.Achievements, achievementsById);
        IndexDailyRewardEntries(Database.DailyRewardEntries);
        IndexList(Database.UpgradeCards, upgradeCardsById);
        IndexList(Database.UpgradeCardRewardPools, upgradeCardRewardPoolsById);
        rewardPoolList.Clear();
        if (Database.RewardPools != null)
        {
            for (int i = 0; i < Database.RewardPools.Count; i++)
            {
                RewardPoolSO pool = Database.RewardPools[i];
                if (pool != null)
                {
                    rewardPoolList.Add(pool);
                }
            }
        }

        if (ShouldLog())
        {
            Debug.Log(
                $"[ConfigManager] 已加载配置：Player={playersById.Count}, Enemy={enemiesById.Count}, " +
                $"Skill={skillsById.Count}, Buff={buffsById.Count}, Wave={wavesById.Count}, " +
                $"WaveSchedule={waveSchedulesById.Count}, " +
                $"Boss={bossesById.Count}, BossSkill={bossSkillsById.Count}, " +
                $"SpecialAbility={specialEnemyAbilitiesById.Count}, DropTable={dropTablesById.Count}, " +
                $"AutoAttack={autoAttacksById.Count}, Upgrade={upgradeOptionsById.Count}, " +
                $"RewardPool={rewardPoolsById.Count}, Talent={talentsById.Count}, " +
                $"Equipment={equipmentById.Count}, Map={mapsById.Count}, " +
                $"GameplayEvent={gameplayEventsById.Count}, ShopItem={shopItemsById.Count}, " +
                $"Achievement={achievementsById.Count}, DailyReward={dailyRewardsByDay.Count}, " +
                $"UpgradeCard={upgradeCardsById.Count}, UpgradeCardPool={upgradeCardRewardPoolsById.Count}");
        }
    }

    /// <summary>索引每日奖励条目（按 DayIndex）。</summary>
    /// <param name="list">每日奖励列表。</param>
    private void IndexDailyRewardEntries(IReadOnlyList<DailyRewardEntrySO> list)
    {
        dailyRewardsByDay.Clear();
        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            DailyRewardEntrySO entry = list[i];
            if (entry == null)
            {
                continue;
            }

            dailyRewardsByDay[entry.DayIndex] = entry;
        }
    }

    /// <summary>将配置列表写入字典索引。</summary>
    /// <typeparam name="T">配置类型。</typeparam>
    /// <param name="list">配置列表。</param>
    /// <param name="map">目标字典。</param>
    private static void IndexList<T>(IReadOnlyList<T> list, Dictionary<string, T> map) where T : ConfigDataBase
    {
        if (list == null)
        {
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            T entry = list[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ConfigId))
            {
                continue;
            }

            map[entry.ConfigId] = entry;
        }
    }

    /// <summary>清空全部索引缓存。</summary>
    private void ClearCache()
    {
        playersById.Clear();
        enemiesById.Clear();
        skillsById.Clear();
        buffsById.Clear();
        wavesById.Clear();
        waveSchedulesById.Clear();
        bossesById.Clear();
        bossSkillsById.Clear();
        specialEnemyAbilitiesById.Clear();
        dropTablesById.Clear();
        autoAttacksById.Clear();
        upgradeOptionsById.Clear();
        rewardPoolsById.Clear();
        talentsById.Clear();
        equipmentById.Clear();
        mapsById.Clear();
        gameplayEventsById.Clear();
        shopItemsById.Clear();
        achievementsById.Clear();
        dailyRewardsByDay.Clear();
        upgradeCardsById.Clear();
        upgradeCardRewardPoolsById.Clear();
        rewardPoolList.Clear();
    }

    /// <summary>从字典按 configId 查找配置。</summary>
    /// <typeparam name="T">配置类型。</typeparam>
    /// <param name="map">索引字典。</param>
    /// <param name="configId">配置 Id。</param>
    /// <param name="data">输出的配置数据。</param>
    /// <returns>找到返回 true，否则返回 false。</returns>
    private static bool TryGet<T>(Dictionary<string, T> map, string configId, out T data)
    {
        data = default;
        return !string.IsNullOrWhiteSpace(configId) && map.TryGetValue(configId, out data);
    }

    /// <summary>输出配置校验结果日志。</summary>
    private void LogValidationResult()
    {
        if (LastValidation == null)
        {
            return;
        }

        if (LastValidation.IsValid)
        {
            if (ShouldLog())
            {
                Debug.Log("[ConfigManager] 配置校验通过。");
            }

            return;
        }

        Debug.LogError($"[ConfigManager] 配置校验失败:\n{LastValidation.BuildReport()}");
    }

    /// <summary>记录缺失配置的 Error 日志。</summary>
    /// <param name="typeName">配置类型名。</param>
    /// <param name="configId">缺失的 configId。</param>
    private void LogMissingConfig(string typeName, string configId)
    {
        Debug.LogError($"[ConfigManager] 未找到 {typeName} configId={configId}");
    }
}
