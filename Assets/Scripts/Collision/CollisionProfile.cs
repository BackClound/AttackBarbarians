using UnityEngine;

/// <summary>
/// 实体碰撞探测点：可覆盖射线/圆形查询的原点与距离。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在 Player / Enemy / Wall 等需要物理探测的物体或其子节点。</para>
/// </remarks>
[DisallowMultipleComponent]
public class CollisionProfile : MonoBehaviour
{
    [SerializeField] private CollisionRole role = CollisionRole.None;
    [SerializeField] private Transform probeOrigin;
    [SerializeField] private float rayDistance = 1.5f;
    [SerializeField] private LayerMask rayLayerMask;

    /// <summary>碰撞体在战斗查询中的语义角色。</summary>
    public CollisionRole Role => role;
    /// <summary>探测射线/圆形的原点 Transform。</summary>
    public Transform ProbeOrigin => probeOrigin != null ? probeOrigin : transform;
    /// <summary>向下射线检测的最大距离。</summary>
    public float RayDistance => Mathf.Max(0.05f, rayDistance);
    /// <summary>射线检测使用的层级掩码。</summary>
    public LayerMask RayLayerMask => rayLayerMask;
    /// <summary>探测点的世界坐标。</summary>
    public Vector2 ProbePosition => ProbeOrigin.position;

    /// <summary>
    /// 从探测点向下发射射线。
    /// </summary>
    /// <param name="hit">命中信息。</param>
    /// <returns>是否命中碰撞体。</returns>
    public bool TryRaycastDown(out RaycastHit2D hit)
    {
        Vector2 origin = ProbePosition;
        LayerMask mask = rayLayerMask.value != 0 ? rayLayerMask : (LayerMask)(1 << 0);
        return CollisionQuery.Raycast(origin, Vector2.down, RayDistance, mask, out hit);
    }
}
