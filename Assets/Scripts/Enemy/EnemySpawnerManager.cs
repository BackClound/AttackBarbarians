using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人生成：由 <see cref="WaveManager"/> 驱动，从对象池取出并初始化 <see cref="EnemyController"/>。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。推荐挂在 <c>GameSystems</c> 或场景 <c>EnemyGenerateManager</c> 同级物体。</para>
/// </remarks>
public class EnemySpawnerManager : MonoBehaviour, IGameSystem
{
    [Header("Legacy")]
    [SerializeField] private EnemyGenerateManager legacySpawner;
    [SerializeField] private bool disableLegacySpawnerOnInit = true;

    [Header("Spawn Bounds")]
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private Vector2 viewportMin = new Vector2(0.1f, 0.9f);
    [SerializeField] private Vector2 viewportMax = new Vector2(0.9f, 1.1f);

    private readonly List<string> weightedEnemyIds = new List<string>(16);

    private float minLeftX;
    private float maxRightX;
    private float minY;
    private float maxY;
    private bool boundsReady;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public int AliveEnemyCount { get; private set; }

    public void Initialize()
    {
        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }

        if (legacySpawner == null)
        {
            legacySpawner = FindFirstObjectByType<EnemyGenerateManager>();
        }

        if (disableLegacySpawnerOnInit && legacySpawner != null)
        {
            legacySpawner.SetAutoSpawnEnabled(false);
        }

        GameEvents.SubscribeEnemyKilled(OnEnemyKilled);
        StartCoroutine(DelayInitializeBounds());
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        GameEvents.UnsubscribeEnemyKilled(OnEnemyKilled);
        isInitialized = false;
        boundsReady = false;
    }

    public void ConfigureWavePool(WaveDataSO waveData)
    {
        weightedEnemyIds.Clear();

        if (waveData == null || waveData.EnemyConfigIds == null)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out ConfigManager configManager))
        {
            return;
        }

        IReadOnlyList<string> ids = waveData.EnemyConfigIds;
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            if (!configManager.TryGetEnemy(id, out EnemyDataSO enemyData))
            {
                Debug.LogWarning($"[EnemySpawnerManager] 波次敌人配置缺失 configId={id}");
                continue;
            }

            int weight = enemyData.SpawnWeight;
            for (int w = 0; w < weight; w++)
            {
                weightedEnemyIds.Add(id);
            }
        }
    }

    public bool TrySpawnEnemy(string forcedConfigId, float statMultiplier, int waveIndex)
    {
        if (!boundsReady)
        {
            Debug.LogWarning("[EnemySpawnerManager] 生成边界未就绪，无法生成敌人。");
            return false;
        }

        string configId = forcedConfigId;
        Debug.Log($"[EnemySpawnerManager] 尝试生成敌人 configId={configId} multiplier={statMultiplier} waveIndex={waveIndex}");
        if (string.IsNullOrEmpty(configId))
        {
            configId = PickRandomEnemyId();
        }

        if (string.IsNullOrEmpty(configId))
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out ConfigManager configManager) ||
            !configManager.TryGetEnemy(configId, out EnemyDataSO enemyData))
        {
            Debug.LogWarning($"[EnemySpawnerManager] 无法生成敌人，配置缺失 configId={configId}");
            return false;
        }

        Vector3 position = new Vector3(
            Random.Range(minLeftX, maxRightX),
            Random.Range(minY, maxY),
            0f);
        GameObject instance = SpawnFromPool(enemyData, position);
        if (instance == null)
        {
            Debug.LogWarning($"[EnemySpawnerManager] 无法生成敌人，实例化失败 configId={configId}");
            return false;
        }

        if (!instance.TryGetComponent(out EnemyController controller))
        {
            Debug.LogWarning($"[EnemySpawnerManager] Prefab 缺少 EnemyController configId={configId}", instance);
            return false;
        }

        controller.InitializeForSpawn(configId, statMultiplier, waveIndex);
        AliveEnemyCount++;
        return true;
    }

    private GameObject SpawnFromPool(EnemyDataSO data, Vector3 position)
    {
        string poolKey = data.PoolKey;
        if (ServiceLocator.TryGet(out PoolManager poolManager) && poolManager.HasPool(poolKey))
        {
            Debug.Log($"[EnemySpawnerManager] 从对象池生成敌人 poolKey={poolKey} configId={data.ConfigId}");
            return poolManager.Spawn(poolKey, position, Quaternion.identity);
        }

        if (data.Prefab != null)
        {
            Debug.LogWarning($"[EnemySpawnerManager] 对象池缺失，直接实例化敌人 configId={data.ConfigId} poolKey={poolKey}");
            return Instantiate(data.Prefab, position, Quaternion.identity);
        }
        Debug.LogWarning($"[EnemySpawnerManager] 无法生成敌人，既没有对象池也没有Prefab configId={data.ConfigId}");
        return null;
    }

    private string PickRandomEnemyId()
    {
        if (weightedEnemyIds.Count == 0)
        {
            return GameConstants.ConfigIds.EnemyBat;
        }

        int index = Random.Range(0, weightedEnemyIds.Count);
        return weightedEnemyIds[index];
    }

    private void OnEnemyKilled(GameEventContext ctx)
    {
        if (ctx.Payload is not EnemyEventArgs)
        {
            return;
        }

        AliveEnemyCount = Mathf.Max(0, AliveEnemyCount - 1);
    }

    private IEnumerator DelayInitializeBounds()
    {
        yield return new WaitForSeconds(0.2f);
        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }

        if (spawnCamera == null)
        {
            yield break;
        }

        minLeftX = spawnCamera.ViewportToWorldPoint(new Vector3(viewportMin.x, 0f)).x;
        maxRightX = spawnCamera.ViewportToWorldPoint(new Vector3(viewportMax.x, 0f)).x;
        minY = spawnCamera.ViewportToWorldPoint(new Vector3(0f, viewportMin.y)).y;
        maxY = spawnCamera.ViewportToWorldPoint(new Vector3(0f, viewportMax.y)).y;
        boundsReady = true;
    }
}
