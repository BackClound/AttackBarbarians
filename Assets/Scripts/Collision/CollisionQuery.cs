using UnityEngine;

/// <summary>
/// NonAlloc 物理查询工具；Bootstrap 未完成时可直接调用，有 <see cref="CollisionManager"/> 时优先走 Manager 缓冲。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。</remarks>
public static class CollisionQuery
{
    private static readonly Collider2D[] SharedBuffer = new Collider2D[64];

    /// <summary>
    /// 圆形区域 NonAlloc 重叠查询。
    /// </summary>
    /// <param name="center">圆心世界坐标。</param>
    /// <param name="radius">查询半径。</param>
    /// <param name="layerMask">层级掩码。</param>
    /// <param name="buffer">结果缓冲区，为空时使用内部共享缓冲。</param>
    /// <returns>命中的碰撞体数量。</returns>
    public static int OverlapCircleNonAlloc(
        Vector2 center,
        float radius,
        LayerMask layerMask,
        Collider2D[] buffer)
    {
        if (ServiceLocator.TryGet(out CollisionManager manager))
        {
            return manager.OverlapCircle(center, radius, layerMask, buffer);
        }

        Collider2D[] useBuffer = buffer ?? SharedBuffer;
        return Physics2D.OverlapCircleNonAlloc(center, radius, useBuffer, layerMask);
    }

    /// <summary>
    /// 发射 2D 射线，优先经 <see cref="CollisionManager"/> 调度。
    /// </summary>
    /// <param name="origin">射线起点。</param>
    /// <param name="direction">射线方向。</param>
    /// <param name="distance">最大距离。</param>
    /// <param name="layerMask">层级掩码。</param>
    /// <param name="hit">命中信息。</param>
    /// <returns>是否命中碰撞体。</returns>
    public static bool Raycast(
        Vector2 origin,
        Vector2 direction,
        float distance,
        LayerMask layerMask,
        out RaycastHit2D hit)
    {
        if (ServiceLocator.TryGet(out CollisionManager manager))
        {
            return manager.Raycast(origin, direction, distance, layerMask, out hit);
        }

        hit = Physics2D.Raycast(origin, direction, distance, layerMask);
        return hit.collider != null;
    }

    /// <summary>
    /// 解析碰撞体所属的根 GameObject（优先 Rigidbody 宿主）。
    /// </summary>
    /// <param name="collider">碰撞体。</param>
    /// <returns>根对象，collider 为空时返回 null。</returns>
    public static GameObject ResolveRoot(Collider2D collider)
    {
        if (collider == null)
        {
            return null;
        }

        if (collider.attachedRigidbody != null)
        {
            return collider.attachedRigidbody.gameObject;
        }

        return collider.gameObject;
    }

    /// <summary>
    /// 从碰撞体解析敌人组件（需 Enemy 标签）。
    /// </summary>
    /// <param name="collider">碰撞体。</param>
    /// <param name="enemy">解析到的敌人。</param>
    /// <returns>是否成功解析。</returns>
    public static bool TryResolveEnemy(Collider2D collider, out Enemy enemy)
    {
        enemy = null;
        GameObject root = ResolveRoot(collider);
        if (root == null)
        {
            return false;
        }

        if (!root.CompareTag(GameConstants.Tags.Enemy))
        {
            return false;
        }

        enemy = root.GetComponent<Enemy>();
        return enemy != null;
    }

    /// <summary>
    /// 从射线命中解析墙体控制器。
    /// </summary>
    /// <param name="hit">射线命中信息。</param>
    /// <param name="wall">解析到的墙体控制器。</param>
    /// <returns>是否成功解析。</returns>
    public static bool TryResolveWall(RaycastHit2D hit, out WallControlManager wall)
    {
        wall = null;
        if (hit.collider == null)
        {
            return false;
        }

        wall = hit.collider.GetComponent<WallControlManager>();
        if (wall == null)
        {
            wall = hit.collider.GetComponentInParent<WallControlManager>();
        }

        return wall != null;
    }
}
