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

    /// <summary>系统是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>默认投射物配置（请求未指定时使用）。</summary>
    public ProjectileDataSO DefaultData => ResolveData(null);

    /// <summary>加载默认配置并完成初始化。</summary>
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

    /// <summary>每帧 Tick（本服务无逐帧逻辑）。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    public void Tick(float deltaTime) { }

    /// <summary>关闭系统并重置初始化标记。</summary>
    public void Shutdown()
    {
        isInitialized = false;
    }

    /// <summary>按请求发射一枚投射物。</summary>
    /// <param name="request">生成请求。</param>
    /// <param name="overrides">技能 Buff 飞行覆盖参数。</param>
    /// <returns>生成的投射物控制器，失败时为 <c>null</c>。</returns>
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

        if (ServiceLocator.TryGet(out PerformanceManager performance) &&
            !performance.TryAcquire(PerformanceBudgetCategory.Projectile))
        {
            return null;
        }

        ProjectileController controller = SpawnInstance(request.SpawnPosition, request.Direction, data);
        if (controller == null)
        {
            if (ServiceLocator.TryGet(out PerformanceManager perfRollback))
            {
                perfRollback.Release(PerformanceBudgetCategory.Projectile);
            }

            return null;
        }

        controller.BeginFlight(request, data, overrides);
        return controller;
    }

    /// <summary>扇形多弹道（当前射击技能默认排布）。</summary>
    /// <param name="template">弹道模板请求。</param>
    /// <param name="count">弹道数量。</param>
    /// <param name="angleBetweenDegrees">相邻弹道夹角（度）。</param>
    /// <returns>成功生成的弹道数量。</returns>
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
    /// <param name="template">弹道模板请求。</param>
    /// <param name="count">弹道数量。</param>
    /// <returns>成功生成的弹道数量。</returns>
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
    /// <param name="request">含排布模式的生成请求。</param>
    /// <returns>成功生成的弹道数量。</returns>
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

    /// <summary>回收投射物实例到对象池。</summary>
    /// <param name="controller">待回收的投射物控制器。</param>
    public void Release(ProjectileController controller)
    {
        if (controller == null)
        {
            return;
        }

        if (ServiceLocator.TryGet(out PerformanceManager performance))
        {
            performance.Release(PerformanceBudgetCategory.Projectile);
        }

        if (ServiceLocator.TryGet(out PoolManager poolManager)
            && poolManager.IsManagedInstance(controller.gameObject))
        {
            poolManager.Despawn(controller.gameObject);
            return;
        }

        controller.gameObject.SetActive(false);
    }

    /// <summary>从对象池或 Prefab 实例化投射物。</summary>
    /// <param name="position">生成位置。</param>
    /// <param name="direction">初始方向。</param>
    /// <param name="data">投射物配置。</param>
    /// <returns>投射物控制器，失败时为 <c>null</c>。</returns>
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

    /// <summary>解析生效的投射物配置（请求优先，否则默认）。</summary>
    /// <param name="requestData">请求中的配置，可为空。</param>
    /// <returns>生效的配置资产。</returns>
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

    /// <summary>将二维方向向量旋转指定角度。</summary>
    /// <param name="direction">原方向。</param>
    /// <param name="angleDegrees">旋转角度（度）。</param>
    /// <returns>旋转后的单位方向。</returns>
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
