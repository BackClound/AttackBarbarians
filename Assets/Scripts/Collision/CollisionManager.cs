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

    /// <summary>系统是否已完成初始化。</summary>
    public bool IsInitialized => isInitialized;
    /// <summary>当前生效的碰撞配置数据。</summary>
    public CollisionDataSO ActiveData => activeData;

    /// <summary>
    /// 加载碰撞配置并分配 NonAlloc 查询缓冲区。
    /// </summary>
    public void Initialize()
    {
        activeData = ResolveData();
        int capacity = activeData != null ? activeData.MaxOverlapResults : 48;
        overlapBuffer = new Collider2D[capacity];
        isInitialized = true;
    }

    /// <summary>每帧更新（当前无逻辑）。</summary>
    /// <param name="deltaTime">帧间隔时间。</param>
    public void Tick(float deltaTime) { }

    /// <summary>
    /// 关闭系统并释放缓冲区与配置引用。
    /// </summary>
    public void Shutdown()
    {
        isInitialized = false;
        activeData = null;
        overlapBuffer = null;
    }

    /// <summary>
    /// 在指定圆形区域内 NonAlloc 查询碰撞体。
    /// </summary>
    /// <param name="center">圆心世界坐标。</param>
    /// <param name="radius">查询半径。</param>
    /// <param name="layerMask">层级掩码。</param>
    /// <param name="externalBuffer">外部缓冲区，为空时使用内部缓冲。</param>
    /// <returns>命中的碰撞体数量。</returns>
    public int OverlapCircle(Vector2 center, float radius, LayerMask layerMask, Collider2D[] externalBuffer = null)
    {
        Collider2D[] buffer = externalBuffer ?? overlapBuffer;
        if (buffer == null || buffer.Length == 0)
        {
            return 0;
        }

        return Physics2D.OverlapCircleNonAlloc(center, radius, buffer, layerMask);
    }

    /// <summary>
    /// 发射 2D 射线并返回首个命中。
    /// </summary>
    /// <param name="origin">射线起点。</param>
    /// <param name="direction">射线方向。</param>
    /// <param name="distance">最大距离。</param>
    /// <param name="layerMask">层级掩码。</param>
    /// <param name="hit">命中信息。</param>
    /// <returns>是否命中碰撞体。</returns>
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

    /// <summary>
    /// 获取玩家扫描敌人的层级掩码，配置无效时使用 fallback。
    /// </summary>
    /// <param name="fallback">备用层级掩码。</param>
    /// <returns>生效的层级掩码。</returns>
    public LayerMask GetPlayerEnemyScanLayers(LayerMask fallback) =>
        activeData != null && activeData.PlayerEnemyScanLayers.value != 0
            ? activeData.PlayerEnemyScanLayers
            : fallback;

    /// <summary>
    /// 获取投射物命中敌人的层级掩码，配置无效时使用 fallback。
    /// </summary>
    /// <param name="fallback">备用层级掩码。</param>
    /// <returns>生效的层级掩码。</returns>
    public LayerMask GetProjectileEnemyLayers(LayerMask fallback) =>
        activeData != null && activeData.ProjectileEnemyLayers.value != 0
            ? activeData.ProjectileEnemyLayers
            : fallback;

    /// <summary>
    /// 获取敌人检测墙体的层级掩码，配置无效时使用 fallback。
    /// </summary>
    /// <param name="fallback">备用层级掩码。</param>
    /// <returns>生效的层级掩码。</returns>
    public LayerMask GetEnemyWallLayers(LayerMask fallback) =>
        activeData != null && activeData.EnemyWallLayers.value != 0
            ? activeData.EnemyWallLayers
            : fallback;

    /// <summary>
    /// 向下射线检测墙体并解析 <see cref="WallControlManager"/>。
    /// </summary>
    /// <param name="origin">射线起点。</param>
    /// <param name="distance">射线距离。</param>
    /// <param name="layerMask">层级掩码。</param>
    /// <param name="hit">命中信息。</param>
    /// <param name="wall">解析到的墙体控制器。</param>
    /// <returns>是否成功检测到墙体。</returns>
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

    /// <summary>
    /// 处理投射物碰撞：解析敌人并校验是否可受伤。
    /// </summary>
    /// <param name="collider">命中的碰撞体。</param>
    /// <param name="enemy">解析到的敌人组件。</param>
    /// <param name="hitObject">受击 GameObject。</param>
    /// <returns>是否为有效敌人命中。</returns>
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

    /// <summary>
    /// 解析生效的碰撞配置（优先 Inspector 覆盖，否则从 Resources 加载）。
    /// </summary>
    /// <returns>碰撞配置资产，可能为 null。</returns>
    private CollisionDataSO ResolveData()
    {
        if (dataOverride != null)
        {
            return dataOverride;
        }

        return Resources.Load<CollisionDataSO>(configResourcePath);
    }
}
