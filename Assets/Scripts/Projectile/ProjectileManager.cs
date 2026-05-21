using UnityEngine;

/// <summary>
/// 投射物调度：从对象池生成、弹道排布、启动飞行与回收。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 根或子物体上。</para>
/// <para><b>获取方式：</b><see cref="ServiceLocator.Get{T}"/>。</para>
/// </remarks>
public class ProjectileManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private ProjectileDataSO defaultProjectileData;

    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public ProjectileDataSO DefaultData => ResolveData(null);

    public void Initialize()
    {
        if (defaultProjectileData == null)
        {
            defaultProjectileData = Resources.Load<ProjectileDataSO>(GameConstants.ResourcePaths.ProjectileDefault);
            if (defaultProjectileData == null)
            {
                Debug.LogWarning(
                    "[ProjectileManager] 未找到默认投射物配置: " + GameConstants.ResourcePaths.ProjectileDefault);
            }
        }

        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        isInitialized = false;
    }

    /// <summary>按请求发射一枚投射物。</summary>
    public ProjectileController Spawn(ProjectileSpawnRequest request, ProjectileRuntimeOverrides overrides = default)
    {
        if (!isInitialized)
        {
            Initialize();
        }

        ProjectileDataSO data = ResolveData(request.Data);
        if (data == null)
        {
            Debug.LogWarning("[ProjectileManager] Spawn 失败：无有效 ProjectileDataSO。");
            return null;
        }

        ProjectileController controller = SpawnInstance(request.SpawnPosition, request.Direction, data);
        if (controller == null)
        {
            return null;
        }

        controller.BeginFlight(request, data, overrides);
        return controller;
    }

    /// <summary>扇形多弹道（当前射击技能默认排布）。</summary>
    public int SpawnFan(ProjectileSpawnRequest template, int count, float angleBetweenDegrees)
    {
        if (count <= 1)
        {
            return Spawn(template) != null ? 1 : 0;
        }

        int spawned = 0;
        int middle = count / 2;
        for (int i = 0; i < count; i++)
        {
            float angleOffset = (i - middle) * angleBetweenDegrees;
            Vector2 dir = Rotate(template.Direction, angleOffset);
            ProjectileSpawnRequest req = template.WithDirection(dir);
            if (Spawn(req) != null)
            {
                spawned++;
            }
        }

        return spawned;
    }

    /// <summary>环形均匀分布弹道。</summary>
    public int SpawnRing(ProjectileSpawnRequest template, int count)
    {
        if (count <= 0)
        {
            return 0;
        }

        int spawned = 0;
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = Rotate(Vector2.up, step * i);
            ProjectileSpawnRequest req = template.WithDirection(dir);
            if (Spawn(req) != null)
            {
                spawned++;
            }
        }

        return spawned;
    }

    /// <summary>根据 Pattern 分发排布。</summary>
    public int SpawnPattern(ProjectileSpawnRequest request)
    {
        switch (request.Pattern)
        {
            case ProjectileSpawnPattern.Fan:
                return SpawnFan(request, request.PatternCount, request.PatternAngleDegrees);
            case ProjectileSpawnPattern.Ring:
                return SpawnRing(request, request.PatternCount);
            case ProjectileSpawnPattern.MultiWave:
            case ProjectileSpawnPattern.Single:
            default:
                return Spawn(request) != null ? 1 : 0;
        }
    }

    public void Release(ProjectileController controller)
    {
        if (controller == null)
        {
            return;
        }

        if (ServiceLocator.TryGet(out PoolManager poolManager)
            && poolManager.IsManagedInstance(controller.gameObject))
        {
            poolManager.Despawn(controller.gameObject);
            return;
        }

        controller.gameObject.SetActive(false);
    }

    private ProjectileController SpawnInstance(Vector3 position, Vector2 direction, ProjectileDataSO data)
    {
        float zAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, zAngle);

        if (ServiceLocator.TryGet(out PoolManager poolManager))
        {
            ProjectileController fromPool = poolManager.Spawn<ProjectileController>(
                data.PoolKey,
                position,
                rotation);
            if (fromPool != null)
            {
                return fromPool;
            }
        }

        if (data.Prefab != null)
        {
            GameObject instance = Instantiate(data.Prefab, position, rotation);
            return instance.GetComponent<ProjectileController>();
        }

        Debug.LogWarning("[ProjectileManager] 对象池与 Prefab 均不可用，无法生成投射物。");
        return null;
    }

    private ProjectileDataSO ResolveData(ProjectileDataSO requestData)
    {
        if (requestData != null)
        {
            return requestData;
        }

        if (defaultProjectileData != null)
        {
            return defaultProjectileData;
        }

        return Resources.Load<ProjectileDataSO>(GameConstants.ResourcePaths.ProjectileDefault);
    }

    private static Vector2 Rotate(Vector2 direction, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos).normalized;
    }
}
