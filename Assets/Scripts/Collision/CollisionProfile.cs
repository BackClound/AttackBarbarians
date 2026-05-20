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

    public CollisionRole Role => role;
    public Transform ProbeOrigin => probeOrigin != null ? probeOrigin : transform;
    public float RayDistance => Mathf.Max(0.05f, rayDistance);
    public LayerMask RayLayerMask => rayLayerMask;

    public Vector2 ProbePosition => ProbeOrigin.position;

    public bool TryRaycastDown(out RaycastHit2D hit)
    {
        Vector2 origin = ProbePosition;
        LayerMask mask = rayLayerMask.value != 0 ? rayLayerMask : (LayerMask)(1 << 0);
        return CollisionQuery.Raycast(origin, Vector2.down, RayDistance, mask, out hit);
    }
}
