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

    public LayerMask PlayerEnemyScanLayers => playerEnemyScanLayers;
    public float DefaultPlayerScanRadius => Mathf.Max(0.1f, defaultPlayerScanRadius);
    public LayerMask EnemyWallLayers => enemyWallLayers;
    public float DefaultWallRayDistance => Mathf.Max(0.05f, defaultWallRayDistance);
    public LayerMask ProjectileEnemyLayers => projectileEnemyLayers;
    public int MaxOverlapResults => Mathf.Clamp(maxOverlapResults, 4, 128);
}
