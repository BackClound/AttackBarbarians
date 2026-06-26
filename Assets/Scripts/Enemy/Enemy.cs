using UnityEngine;

/// <summary>
/// 敌人实体：分类标记、攻击探测、状态机与对象池生命周期；受击经 <see cref="DamagePipeline"/> 结算。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在敌人 Prefab 根节点（如 BatEnemy 子类）。</para>
/// <para><b>行为：</b>自上方生成向下移动，进入攻击范围后攻击城墙，伤害传导至玩家。</para>
/// </remarks>
public class Enemy : Entity, IDamagable, IPoolable
{
    /// <summary>敌人血量组件。</summary>
    public Enemy_Health enemy_Health;
    /// <summary>敌人运行时协调器。</summary>
    public EnemyController controller { get; private set; }

    [Header("Classification")]
    [SerializeField] private bool isBoss;
    [SerializeField] private bool isElite;
    [SerializeField] private bool isSpecial;

    /// <summary>是否为 Boss 敌人。</summary>
    public bool IsBoss => isBoss;
    /// <summary>是否为精英敌人。</summary>
    public bool IsElite => isElite;
    /// <summary>是否为特殊机制敌人。</summary>
    public bool IsSpecial => isSpecial;

    /// <summary>
    /// 设置 Boss 标记（波次生成时由 <see cref="EnemySpawnerManager"/> 调用）。
    /// </summary>
    /// <param name="value">是否为 Boss。</param>
    public void SetBossFlag(bool value) => isBoss = value;

    /// <summary>
    /// 设置精英标记。
    /// </summary>
    /// <param name="value">是否为精英。</param>
    public void SetEliteFlag(bool value) => isElite = value;

    /// <summary>
    /// 设置特殊敌人标记。
    /// </summary>
    /// <param name="value">是否为特殊敌人。</param>
    public void SetSpecialFlag(bool value) => isSpecial = value;

    [Header("Attack probe (Inspector)")]
    [SerializeField] protected Transform attackCheck;
    [SerializeField] protected float attackDistance;
    [SerializeField] protected LayerMask wallLayer;
    [SerializeField] public float moveSpeed;
    [SerializeField] public float cooldownThreshold;

    [Header("Animation Params")]
    [SerializeField] private string idleAnimParam = EntityAnimParams.EnemyMove;
    [SerializeField] private string moveAnimParam = EntityAnimParams.EnemyMove;
    [SerializeField] private string attackAnimParam = EntityAnimParams.EnemyAttack;
    [SerializeField] private string deadAnimParam = EntityAnimParams.EnemyDead;

    #region States
    /// <summary>待机状态实例。</summary>
    public EnemyIdleState idleState;
    /// <summary>移动状态实例。</summary>
    public EnemyMoveState moveState;
    /// <summary>攻击状态实例。</summary>
    public EnemyAttackState attackState;
    /// <summary>死亡状态实例。</summary>
    public EnemyDeadState deadState;
    #endregion

    /// <summary>向下射线攻击探测点。</summary>
    public Transform AttackProbe => attackCheck != null ? attackCheck : transform;
    /// <summary>攻击探测射线长度。</summary>
    public float AttackProbeDistance => attackDistance;
    /// <summary>墙体检测层级掩码。</summary>
    public LayerMask WallLayer => wallLayer;

    /// <summary>
    /// 设置攻击探测射线距离。
    /// </summary>
    /// <param name="distance">射线长度。</param>
    public void SetAttackProbeDistance(float distance)
    {
        attackDistance = Mathf.Max(0.05f, distance);
    }

    /// <summary>
    /// 缓存组件引用。
    /// </summary>
    public override void Awake()
    {
        base.Awake();
        enemy_Health = GetComponent<Enemy_Health>();
        controller = GetComponent<EnemyController>();
        InitializeStateMachine();
    }

    /// <summary>创建默认四态状态机（Idle/Move/Attack/Dead）。</summary>
    protected virtual void InitializeStateMachine()
    {
        if (stateMachine != null)
        {
            return;
        }

        stateMachine = new StateMachine();
        idleState = new EnemyIdleState(this, stateMachine, idleAnimParam);
        moveState = new EnemyMoveState(this, stateMachine, moveAnimParam);
        attackState = new EnemyAttackState(this, stateMachine, attackAnimParam);
        deadState = new EnemyDeadState(this, stateMachine, deadAnimParam);
    }

