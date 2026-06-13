using UnityEngine;

/// <summary>
/// 城墙表现与受击代理：将敌人伤害转发到 Player 血量，并驱动墙体动画状态机。
/// </summary>
/// <remarks>
/// <para><b>是否需要挂载：</b>是。挂在场景墙体物体上。</para>
/// <para><b>获取方式：</b><see cref="Instance"/> 或旧名 <see cref="sInstance"/>（不再使用 Find 懒查找）。</para>
/// </remarks>
public class WallControlManager : MonoBehaviour
{
    /// <summary>单例访问（旧名兼容）。</summary>
    public static WallControlManager sInstance => Instance;

    /// <summary>全局单例实例。</summary>
    public static WallControlManager Instance => SingletonHost<WallControlManager>.Instance;

    /// <summary>单例是否已创建。</summary>
    public static bool HasInstance => SingletonHost<WallControlManager>.HasInstance;

    /// <summary>关联的玩家血量组件。</summary>
    private Player_Health playerHealth
    {
        get
        {
            if (Player.HasInstance)
            {
                return Player.Instance.player_Health;
            }
            return null;
        }
    }

    /// <summary>墙体动画控制器。</summary>
    public Animator anim;
    /// <summary>当前帧是否被标记为受击。</summary>
    [SerializeField] public bool beDamaged;

    #region 
    /// <summary>墙体动画状态机。</summary>
    public StateMachine stateMachine;
    /// <summary>空闲状态实例。</summary>
    public WallIdleState idleState;
    /// <summary>受击状态实例。</summary>
    public WallDamageState damageState;
    #endregion

    /// <summary>
    /// 初始化单例、动画器与状态机。
    /// </summary>
    private void Awake()
    {
        if (!SingletonHost<WallControlManager>.TryClaim(this, this, SingletonOptions.SceneDefault, out bool destroyedOwner) || destroyedOwner)
        {
            return;
        }

        anim = GetComponent<Animator>();
        beDamaged = false;

        stateMachine = new StateMachine();
        idleState = new WallIdleState(this, stateMachine, "isIdle");
        damageState = new WallDamageState(this, stateMachine, "isDamaged");
    }

    /// <summary>
    /// 释放单例引用。
    /// </summary>
    private void OnDestroy()
    {
        SingletonHost<WallControlManager>.Release(this);
    }

    /// <summary>
    /// 启动时将状态机初始化为空闲状态。
    /// </summary>
    private void Start()
    {
        stateMachine.InitialState(idleState);
    }

    /// <summary>
    /// 每帧驱动当前状态更新。
    /// </summary>
    private void Update()
    {
        stateMachine.currentState.OnUpdate();
    }

    /// <summary>
    /// 以简单浮点伤害对玩家结算（跳过完整公式链）。
    /// TODO：同一帧多敌人同时攻击时需考虑同步，避免仅一次伤害生效。
    /// </summary>
    /// <param name="damage">基础伤害数值。</param>
    public void TakeDamage(float damage)
    {
        ApplyDamageToPlayer(DamageInfo.FromFloat(damage, ResolvePlayerTarget(), null));
    }

    /// <summary>
    /// 将伤害信息转发至玩家并标记城墙受击。
    /// </summary>
    /// <param name="info">伤害上下文。</param>
    /// <returns>伤害结算结果。</returns>
    private DamageResult ApplyDamageToPlayer(DamageInfo info)
    {
        GameObject target = ResolvePlayerTarget();
        if (target == null)
        {
            return DamageResult.None;
        }

        DamageInfo resolved = info.Target != null ? info : info.WithTarget(target);
        DamageResult result = DamagePipeline.Apply(resolved);
        beDamaged = true;
        return result;
    }

    /// <summary>
    /// 敌人攻击城墙时由 <see cref="EnemyController.ExecuteWallAttack"/> 调用，转发至玩家血量并由 <see cref="DamageSystem"/> 结算。
    /// </summary>
    /// <param name="enemy">攻击来源敌人。</param>
    /// <param name="baseDamage">基础伤害数值。</param>
    public void TakeDamageFromEnemy(Enemy enemy, float baseDamage)
    {
        GameObject target = ResolvePlayerTarget();
        if (target == null)
        {
            Debug.LogWarning("[WallControlManager] 无法结算伤害：Player 未就绪。");
            return;
        }

        ApplyDamageToPlayer(DamageInfo.Create(enemy, target, baseDamage));
    }

    /// <summary>
    /// 解析玩家 GameObject 作为伤害目标。
    /// </summary>
    /// <returns>玩家对象，未就绪时返回 null。</returns>
    private GameObject ResolvePlayerTarget()
    {
        return Player.HasInstance ? Player.Instance.gameObject : null;
    }

    /// <summary>
    /// 重置城墙受击标记。
    /// </summary>
    /// <param name="isDamaged">是否处于受击状态。</param>
    public void ResetDamageState(bool isDamaged)
    {
        beDamaged = isDamaged;
    }

    /// <summary>
    /// 动画事件回调：通知当前状态动画已播放完毕。
    /// </summary>
    public void OnAnimFinished()
    {
        if (stateMachine.currentState == null) return;
        stateMachine.currentState.OnAnimFinished();
    }


}
