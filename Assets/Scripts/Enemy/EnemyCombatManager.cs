using UnityEngine;

/// <summary>
/// 兼容层：旧 Prefab 仍挂 <c>EnemyCombatManager</c> 时转发到 <see cref="EnemyCombatBridge"/>。
/// </summary>
[System.Obsolete("Use EnemyCombatBridge. Wall raycast and damage moved to EnemyController + CollisionManager.")]
public class EnemyCombatManager : EnemyCombatBridge
{
    [SerializeField] protected float maxCheckDistance = 1f;
    [SerializeField] protected Transform checkPosition;
    [SerializeField] protected LayerMask enemyLayer;
    [SerializeField] protected string enemyTag;
}
