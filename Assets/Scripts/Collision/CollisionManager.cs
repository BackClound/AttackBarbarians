using UnityEngine;

/// <summary>
/// 统一碰撞查询调度：NonAlloc Overlap / Raycast、敌人与墙体解析。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 <c>GameSystems</c> 根或子物体上。</para>
/// <para><b>获取方式：</b><see cref="ServiceLocator.Get{T}"/> / <see cref="CollisionQuery"/> 静态入口。</para>
/// </remarks>
public class CollisionManager : MonoBehaviour, IGameSystem
{
    [SerializeField] private CollisionDataSO dataOverride;
    [SerializeField] private string configResourcePath = GameConstants.ResourcePaths.CollisionDefault;

    private CollisionDataSO activeData;
    private Collider2D[] overlapBuffer;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;
    public CollisionDataSO ActiveData => activeData;

    public void Initialize()
    {
        activeData = ResolveData();
        int capacity = activeData != null ? activeData.MaxOverlapResults : 48;
        overlapBuffer = new Collider2D[capacity];
        isInitialized = true;
    }

    public void Tick(float deltaTime) { }

    public void Shutdown()
    {
        isInitialized = false;
        activeData = null;
        overlapBuffer = null;
    }

    public int OverlapCircle(Vector2 center, float radius, LayerMask layerMask, Collider2D[] externalBuffer = null)
    {
        Collider2D[] buffer = externalBuffer ?? overlapBuffer;
        if (buffer == null || buffer.Length == 0)
        {
            return 0;
        }

        return Physics2D.OverlapCircleNonAlloc(center, radius, buffer, layerMask);
    }

    public bool Raycast(
        Vector2 origin,
        Vector2 direction,
        float distance,
        LayerMask layerMask,
        out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(origin, direction.normalized, distance, layerMask);
        return hit.collider != null;
    }

    public LayerMask GetPlayerEnemyScanLayers(LayerMask fallback) =>
        activeData != null && activeData.PlayerEnemyScanLayers.value != 0
            ? activeData.PlayerEnemyScanLayers
            : fallback;

    public LayerMask GetProjectileEnemyLayers(LayerMask fallback) =>
        activeData != null && activeData.ProjectileEnemyLayers.value != 0
            ? activeData.ProjectileEnemyLayers
            : fallback;

    public LayerMask GetEnemyWallLayers(LayerMask fallback) =>
        activeData != null && activeData.EnemyWallLayers.value != 0
            ? activeData.EnemyWallLayers
            : fallback;

    public bool TryDetectWall(
        Vector2 origin,
        float distance,
        LayerMask layerMask,
        out RaycastHit2D hit,
        out WallControlManager wall)
    {
        wall = null;
        if (!Raycast(origin, Vector2.down, distance, layerMask, out hit))
        {
            return false;
        }

        return CollisionQuery.TryResolveWall(hit, out wall);
    }

    public bool TryProcessProjectileHit(
        Collider2D collider,
        out Enemy enemy,
        out GameObject hitObject)
    {
        enemy = null;
        hitObject = null;
        if (!CollisionQuery.TryResolveEnemy(collider, out enemy))
        {
            return false;
        }

        hitObject = enemy.gameObject;
        return enemy.enemy_Health != null && enemy.enemy_Health.CanBeDamage();
    }

    private CollisionDataSO ResolveData()
    {
        if (dataOverride != null)
        {
            return dataOverride;
        }

        return Resources.Load<CollisionDataSO>(configResourcePath);
    }
}
