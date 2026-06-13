using UnityEngine;

/// <summary>
/// 全局碰撞查询配置：LayerMask、默认半径与射线长度。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject 资产）。</para>
/// <para><b>路径：</b><c>Assets/Resources/Config/Collision/Collision_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "CollisionData", menuName = "Attack Barbarians/Config/Collision Data")]
public class CollisionDataSO : ScriptableObject
{
    [Header("Player Target Scan")]
    [SerializeField] private LayerMask playerEnemyScanLayers;
    [SerializeField] private float defaultPlayerScanRadius = 25f;

    [Header("Enemy Wall Attack")]
    [SerializeField] private LayerMask enemyWallLayers;
    [SerializeField] private float defaultWallRayDistance = 1.5f;

    [Header("Projectile Hit")]
    [SerializeField] private LayerMask projectileEnemyLayers;

    [Header("Buffers")]
    [SerializeField] private int maxOverlapResults = 48;

    /// <summary>玩家扫描敌人的层级掩码。</summary>
    public LayerMask PlayerEnemyScanLayers => playerEnemyScanLayers;
    /// <summary>玩家默认圆形扫描半径。</summary>
    public float DefaultPlayerScanRadius => Mathf.Max(0.1f, defaultPlayerScanRadius);
    /// <summary>敌人检测墙体的层级掩码。</summary>
    public LayerMask EnemyWallLayers => enemyWallLayers;
    /// <summary>敌人默认向下射线检测距离。</summary>
    public float DefaultWallRayDistance => Mathf.Max(0.05f, defaultWallRayDistance);
    /// <summary>投射物命中敌人的层级掩码。</summary>
    public LayerMask ProjectileEnemyLayers => projectileEnemyLayers;
    /// <summary>Overlap 查询缓冲区的最大容量。</summary>
    public int MaxOverlapResults => Mathf.Clamp(maxOverlapResults, 4, 128);
}
