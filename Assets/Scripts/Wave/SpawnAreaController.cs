using System.Collections;
using UnityEngine;

/// <summary>
/// 根据相机视口计算敌人生成矩形区域。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 或 <see cref="EnemySpawnerManager"/> 同物体。</para>
/// </remarks>
public class SpawnAreaController : MonoBehaviour
{
    [SerializeField] private Camera spawnCamera;
    [SerializeField] private Vector2 viewportMin = new Vector2(0.1f, 0.9f);
    [SerializeField] private Vector2 viewportMax = new Vector2(0.9f, 1.1f);
    [SerializeField] private float initializeDelaySeconds = 0.2f;

    private float minLeftX;
    private float maxRightX;
    private float minY;
    private float maxY;
    private bool boundsReady;

    /// <summary>生成边界是否已计算完成。</summary>
    public bool IsReady => boundsReady;

    /// <summary>
    /// 应用地图生成区域配置并重新初始化边界。
    /// </summary>
    /// <param name="config">地图生成区域配置。</param>
    public void ApplyMapConfig(MapSpawnAreaConfig config)
    {
        viewportMin = config.ViewportMin;
        viewportMax = config.ViewportMax;
        initializeDelaySeconds = config.InitializeDelaySeconds;
        BeginInitialize();
    }

    /// <summary>解析相机引用。</summary>
    private void Awake()
    {
        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }
    }

    /// <summary>启动协程重新计算生成边界。</summary>
    public void BeginInitialize()
    {
        StopAllCoroutines();
        StartCoroutine(InitializeBoundsRoutine());
    }

    /// <summary>
    /// 在已就绪的矩形区域内随机取一个生成点。
    /// </summary>
    /// <param name="position">输出的世界坐标。</param>
    /// <returns>边界就绪时返回 true，否则返回 false。</returns>
    public bool TryGetRandomSpawnPosition(out Vector3 position)
    {
        position = Vector3.zero;
        if (!boundsReady)
        {
            return false;
        }

        position = new Vector3(
            Random.Range(minLeftX, maxRightX),
            Random.Range(minY, maxY),
            0f);
        return true;
    }

    /// <summary>延迟后将视口范围转换为世界坐标边界。</summary>
    /// <returns>协程迭代器。</returns>
    private IEnumerator InitializeBoundsRoutine()
    {
        boundsReady = false;
        if (initializeDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(initializeDelaySeconds);
        }

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
