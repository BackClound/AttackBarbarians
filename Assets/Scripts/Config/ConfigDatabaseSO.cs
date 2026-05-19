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
    [SerializeField] private List<BuffDataSO> buffs = new List<BuffDataSO>();
    [SerializeField] private List<WaveDataSO> waves = new List<WaveDataSO>();
    [SerializeField] private List<BossDataSO> bosses = new List<BossDataSO>();
    [SerializeField] private List<DropTableSO> dropTables = new List<DropTableSO>();

    public IReadOnlyList<PlayerDataSO> Players => players;
    public IReadOnlyList<EnemyDataSO> Enemies => enemies;
    public IReadOnlyList<SkillDataSO> Skills => skills;
    public IReadOnlyList<BuffDataSO> Buffs => buffs;
    public IReadOnlyList<WaveDataSO> Waves => waves;
    public IReadOnlyList<BossDataSO> Bosses => bosses;
    public IReadOnlyList<DropTableSO> DropTables => dropTables;

    public bool TryGetPlayer(string configId, out PlayerDataSO data) =>
        TryGet(players, configId, out data);

    public bool TryGetEnemy(string configId, out EnemyDataSO data) =>
        TryGet(enemies, configId, out data);

    public bool TryGetSkill(string configId, out SkillDataSO data) =>
        TryGet(skills, configId, out data);

    public bool TryGetBuff(string configId, out BuffDataSO data) =>
        TryGet(buffs, configId, out data);

    public bool TryGetWave(string configId, out WaveDataSO data) =>
        TryGet(waves, configId, out data);

    public bool TryGetBoss(string configId, out BossDataSO data) =>
        TryGet(bosses, configId, out data);

    public bool TryGetDropTable(string configId, out DropTableSO data) =>
        TryGet(dropTables, configId, out data);

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
