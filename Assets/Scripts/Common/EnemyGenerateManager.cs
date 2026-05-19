using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGenerateManager : MonoBehaviour
{
    [Header("生成位置")]
    private Camera mainCamera;
    private float minLeftPosition;
    private float maxRightPosition;
    private float minYPosition;
    private float maxYPosition;

    [Header("生成enemy机制")]
    //每个波次可以生成的最大enemy数量
    private int maxSpawnCount;
    //当前波次的时间
    [SerializeField] private float spawnCoolDownTimer;
    [SerializeField] private bool canAutoGenerateEnenmy;

    //当前波次内可以创建enemy的次数
    private int spawnCount;
    //每次创建敌人的随机数量（随机值，但是保证 enemySpawnAmount * spawnCount < maxSpawnCount）
    private int enemySpawnAmount;
    //当前波次时间内，每次生成的enemy的间隔
    [SerializeField] private float spawnInterval;
    //当前波次时间内，每次生成的enemy的间隔
    [SerializeField] private float _baseSpawnInterval;

    //用于保存在不同时间段内可以产生的enemy的数量
    private Dictionary<int, List<GameObject>> enemyList = new Dictionary<int, List<GameObject>>();

    [SerializeField] private GameObject[] enemyPrefabs;
    [Tooltip("与 enemyPrefabs 一一对应的对象池 Key；留空或不足时使用 GameConstants.PoolKeys.Enemy")]
    [SerializeField] private string[] enemyPoolKeys;

    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        _baseSpawnInterval = spawnInterval > 0.05f ? spawnInterval : 1.5f;
        //初始化 spawn信息
        spawnCoolDownTimer = 0;
        StartCoroutine(DelayInitialEnemyBounds());
    }

    private void Update()
    {
            spawnCoolDownTimer += Time.deltaTime;
            if (spawnCoolDownTimer > spawnInterval && canAutoGenerateEnenmy)
        {
            CreateEnemy();
            spawnCoolDownTimer = 0;
        }
    }

    private void CreateEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            return;
        }

        var xPosition = Random.Range(minLeftPosition, maxRightPosition);
        var yPosition = Random.Range(minYPosition, maxYPosition);
        var spawnPosition = new Vector3(xPosition, yPosition, 0f);
        int index = Random.Range(0, enemyPrefabs.Length);
        string poolKey = ResolveEnemyPoolKey(index);

        if (ServiceLocator.TryGet(out PoolManager poolManager) &&
            poolManager.HasPool(poolKey) &&
            poolManager.Spawn(poolKey, spawnPosition, Quaternion.identity) != null)
        {
            return;
        }

        Instantiate(enemyPrefabs[index], spawnPosition, Quaternion.identity);
    }

    private string ResolveEnemyPoolKey(int prefabIndex)
    {
        if (enemyPoolKeys != null &&
            prefabIndex >= 0 &&
            prefabIndex < enemyPoolKeys.Length &&
            !string.IsNullOrEmpty(enemyPoolKeys[prefabIndex]))
        {
            return enemyPoolKeys[prefabIndex];
        }

        return GameConstants.PoolKeys.Enemy;
    }

    /// <summary>
    /// 初始化Enemy的生成区域，控制在一个矩形区域内
    /// </summary>
    /// <returns>延迟初始化，保证可以正常获取到mainCamera</returns>
    public IEnumerator DelayInitialEnemyBounds()
    {
        yield return new WaitForSeconds(.2f);
        minLeftPosition = mainCamera.ViewportToWorldPoint(new Vector3(0.1f, 0f)).x;
        maxRightPosition = mainCamera.ViewportToWorldPoint(new Vector3(0.9f, 0f)).x;
        minYPosition = mainCamera.ViewportToWorldPoint(new Vector3(0, 0.9f)).y;
        maxYPosition = mainCamera.ViewportToWorldPoint(new Vector3(0, 1.1f)).y;
    }
}
