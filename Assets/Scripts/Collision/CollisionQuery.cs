using UnityEngine;

/// <summary>
/// NonAlloc 物理查询工具；Bootstrap 未完成时可直接调用，有 <see cref="CollisionManager"/> 时优先走 Manager 缓冲。
/// </summary>
/// <remarks><b>是否需要挂载：</b>否。</remarks>
public static class CollisionQuery
{
    private static readonly Collider2D[] SharedBuffer = new Collider2D[64];

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
