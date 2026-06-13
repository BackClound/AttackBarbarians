using UnityEngine;

/// <summary>
/// 玩家实体：动画回调、状态实例与单例；状态机 Tick 由 <see cref="PlayerController"/> 驱动。
/// </summary>
public class Player : Entity
{
    /// <summary>场景内玩家单例。旧代码请逐步改用 <see cref="Instance"/>。</summary>
    public static Player sInstance => Instance;

    public static Player Instance => SingletonHost<Player>.Instance;

    public static bool HasInstance => SingletonHost<Player>.HasInstance;

    #region Player other Controlers
    /// <summary>玩家运行时协调器。</summary>
    public PlayerController controller { get; private set; }
    /// <summary>玩家血量组件。</summary>
    public Player_Health player_Health { get; private set; }
    /// <summary>玩家技能管理器。</summary>
    public PlayerSkillManager skillManager { get; private set; }
    /// <summary>动画攻击帧桥接组件。</summary>
    public PlayerCombatBridge combatBridge { get; private set; }
    #endregion

    #region Player State
    /// <summary>待机状态实例。</summary>
    public PlayerIdleState idleState { get; private set; }
    /// <summary>射击动画状态实例。</summary>
    public PlayerShootState shootState { get; private set; }
    /// <summary>死亡状态实例。</summary>
    public PlayerDeadState deadState { get; private set; }
    #endregion

    /// <summary>初始化单例、状态机与各子组件引用。</summary>
    public override void Awake()
    {
        base.Awake();
        if (!SingletonHost<Player>.TryClaim(this, this, SingletonOptions.SceneDefault, out bool destroyedOwner) || destroyedOwner)
        {
            return;
        }

        stateMachine = new StateMachine();
        idleState = new PlayerIdleState(this, stateMachine, "Idle");
        shootState = new PlayerShootState(this, stateMachine, "Shoot");
        deadState = new PlayerDeadState(this, stateMachine, "Dead");

        controller = GetComponent<PlayerController>();
        player_Health = GetComponent<Player_Health>();
        skillManager = GetComponent<PlayerSkillManager>();
        combatBridge = GetComponent<PlayerCombatBridge>();
    }

    /// <summary>启动状态机，初始进入待机状态。</summary>
    public override void Start()
    {
        stateMachine.InitialState(idleState);
    }

    /// <summary>Animator 动画结束回调，转发至当前状态。</summary>
    public override void OnAniamtorFinished()
    {
        stateMachine.currentState?.OnAnimFinished();
    }

    /// <summary>切换至死亡状态。</summary>
    public void Die()
    {
        stateMachine.ChangeState(deadState);
    }

    /// <summary>Animator 攻击帧回调，转发至当前状态。</summary>
    public void OnAnimatorAttackTrigger()
    {
        stateMachine.currentState?.OnAnimAttackTrigger();
    }

    /// <summary>释放单例引用。</summary>
    private void OnDestroy()
    {
        SingletonHost<Player>.Release(this);
    }
}
