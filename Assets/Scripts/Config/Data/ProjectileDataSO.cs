using UnityEngine;

/// <summary>
/// 投射物数值与表现配置：速度、碰撞、穿透、弹射、生命周期与对象池 Key。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>否（ScriptableObject）。</para>
/// <para><b>默认资产：</b><c>Assets/Resources/Config/Projectile/Projectile_Default.asset</c></para>
/// </remarks>
[CreateAssetMenu(fileName = "ProjectileData", menuName = "Attack Barbarians/Config/Projectile Data")]
public class ProjectileDataSO : ConfigDataBase
{
    [Header("Pool")]
    // 对象池键
    [SerializeField] private string poolKey = GameConstants.PoolKeys.Bullet;
    // 预制体
    [SerializeField] private GameObject prefab;

    [Header("Motion")]
    // 移动方式
    [SerializeField] private ProjectileMotionType motionType = ProjectileMotionType.Straight;
    // 移动速度
    [SerializeField] private float moveSpeed = 20f;
    // 追踪旋转速度
    [SerializeField] private float homingTurnRate = 360f;
    // 轨道半径
    [SerializeField] private float orbitRadius = 1f;
    // 轨道旋转速度
    [SerializeField] private float orbitAngularSpeed = 180f;

    [Header("Hit")]
    // 命中半径
    [SerializeField] private float hitRadius = 0.2f;
    // 命中层
    [SerializeField] private LayerMask hitLayerMask;
    // 最大命中次数
    [SerializeField] private int maxHitCount = 1;
    // 穿透次数
    [SerializeField] private int pierceCount;
    // 弹射次数
    [SerializeField] private int bounceCount;
    // 命中冷却时间
    [SerializeField] private float hitCooldownPerTarget = 0f;

    [Header("Lifetime")]
    // 生命周期
    [SerializeField] private float lifetimeSeconds = 4f;
    // 命中次数达到上限时是否销毁
    [SerializeField] private bool despawnOnHitLimit = true;

    [Header("VFX")]
    // 命中特效预制体
    [SerializeField] private GameObject hitEffectPrefab;

    public string PoolKey => string.IsNullOrEmpty(poolKey) ? GameConstants.PoolKeys.Bullet : poolKey;
    public GameObject Prefab => prefab;
    public ProjectileMotionType MotionType => motionType;
    public float MoveSpeed => Mathf.Max(0.01f, moveSpeed);
    public float HomingTurnRate => Mathf.Max(0f, homingTurnRate);
    public float OrbitRadius => Mathf.Max(0.01f, orbitRadius);
    public float OrbitAngularSpeed => orbitAngularSpeed;
    public float HitRadius => Mathf.Max(0.01f, hitRadius);
    public LayerMask HitLayerMask => hitLayerMask;
    public int MaxHitCount => Mathf.Max(1, maxHitCount);
    public int PierceCount => Mathf.Max(0, pierceCount);
    public int BounceCount => Mathf.Max(0, bounceCount);
    public float HitCooldownPerTarget => Mathf.Max(0f, hitCooldownPerTarget);
    public float LifetimeSeconds => Mathf.Max(0.05f, lifetimeSeconds);
    public bool DespawnOnHitLimit => despawnOnHitLimit;
    public GameObject HitEffectPrefab => hitEffectPrefab;

    public override void CollectValidationErrors(ConfigValidationResult result)
    {
        base.CollectValidationErrors(result);

        if (prefab == null)
        {
            result.AddWarning(name, "未配置 prefab，运行时将依赖对象池条目中的 Prefab。");
        }

        if (hitLayerMask.value == 0)
        {
            result.AddWarning(name, "hitLayerMask 未设置，投射物可能无法命中目标。");
        }
    }
}
