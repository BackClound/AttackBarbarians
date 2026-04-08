using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
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

    //用于保存在不同时间段内可以产生的enemy的数量
    private Dictionary<int, List<GameObject>> enemyList = new Dictionary<int, List<GameObject>>();

    [SerializeField] private GameObject[] enemyPrefabs;

    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
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
        var xPosition = Random.Range(minLeftPosition, maxRightPosition);
        var yPosition = Random.Range(minYPosition, maxYPosition);
        int index = Random.Range(0, enemyPrefabs.Length);
        Instantiate(enemyPrefabs[index], new Vector3(xPosition, yPosition), Quaternion.identity);
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
