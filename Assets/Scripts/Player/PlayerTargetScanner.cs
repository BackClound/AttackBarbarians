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

    public IReadOnlyList<Enemy> Results => results;
    public Enemy PrimaryTarget { get; private set; }

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

    public void SetScanOrigin(Vector2 origin) => scanOrigin = origin;

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

    public void CopyResultsTo(List<Enemy> destination)
    {
        destination.Clear();
        for (int i = 0; i < results.Count; i++)
        {
            destination.Add(results[i]);
        }
    }

    private void SortByPolicy(List<Enemy> enemies)
    {
        Vector2 wallPoint = wallReference != null ? (Vector2)wallReference.position : fallbackWallPoint;
        targetComparer.Configure(policy, wallPoint, scanOrigin);
        enemies.Sort(targetComparer);
    }

    private sealed class EnemyTargetComparer : IComparer<Enemy>
    {
        private PlayerTargetPolicy activePolicy;
        private Vector2 wallPoint;
        private Vector2 origin;

        public void Configure(PlayerTargetPolicy targetPolicy, Vector2 wall, Vector2 scanOrigin)
        {
            activePolicy = targetPolicy;
            wallPoint = wall;
            origin = scanOrigin;
        }

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