    /// <summary>
    /// 启动状态机；若 <see cref="EnemyController"/> 已初始化则跳过（由池化流程驱动）。
    /// </summary>
    public override void Start()
    {
        if (controller != null && controller.IsReady)
        {
            return;
        }

        stateMachine.InitialState(idleState);
        moveSpeed = enemy_Health.entity_Stats.GetMoveSpeed();
    }

    /// <summary>
    /// 检测攻击探测点下方是否命中墙体。
    /// </summary>
    /// <returns>墙体在攻击范围内时为 <c>true</c>。</returns>
    public bool IsWallDetected()
    {
        if (controller != null && controller.IsReady)
        {
            return controller.IsWallInAttackRange();
        }

        return Physics2D.Raycast(AttackProbe.position, Vector2.down, AttackProbeDistance, wallLayer).collider != null;
    }

    /// <summary>
    /// 获取近战攻击伤害值（优先读 <see cref="EnemyController"/> 配置）。
    /// </summary>
    /// <returns>对城墙/玩家造成的伤害数值。</returns>
    public virtual float GetDamageValue()
    {
        if (controller != null && controller.IsReady)
        {
            return controller.GetMeleeDamage();
        }

        if (enemy_Health != null && enemy_Health.entity_Stats != null)
        {
            return enemy_Health.entity_Stats.GetBaseAttackDamage();
        }

        return 10f;
    }

    /// <summary>
    /// 设置刚体速度。
    /// </summary>
    /// <param name="velocity">目标速度向量。</param>
    public void SetVelocity(Vector2 velocity)
    {
        rb.velocity = velocity;
    }

    /// <summary>Animator 动画结束回调，转发至当前状态。</summary>
    public override void OnAnimatorFinished()
    {
        base.OnAnimatorFinished();
        stateMachine.currentState?.OnAnimFinished();
    }

    /// <summary>
    /// Animator 攻击帧回调，转发至当前状态。
    /// </summary>
    public void OnAnimatorAttackTrigger()
    {
        stateMachine.currentState?.OnAnimAttackTrigger();
    }

    /// <summary>
    /// 销毁或回收到对象池。
    /// </summary>
    public void Die()
    {
        if (ServiceLocator.TryGet(out PoolManager poolManager) && poolManager.IsManagedInstance(gameObject))
        {
            poolManager.Despawn(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 对象池取出回调：重置血量与状态机。
    /// </summary>
    public void OnSpawn()
    {
        if (controller != null)
        {
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            return;
        }

        enemy_Health.ResetForPool();
        moveSpeed = enemy_Health.entity_Stats.GetMoveSpeed();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        stateMachine.InitialState(idleState);
    }

    /// <summary>
    /// 对象池回收回调：清除分类标记、控制状态与动画。
    /// </summary>
    public void OnDespawn()
    {
        SetBossFlag(false);
        SetEliteFlag(false);
        SetSpecialFlag(false);
        controller?.OnPoolDespawn();

        if (GetComponent<EliteController>() is EliteController eliteController)
        {
            eliteController.ResetForPool();
        }

        if (GetComponent<SpecialEnemyController>() is SpecialEnemyController specialController)
        {
            specialController.ResetForPool();
        }

        if (GetComponent<EnemyDamageShield>() is EnemyDamageShield shield)
        {
            shield.Clear();
        }

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        AnimationDriver?.ResetDriver();
    }

    /// <summary>
    /// 在 Scene 视图绘制攻击探测射线。
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Transform probe = AttackProbe;
        Gizmos.DrawLine(probe.position, probe.position + Vector3.down * AttackProbeDistance);
    }

    /// <summary>
    /// 以浮点伤害受击（兼容旧入口）。
    /// </summary>
    /// <param name="damage">伤害数值。</param>
    public override void TakeDamage(float damage)
    {
        TakeDamage(DamageInfo.FromFloat(damage, gameObject));
    }

    /// <summary>
    /// 以完整伤害上下文受击，经 <see cref="DamagePipeline"/> 结算。
    /// </summary>
    /// <param name="info">伤害上下文。</param>
    /// <returns>伤害结算结果。</returns>
    public override DamageResult TakeDamage(DamageInfo info)
    {
        DamageInfo resolved = info.Target != null ? info : info.WithTarget(gameObject);
        return DamagePipeline.Apply(resolved);
    }
}
