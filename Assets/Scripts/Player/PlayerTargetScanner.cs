using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家射程内敌人扫描与目标排序（经 <see cref="CollisionQuery"/> NonAlloc 查询）。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否。由 <see cref="PlayerController"/> 持有并配置。</para>
/// </remarks>
public sealed class PlayerTargetScanner
{
    private const int MaxOverlapResults = 48;
    private readonly Collider2D[] overlapBuffer = new Collider2D[MaxOverlapResults];
    private readonly List<Enemy> results = new List<Enemy>(24);
    private readonly EnemyTargetComparer targetComparer = new EnemyTargetComparer();

    private Vector2 scanOrigin;
    private float scanRadius = 25f;
    private LayerMask enemyLayer;
    private string enemyTag = GameConstants.Tags.Enemy;
    private PlayerTargetPolicy policy = PlayerTargetPolicy.NearestToWall;
    private Transform wallReference;
    private Vector2 fallbackWallPoint;

    /// <summary>最近一次扫描命中的敌人列表（已按策略排序）。</summary>
    public IReadOnlyList<Enemy> Results => results;
    /// <summary>排序后的首要攻击目标。</summary>
    public Enemy PrimaryTarget { get; private set; }

    /// <summary>
    /// 配置扫描原点、半径、层级与目标选择策略。
    /// </summary>
    /// <param name="scanTransform">扫描原点 Transform。</param>
    /// <param name="radius">圆形扫描半径。</param>
    /// <param name="layerMask">敌人层级掩码。</param>
    /// <param name="tag">敌人 Tag 过滤，空则使用默认。</param>
    /// <param name="targetPolicy">目标排序策略。</param>
    /// <param name="wallTransform">墙体参考点，用于「靠近城墙」策略。</param>
    /// <param name="wallFallback">墙体参考点缺失时的备用坐标。</param>
    public void Configure(
        Transform scanTransform,
        float radius,
        LayerMask layerMask,
        string tag,
        PlayerTargetPolicy targetPolicy,
        Transform wallTransform,
        Vector2 wallFallback)
    {
        scanOrigin = scanTransform != null ? scanTransform.position : Vector2.zero;
        scanRadius = Mathf.Max(0.1f, radius);
        enemyLayer = layerMask;
        enemyTag = string.IsNullOrEmpty(tag) ? GameConstants.Tags.Enemy : tag;
        policy = targetPolicy;
        wallReference = wallTransform;
        fallbackWallPoint = wallFallback;
    }

    /// <summary>
    /// 更新扫描原点世界坐标。
    /// </summary>
    /// <param name="origin">新的扫描原点。</param>
    public void SetScanOrigin(Vector2 origin) => scanOrigin = origin;

    /// <summary>
    /// 执行一次圆形 NonAlloc 扫描，过滤可受伤敌人并按策略排序。
    /// </summary>
    /// <returns>命中的有效敌人数量。</returns>
    public int Scan()
    {
        results.Clear();
        PrimaryTarget = null;

        int hitCount = CollisionQuery.OverlapCircleNonAlloc(scanOrigin, scanRadius, enemyLayer, overlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D collider = overlapBuffer[i];
            if (collider == null)
            {
                continue;
            }

            if (!CollisionQuery.TryResolveEnemy(collider, out Enemy enemy))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(enemyTag) && !enemy.CompareTag(enemyTag))
            {
                continue;
            }

            if (enemy.enemy_Health == null || !enemy.enemy_Health.CanBeDamage())
            {
                continue;
            }

            results.Add(enemy);
        }

        if (results.Count == 0)
        {
            return 0;
        }

        SortByPolicy(results);
        PrimaryTarget = results[0];
        return results.Count;
    }

    /// <summary>
    /// 将扫描结果复制到外部列表。
    /// </summary>
    /// <param name="destination">目标列表，调用前会被清空。</param>
    public void CopyResultsTo(List<Enemy> destination)
    {
        destination.Clear();
        for (int i = 0; i < results.Count; i++)
        {
            destination.Add(results[i]);
        }
    }

    /// <summary>
    /// 按当前 <see cref="PlayerTargetPolicy"/> 对敌人列表排序。
    /// </summary>
    /// <param name="enemies">待排序的敌人列表。</param>
    private void SortByPolicy(List<Enemy> enemies)
    {
        Vector2 wallPoint = wallReference != null ? (Vector2)wallReference.position : fallbackWallPoint;
        targetComparer.Configure(policy, wallPoint, scanOrigin);
        enemies.Sort(targetComparer);
    }

    /// <summary>
    /// 敌人目标比较器，按策略计算排序优先级。
    /// </summary>
    private sealed class EnemyTargetComparer : IComparer<Enemy>
    {
        private PlayerTargetPolicy activePolicy;
        private Vector2 wallPoint;
        private Vector2 origin;

        /// <summary>
        /// 配置比较策略与参考坐标。
        /// </summary>
        /// <param name="targetPolicy">目标选择策略。</param>
        /// <param name="wall">墙体参考点。</param>
        /// <param name="scanOrigin">扫描原点。</param>
        public void Configure(PlayerTargetPolicy targetPolicy, Vector2 wall, Vector2 scanOrigin)
        {
            activePolicy = targetPolicy;
            wallPoint = wall;
            origin = scanOrigin;
        }

        /// <summary>
        /// 比较两个敌人的攻击优先级。
        /// </summary>
        /// <param name="a">敌人 A。</param>
        /// <param name="b">敌人 B。</param>
        /// <returns>排序比较结果。</returns>
        public int Compare(Enemy a, Enemy b)
        {
            if (a == null || b == null)
            {
                return a == null ? (b == null ? 0 : -1) : 1;
            }

            if (activePolicy == PlayerTargetPolicy.BossFirst)
            {
                int bossCompare = b.IsBoss.CompareTo(a.IsBoss);
                if (bossCompare != 0)
                {
                    return bossCompare;
                }
            }

            switch (activePolicy)
            {
                case PlayerTargetPolicy.LowestHealth:
                    return a.enemy_Health.CurrentHp.CompareTo(b.enemy_Health.CurrentHp);

                case PlayerTargetPolicy.NearestToWall:
                case PlayerTargetPolicy.BossFirst:
                    float distA = ((Vector2)a.transform.position - wallPoint).sqrMagnitude;
                    float distB = ((Vector2)b.transform.position - wallPoint).sqrMagnitude;
                    return distA.CompareTo(distB);

                case PlayerTargetPolicy.Nearest:
                default:
                    float nearA = ((Vector2)a.transform.position - origin).sqrMagnitude;
                    float nearB = ((Vector2)b.transform.position - origin).sqrMagnitude;
                    return nearA.CompareTo(nearB);
            }
        }
    }
}
