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
    private readonly Dictionary<string, BossDataSO> bossesById = new Dictionary<string, BossDataSO>(4);
    private readonly Dictionary<string, DropTableSO> dropTablesById = new Dictionary<string, DropTableSO>(8);

    public bool IsInitialized { get; private set; }
    public GameConfig GameConfig => gameConfig;
    public ConfigDatabaseSO Database { get; private set; }
    public ConfigValidationResult LastValidation { get; private set; }

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

        IsInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        ClearCache();
        IsInitialized = false;
        LastValidation = null;
    }

    public bool ShouldLog()
    {
        return gameConfig != null && gameConfig.EnableRuntimeLogs;
    }

    public bool TryGetPlayer(string configId, out PlayerDataSO data) =>
        TryGet(playersById, configId, out data);

    public bool TryGetEnemy(string configId, out EnemyDataSO data) =>
        TryGet(enemiesById, configId, out data);

    public bool TryGetSkill(string configId, out SkillDataSO data) =>
        TryGet(skillsById, configId, out data);

    public bool TryGetBuff(string configId, out BuffDataSO data) =>
        TryGet(buffsById, configId, out data);

    public bool TryGetWave(string configId, out WaveDataSO data) =>
        TryGet(wavesById, configId, out data);

    public bool TryGetBoss(string configId, out BossDataSO data) =>
        TryGet(bossesById, configId, out data);

    public bool TryGetDropTable(string configId, out DropTableSO data) =>
        TryGet(dropTablesById, configId, out data);

    public PlayerRuntimeData CreatePlayerRuntime(string configId, int level = -1)
    {
        if (!TryGetPlayer(configId, out PlayerDataSO source))
        {
            LogMissingConfig(nameof(PlayerDataSO), configId);
            return null;
        }

        return ConfigRuntimeFactory.CreatePlayer(source, level);
    }

    public EnemyRuntimeData CreateEnemyRuntime(string configId)
    {
        if (!TryGetEnemy(configId, out EnemyDataSO source))
        {
            LogMissingConfig(nameof(EnemyDataSO), configId);
            return null;
        }

        return ConfigRuntimeFactory.CreateEnemy(source);
    }

    public SkillRuntimeData CreateSkillRuntime(string configId, int level = 1)
    {
        if (!TryGetSkill(configId, out SkillDataSO source))
        {
            LogMissingConfig(nameof(SkillDataSO), configId);
            return null;
        }

        return ConfigRuntimeFactory.CreateSkill(source, level);
    }

    public BuffRuntimeData CreateBuffRuntime(string configId, int stacks = 1)
    {
        if (!TryGetBuff(configId, out BuffDataSO source))
        {
            LogMissingConfig(nameof(BuffDataSO), configId);
            return null;
        }

        return ConfigRuntimeFactory.CreateBuff(source, stacks);
    }

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
        IndexList(Database.Bosses, bossesById);
        IndexList(Database.DropTables, dropTablesById);

        if (ShouldLog())
        {
            Debug.Log(
                $"[ConfigManager] 已加载配置：Player={playersById.Count}, Enemy={enemiesById.Count}, " +
                $"Skill={skillsById.Count}, Buff={buffsById.Count}, Wave={wavesById.Count}, " +
                $"Boss={bossesById.Count}, DropTable={dropTablesById.Count}");
        }
    }

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

    private void ClearCache()
    {
        playersById.Clear();
        enemiesById.Clear();
        skillsById.Clear();
        buffsById.Clear();
        wavesById.Clear();
        bossesById.Clear();
        dropTablesById.Clear();
    }

    private static bool TryGet<T>(Dictionary<string, T> map, string configId, out T data)
    {
        data = default;
        return !string.IsNullOrWhiteSpace(configId) && map.TryGetValue(configId, out data);
    }

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

    private void LogMissingConfig(string typeName, string configId)
    {
        Debug.LogError($"[ConfigManager] 未找到 {typeName} configId={configId}");
    }
}
