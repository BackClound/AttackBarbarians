using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 投射物实例：移动、命中检测、伤害提交与对象池回收。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在子弹/法术载体 Prefab 根节点（如 <c>Bullet.prefab</c>）。</para>
/// <para><b>依赖：</b><see cref="Rigidbody2D"/>、触发或碰撞体；由 <see cref="ProjectileManager"/> 调用 <see cref="BeginFlight"/> 启动。</para>
/// </remarks>
[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileController : MonoBehaviour, IPoolable
{
    private const int OverlapBufferSize = 8;

    private static readonly Collider2D[] OverlapBuffer = new Collider2D[OverlapBufferSize];

    [SerializeField] private ProjectileDataSO dataOverride;
    [SerializeField] private LayerMask fallbackHitLayers;
    [SerializeField] private bool useTriggerCollision = true;

    private Rigidbody2D rb;
    private ProjectileDataSO activeData;
    private ProjectileSpawnRequest activeRequest;
    private Vector2 moveDirection;
    private Transform homingTarget;
    private Vector3 orbitCenter;
    private float orbitAngleDegrees;
    private float lifetimeRemaining;
    private int hitsApplied;
    private int piercesRemaining;
    private int bouncesRemaining;
    // 分裂次数
    private int splitsRemaining;
    // 是否爆炸
    private bool isExploding;
    // 爆炸半径
    private float explosionRadius;
    // 爆炸冻结时间
    private float explosionFreezeDuration;
    // 爆炸伤害
    private float explosionDamage;
    private bool isFlying;
    // 是否触发半范围爆炸
    private bool halfRangeExplosionTriggered;
    // 初始生命周期
    private float initialLifetime;
    // 飞行覆盖参数
    private ProjectileRuntimeOverrides flightOverrides;
    // 对象池原始位置
    private Vector3 pooledOrigin;

    private readonly HashSet<int> hitInstanceIds = new HashSet<int>(8);
    private readonly Dictionary<int, float> hitCooldownUntil = new Dictionary<int, float>(8);
    private ContactFilter2D contactFilter;

    /// <summary>是否正在飞行中。</summary>
    public bool IsFlying => isFlying;
    /// <summary>当前生效的投射物配置。</summary>
    public ProjectileDataSO ActiveData => activeData;

    /// <summary>缓存刚体与对象池原点。</summary>
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        pooledOrigin = transform.position;
        BuildContactFilter();
    }

    /// <summary>每帧更新生命周期、运动与命中检测。</summary>
    private void Update()
    {
        if (!isFlying || activeData == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        lifetimeRemaining -= deltaTime;
        if (flightOverrides.ExplodeAtHalfLifetime && !halfRangeExplosionTriggered
            && initialLifetime > 0f && lifetimeRemaining <= initialLifetime * 0.5f)
        {
            halfRangeExplosionTriggered = true;
            TriggerExplosion(flightOverrides.ExplosionRadius, flightOverrides.ExplosionFreezeDuration);
            Recycle();
            return;
        }

        if (lifetimeRemaining <= 0f)
        {
            Recycle();
            return;
        }

        UpdateMotion(deltaTime);
        if (!useTriggerCollision)
        {
            ScanHitsNonAlloc();
        }
    }

    /// <summary>由 <see cref="ProjectileManager"/> 在池 <see cref="IPoolable.OnSpawn"/> 之后调用。</summary>
    /// <param name="request">生成请求。</param>
    /// <param name="data">投射物配置。</param>
    /// <param name="overrides">技能 Buff 飞行覆盖参数。</param>
    public void BeginFlight(ProjectileSpawnRequest request, ProjectileDataSO data, ProjectileRuntimeOverrides overrides = default)
    {
        activeRequest = request;
        activeData = data != null ? data : dataOverride;
        if (activeData == null)
        {
            Debug.LogWarning("[ProjectileController] 缺少 ProjectileDataSO，无法发射。");
            Recycle();
            return;
        }

        BuildContactFilter();
        ResetFlightState();

        transform.position = request.SpawnPosition;
        moveDirection = request.Direction;
        homingTarget = request.Target != null ? request.Target.transform : null;
        orbitCenter = request.SpawnPosition;
        orbitAngleDegrees = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        flightOverrides = overrides;
        lifetimeRemaining = activeData.LifetimeSeconds;
        if (overrides.MaxLifetimeScale > 0f)
        {
            lifetimeRemaining *= overrides.MaxLifetimeScale;
        }

        initialLifetime = lifetimeRemaining;
        piercesRemaining = activeData.PierceCount + overrides.PierceBonus;
        bouncesRemaining = overrides.BounceCount > 0 ? overrides.BounceCount : activeData.BounceCount;
        splitsRemaining = overrides.SplitOnHitCount;
        halfRangeExplosionTriggered = false;

        float zAngle = orbitAngleDegrees;
        transform.rotation = Quaternion.Euler(0f, 0f, zAngle);
        isFlying = true;
        ApplyVelocity();
    }

    /// <summary>对象池取出回调：重置飞行状态。</summary>
    public void OnSpawn()
    {
        ResetFlightState();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    /// <summary>对象池回收回调：清理飞行数据并复位。</summary>
    public void OnDespawn()
    {
        isFlying = false;
        activeRequest = default;
        activeData = null;
        homingTarget = null;
        moveDirection = Vector2.zero;
        hitsApplied = 0;
        piercesRemaining = 0;
        bouncesRemaining = 0;
        lifetimeRemaining = 0f;
        hitInstanceIds.Clear();
        hitCooldownUntil.Clear();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        CancelInvoke();
        transform.position = pooledOrigin;
    }

    /// <summary>触发器碰撞命中入口。</summary>
    /// <param name="other">命中的碰撞体。</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerCollision || !isFlying)
        {
            return;
        }

        TryHitCollider(other);
    }

    /// <summary>重置所有飞行相关运行时状态。</summary>
    private void ResetFlightState()
    {
        isFlying = false;
        hitsApplied = 0;
        piercesRemaining = 0;
        bouncesRemaining = 0;
        splitsRemaining = 0;
        halfRangeExplosionTriggered = false;
        initialLifetime = 0f;
        flightOverrides = default;
        lifetimeRemaining = 0f;
        hitInstanceIds.Clear();
        hitCooldownUntil.Clear();
        moveDirection = Vector2.zero;
        homingTarget = null;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
    }

    /// <summary>构建命中检测的 ContactFilter（含 CollisionManager 层级修正）。</summary>
    private void BuildContactFilter()
    {
        LayerMask mask = activeData != null ? activeData.HitLayerMask : fallbackHitLayers;
        if (ServiceLocator.TryGet(out CollisionManager collisionManager))
        {
            mask = collisionManager.GetProjectileEnemyLayers(mask);
        }

        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = mask,
            useTriggers = true,
        };
    }

    /// <summary>按 <see cref="ProjectileMotionType"/> 更新运动轨迹。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    private void UpdateMotion(float deltaTime)
    {
        switch (activeData.MotionType)
        {
            // 追踪
            case ProjectileMotionType.Homing:
                UpdateHoming(deltaTime);
                break;
            // 轨道
            case ProjectileMotionType.Orbit:
                UpdateOrbit(deltaTime);
                break;
            // 弧线
            case ProjectileMotionType.ArcToPoint:
                UpdateArcToPoint(deltaTime);
                break;
            // 链式
            case ProjectileMotionType.Chain:
            case ProjectileMotionType.Straight:
            default:
                break;
        }

        ApplyVelocity();
    }

    /// <summary>追踪运动：朝 homing 目标转向。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    private void UpdateHoming(float deltaTime)
    {
        if (homingTarget == null)
        {
            return;
        }

        Vector2 toTarget = (Vector2)homingTarget.position - (Vector2)transform.position;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 desired = new Vector3(toTarget.x, toTarget.y, 0f).normalized;
        Vector3 current = new Vector3(moveDirection.x, moveDirection.y, 0f);
        float maxRadians = activeData.HomingTurnRate * Mathf.Deg2Rad * deltaTime;
        Vector3 rotated = Vector3.RotateTowards(current, desired, maxRadians, 0f);
        moveDirection = new Vector2(rotated.x, rotated.y).normalized;
        float zAngle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, zAngle);
    }

    /// <summary>轨道运动：绕中心点圆周运动。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    private void UpdateOrbit(float deltaTime)
    {
        orbitAngleDegrees += activeData.OrbitAngularSpeed * deltaTime;
        float rad = orbitAngleDegrees * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * activeData.OrbitRadius;
        transform.position = orbitCenter + (Vector3)offset;
        moveDirection = offset.normalized;
    }

    /// <summary>弧线运动：朝目标点插值转向。</summary>
    /// <param name="deltaTime">帧间隔（秒）。</param>
    private void UpdateArcToPoint(float deltaTime)
    {
        if (homingTarget == null)
        {
            return;
        }

        Vector2 toTarget = (Vector2)homingTarget.position - (Vector2)transform.position;
        if (toTarget.sqrMagnitude > 0.0001f)
        {
            moveDirection = Vector2.MoveTowards(moveDirection, toTarget.normalized, deltaTime * 2f).normalized;
        }
    }

    /// <summary>将当前方向写入刚体速度。</summary>
    private void ApplyVelocity()
    {
        if (rb == null || moveDirection == Vector2.zero)
        {
            return;
        }

        rb.velocity = moveDirection * activeData.MoveSpeed;
    }

    /// <summary>非触发模式下圆形 NonAlloc 扫描命中。</summary>
    private void ScanHitsNonAlloc()
    {
        float hitRadius = activeData.HitRadius;
        if (flightOverrides.HitRadiusScale > 0f)
        {
            hitRadius *= Mathf.Max(0.5f, flightOverrides.HitRadiusScale);
        }

        int count = Physics2D.OverlapCircle(
            transform.position,
            hitRadius,
            contactFilter,
            OverlapBuffer);

        for (int i = 0; i < count; i++)
        {
            TryHitCollider(OverlapBuffer[i]);
        }
    }

    /// <summary>尝试对单个碰撞体结算命中（含穿透/冷却去重）。</summary>
    /// <param name="collider">命中的碰撞体。</param>
    private void TryHitCollider(Collider2D collider)
    {
        if (collider == null)
        {
            return;
        }

        Enemy enemy;
        GameObject hitObject;
        if (ServiceLocator.TryGet(out CollisionManager collisionManager))
        {
            if (!collisionManager.TryProcessProjectileHit(collider, out enemy, out hitObject))
            {
                return;
            }
        }
        else if (!CollisionQuery.TryResolveEnemy(collider, out enemy))
        {
            return;
        }
        else
        {
            hitObject = enemy.gameObject;
            if (enemy.enemy_Health == null || !enemy.enemy_Health.CanBeDamage())
            {
                return;
            }
        }

        int instanceId = hitObject.GetInstanceID();
        if (hitInstanceIds.Contains(instanceId))
        {
            return;
        }

        if (activeData.HitCooldownPerTarget > 0f
            && hitCooldownUntil.TryGetValue(instanceId, out float until)
            && Time.time < until)
        {
            return;
        }

        ApplyHit(enemy, hitObject, instanceId);
    }

    /// <summary>对敌人应用伤害、状态效果、分裂与命中计数。</summary>
    /// <param name="enemy">命中的敌人。</param>
    /// <param name="hitObject">受击 GameObject。</param>
    /// <param name="instanceId">目标实例 Id（用于去重）。</param>
    private void ApplyHit(Enemy enemy, GameObject hitObject, int instanceId)
    {
        DamageInfo info = activeRequest.DamageInfo.Target != null
            ? activeRequest.DamageInfo
            : activeRequest.DamageInfo.WithTarget(hitObject);

        DamageResult result = DamagePipeline.Apply(info);
        if (result.FinalDamage <= 0f)
        {
            return;
        }

        ApplyStatusOnHit(enemy);
        if (splitsRemaining > 0)
        {
            SpawnSplitProjectiles(hitObject.transform.position);
            splitsRemaining = 0;
        }

        hitsApplied++;
        int maxHits = flightOverrides.MaxHitCountOverride > 0
            ? flightOverrides.MaxHitCountOverride
            : activeData.MaxHitCount;
        hitInstanceIds.Add(instanceId);
        if (activeData.HitCooldownPerTarget > 0f)
        {
            hitCooldownUntil[instanceId] = Time.time + activeData.HitCooldownPerTarget;
        }

        SpawnHitEffect(hitObject.transform.position);
        GameEvents.RaiseProjectileHit(
            activeRequest.Source ?? gameObject,
            new ProjectileHitEventArgs(
                gameObject,
                hitObject,
                result.FinalDamage,
                result.IsCritical,
                activeRequest.SkillId,
                activeData.MotionType));

        bool reachedHitLimit = hitsApplied >= maxHits;
        if (piercesRemaining > 0)
        {
            piercesRemaining--;
            if (!reachedHitLimit)
            {
                return;
            }
        }

        if (activeData.DespawnOnHitLimit && reachedHitLimit)
        {
            Recycle();
        }
    }

    /// <summary>在命中点生成命中特效。</summary>
    /// <param name="position">世界坐标。</param>
    private void SpawnHitEffect(Vector3 position)
    {
        if (activeData.HitEffectPrefab != null)
        {
            CombatEffectSpawner.TrySpawnHitEffect(activeData.HitEffectPrefab, position, Quaternion.identity);
        }
    }

    /// <summary>命中时施加冰冻等控制状态。</summary>
    /// <param name="enemy">命中的敌人。</param>
    private void ApplyStatusOnHit(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        EnemyStatusController status = enemy.GetComponent<EnemyStatusController>();
        if (status == null)
        {
            return;
        }

        if (flightOverrides.OnHitFreezeDuration > 0f)
        {
            status.ApplyFreeze(flightOverrides.OnHitFreezeDuration);
        }
    }

    /// <summary>在指定半径内对敌人造成范围伤害与冰冻。</summary>
    /// <param name="radius">爆炸半径。</param>
    /// <param name="freezeDuration">冰冻持续时间（秒）。</param>
    private void TriggerExplosion(float radius, float freezeDuration)
    {
        if (radius <= 0f)
        {
            return;
        }

        LayerMask layers = activeData != null ? activeData.HitLayerMask : fallbackHitLayers;
        if (ServiceLocator.TryGet(out CollisionManager collisionManager))
        {
            layers = collisionManager.GetProjectileEnemyLayers(layers);
        }
        var contactLayerFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = layers,
            useTriggers = true,
        };
        int count = Physics2D.OverlapCircle(transform.position, radius, contactLayerFilter, OverlapBuffer);
        for (int i = 0; i < count; i++)
        {
            if (!CollisionQuery.TryResolveEnemy(OverlapBuffer[i], out Enemy enemy))
            {
                continue;
            }

            DamageInfo info = activeRequest.DamageInfo.WithTarget(enemy.gameObject);
            DamagePipeline.Apply(info);
            if (freezeDuration > 0f)
            {
                enemy.GetComponent<EnemyStatusController>()?.ApplyFreeze(freezeDuration);
            }
        }
    }

    /// <summary>命中时向两侧分裂出子投射物。</summary>
    /// <param name="origin">分裂起点。</param>
    private void SpawnSplitProjectiles(Vector3 origin)
    {
        if (!ServiceLocator.TryGet(out ProjectileManager manager))
        {
            return;
        }

        const float splitAngle = 25f;
        for (int i = -1; i <= 1; i += 2)
        {
            Vector2 dir = Rotate(moveDirection, splitAngle * i);
            ProjectileSpawnRequest req = activeRequest.WithDirection(dir);
            manager.Spawn(req, flightOverrides);
        }
    }

    /// <summary>将二维方向向量旋转指定角度。</summary>
    /// <param name="direction">原方向。</param>
    /// <param name="angleDegrees">旋转角度（度）。</param>
    /// <returns>旋转后的单位方向。</returns>
    private static Vector2 Rotate(Vector2 direction, float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos).normalized;
    }

    /// <summary>结束飞行并回收到对象池。</summary>
    private void Recycle()
    {
        if (!isFlying)
        {
            return;
        }

        isFlying = false;
        if (ServiceLocator.TryGet(out ProjectileManager manager))
        {
            manager.Release(this);
            return;
        }

        gameObject.SetActive(false);
    }
}
