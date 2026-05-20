using System.Collections;
using UnityEngine;

/// <summary>
/// 根据相机视口计算敌人生成矩形区域（与旧 <see cref="EnemyGenerateManager"/> 算法一致）。
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

    public bool IsReady => boundsReady;

    private void Awake()
    {
        if (spawnCamera == null)
        {
            spawnCamera = Camera.main;
        }
    }

    public void BeginInitialize()
    {
        StopAllCoroutines();
        StartCoroutine(InitializeBoundsRoutine());
    }

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
