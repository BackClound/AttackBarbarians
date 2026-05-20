using UnityEngine;

/// <summary>
/// 敌人运行时协调器：加载 <see cref="EnemyDataSO"/>、应用波次缩放、驱动对象池复用与能力组件。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在敌人 Prefab 根节点（与 <see cref="Enemy"/>、<see cref="Enemy_Health"/> 同物体）。</para>
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
[RequireComponent(typeof(Enemy_Health))]
[RequireComponent(typeof(Entity_Stats))]
public class EnemyController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string defaultConfigId = GameConstants.ConfigIds.EnemyBat;
    [SerializeField] private EnemyDataSO dataOverride;

    private Enemy enemy;
    private Enemy_Health enemyHealth;
    private Entity_Stats entityStats;
    private readonly EnemyRuntimeData runtimeData = new EnemyRuntimeData();
    private readonly StatRuntimeSnapshot scaledSnapshot = new StatRuntimeSnapshot();
    private IEnemyAbility[] abilities;
    private float waveStatMultiplier = 1f;
    private bool isInitialized;

    public Enemy Enemy => enemy;
    public EnemyRuntimeData RuntimeData => runtimeData;
    public string ConfigId => runtimeData.ConfigId;
    public bool IsReady => isInitialized;

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        enemyHealth = GetComponent<Enemy_Health>();
        entityStats = GetComponent<Entity_Stats>();
        abilities = GetComponents<IEnemyAbility>();
    }

    /// <summary>由 <see cref="EnemySpawnerManager"/> 在池取出后调用。</summary>
    public void InitializeForSpawn(string configId, float statMultiplier, int waveIndex)
    {
        waveStatMultiplier = Mathf.Max(0.1f, statMultiplier);
        if (!TryResolveData(configId, out EnemyDataSO data))
        {
            Debug.LogError($"[EnemyController] 未找到 EnemyData configId={configId}", this);
            isInitialized = false;
            return;
        }

        runtimeData.Initialize(data);
        ApplyScaledStats();
        enemyHealth.ResetForPool();
        ApplyCombatFieldsFromConfig(data);
        if (enemy != null && enemy.idleState != null)
        {
            enemy.stateMachine.InitialState(enemy.idleState);
        }

        NotifyAbilitiesSpawn();
        isInitialized = true;

        GameEvents.RaiseEnemySpawned(this, new EnemyEventArgs(
            gameObject,
            transform.position,
            null,
            runtimeData.ConfigId));
    }

    public void OnPoolDespawn()
    {
        isInitialized = false;
        waveStatMultiplier = 1f;
        enemy?.OnDespawn();
    }

    private void Update()
    {
        if (!isInitialized || abilities == null || abilities.Length == 0)
        {
            return;
        }

        float dt = Time.deltaTime;
        for (int i = 0; i < abilities.Length; i++)
        {
            abilities[i]?.OnUpdate(this, dt);
        }
    }

    public int GetExperienceReward()
    {
        if (!isInitialized)
        {
            return 0;
        }

        float baseExp = runtimeData.ExperienceReward;
        if (baseExp <= 0f)
        {
            return 0;
        }

        return Mathf.Max(1, Mathf.RoundToInt(baseExp));
    }

    private void ApplyScaledStats()
    {
        scaledSnapshot.CopyFrom(runtimeData.Stats);
        EnemyStatScaling.ApplyMultiplier(scaledSnapshot, waveStatMultiplier);
        ConfigStatBridge.ApplyToEntityStats(scaledSnapshot, entityStats);
    }

    private void ApplyCombatFieldsFromConfig(EnemyDataSO data)
    {
        if (enemy == null || data == null)
        {
            return;
        }

        enemy.moveSpeed = scaledSnapshot.Get(StatType.MoveSpeed);
        enemy.cooldownThreshold = data.AttackCooldown;
    }

    private void NotifyAbilitiesSpawn()
    {
        if (abilities == null)
        {
            return;
        }

        for (int i = 0; i < abilities.Length; i++)
        {
            abilities[i]?.OnSpawn(this);
        }
    }

    public void NotifyDeath()
    {
        if (abilities == null)
        {
            return;
        }

        for (int i = 0; i < abilities.Length; i++)
        {
            abilities[i]?.OnDeath(this);
        }
    }

    // 尝试解析数据
    private bool TryResolveData(string configId, out EnemyDataSO data)
    {
        if (dataOverride != null)
        {
            data = dataOverride;
            return true;
        }

        string id = string.IsNullOrEmpty(configId) ? defaultConfigId : configId;
        if (ServiceLocator.TryGet(out ConfigManager configManager) && configManager.TryGetEnemy(id, out data))
        {
            return true;
        }

        data = null;
        return false;
    }
}

/// <summary>
/// 敌人属性波次缩放工具（纯逻辑，无挂载）。
/// </summary>
public static class EnemyStatScaling
{
    public static void ApplyMultiplier(StatRuntimeSnapshot snapshot, float multiplier)
    {
        if (snapshot == null || Mathf.Approximately(multiplier, 1f))
        {
            return;
        }

        ScaleStat(snapshot, StatType.MaxHp, multiplier);
        ScaleStat(snapshot, StatType.MoveSpeed, multiplier);
        ScaleStat(snapshot, StatType.AttackSpeed, multiplier);
        ScaleStat(snapshot, StatType.AttackSpeedMulti, multiplier);
        ScaleStat(snapshot, StatType.Damage, multiplier);
        ScaleStat(snapshot, StatType.FireDamage, multiplier);
        ScaleStat(snapshot, StatType.IceDamage, multiplier);
        ScaleStat(snapshot, StatType.LightningDamage, multiplier);
        ScaleStat(snapshot, StatType.Armor, multiplier);
    }

    private static void ScaleStat(StatRuntimeSnapshot snapshot, StatType statType, float multiplier)
    {
        snapshot.Set(statType, snapshot.Get(statType) * multiplier);
    }
}
